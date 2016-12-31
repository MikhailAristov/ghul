using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; //TODO

// This is the controller class that manages the game state data
public class Control_GameState : MonoBehaviour {
    
    private Data_GameState GS;

	public bool AUTOSTART_NEW_GAME;
	public Canvas MAIN_MENU_CANVAS;
	public GameObject RITUAL_ROOM_SCRIBBLES;

	private Control_Camera MAIN_CAMERA_CONTROL;
	private Factory_PrefabController prefabFactory;

    private float? AUTOSAVE_FREQUENCY;
    private float NEXT_AUTOSAVE_IN;

    // Use this for initialization
    void Start ()
    {
		MAIN_CAMERA_CONTROL = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();
		prefabFactory = GetComponent<Factory_PrefabController>();
		if(AUTOSTART_NEW_GAME && Debug.isDebugBuild) {
			onNewGameSelect(); // For debug only
		} else {
			continueFromSavedGameState();
			setAdditionalParameters();
			GS.SUSPENDED = true; // Suspend the game while in the main menu initially
		}
    }

    // Update is called once per frame
    void Update()
    {
        if (GS.SUSPENDED) { return; }

		// Timed autosave
        NEXT_AUTOSAVE_IN -= Time.deltaTime;
		if (NEXT_AUTOSAVE_IN <= 0.0f && AUTOSAVE_FREQUENCY != null) {
			NEXT_AUTOSAVE_IN = (float)AUTOSAVE_FREQUENCY;
            Data_GameState.saveToDisk(GS);
        }

        // Open main menu if the player presses Esc
        if (Input.GetButton("Cancel")) {
            GS.SUSPENDED = true;
            MAIN_MENU_CANVAS.enabled = true;
        }

		// Check if an item has been placed
		if(GS.NEXT_ITEM_PLEASE == true) {
			// Reset the flag
			GS.NEXT_ITEM_PLEASE = false;
			// Check if it's all of them
			if(GS.ITEMS.Count < Global_Settings.read("RITUAL_ITEMS_REQUIRED")) {
				spawnNextItem();
			} else {
				triggerEndgame();
			}
		}
    }

    // This method updates parameters after loading or resetting the game
    private void setAdditionalParameters()
    {
        // Train the camera on the main character
        MAIN_CAMERA_CONTROL.loadGameState(GS);
        MAIN_CAMERA_CONTROL.setFocusOn(GS.getCHARA().pos);

        // Initialize autosave
		AUTOSAVE_FREQUENCY = Global_Settings.read("AUTOSAVE_FREQUENCY");
		NEXT_AUTOSAVE_IN = (float)AUTOSAVE_FREQUENCY;
    }

    // This method loads the saved game state to memory
    private void continueFromSavedGameState()
    {
        // Load the game state from the disk
        GS = Data_GameState.loadFromDisk();
        // If no game state has been found, initialize it instead
        if(GS == null) { resetGameState(); return; }

        // Fix all the room object references, including the door and item spot assignments
        foreach (Data_Room r in GS.ROOMS.Values) {
			r.fixObjectReferences(GS, prefabFactory);
            r.env.loadGameState(GS, r.INDEX); // While we are on it, load game state into room environment scripts 
		}

		// Fix door object references first, because Data_Room.fixObjectReferences() relies on them being set
		foreach (Data_Door d in GS.DOORS.Values) {
			d.fixObjectReferences(GS, prefabFactory);
		}

        // Fix the character, cadaver and monster object references
        GS.getCHARA().fixObjectReferences(GS);
        GS.getCHARA().control.loadGameState(GS);
        GS.getMonster().fixObjectReferences(GS);
        GS.getMonster().control.loadGameState(GS);
		GS.getCadaver().fixObjectReferences(GS);

		// Placing the cadaver sprite in the location they used to be
		Data_Cadaver cadaver = GS.getCadaver();
		Vector3 positionOfCadaver = new Vector3 (cadaver.atPos, cadaver.isIn.gameObj.transform.position.y - 1.55f, 0);
		cadaver.gameObj.transform.Translate(positionOfCadaver - cadaver.gameObj.transform.position);
		cadaver.updatePosition(cadaver.isIn, cadaver.atPos);

		// Fix the items
		int numOfItems = GS.ITEMS.Count; 
		for (int i = 0; i < numOfItems; i++) {
			Data_Item item = GS.getItemByIndex(i);
			item.fixObjectReferences(GS, prefabFactory);
			item.control.loadGameState(GS, i);
			item.control.updateGameObjectPosition();
		}
		StartCoroutine(updateWallScribbles(0.0f));
    }

    // This method initializes the game state back to default
    private void resetGameState()
    {
		// Remove all doors, rooms and items
		List<GameObject> oldGameObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("Item"));
		oldGameObjects.AddRange(GameObject.FindGameObjectsWithTag("Door"));
		oldGameObjects.AddRange(GameObject.FindGameObjectsWithTag("Room"));
		foreach(GameObject go in oldGameObjects) { Destroy(go); }

		// Reset the prefab generator counters
		prefabFactory.resetAllCounters();

        // Initialize game settings
        GS = new Data_GameState();

		// Initialize the starting ritual room
		initializeTheRitualRoom();

		// Spawn more rooms from prefabs
		int maxRooms = (int)Global_Settings.read("TOTAL_NUMBER_OF_ROOMS");
		while(GS.ROOMS.Count < maxRooms) {
			// Generate the room game object from prefabs
			GameObject roomObj = prefabFactory.spawnRandomRoom(Global_Settings.read("VERTICAL_ROOM_SPACING"));
			Factory_PrefabRooms.RoomPrefab roomPrefab = prefabFactory.getRoomPrefabDetails(roomObj.name);
			// Load the prefab details into the data object
			Data_Room newRoom = new Data_Room(GS.ROOMS.Count, roomObj, roomPrefab);
			GS.addRoom(newRoom);
			// Add doors
			if(roomPrefab.doorSpawnLeft) {
				spawnDoor(Data_Door.TYPE_LEFT_SIDE, newRoom, roomObj.transform, roomPrefab.size.x, 0);
			}
			foreach(float xPos in roomPrefab.doorSpawns) {
				spawnDoor(Data_Door.TYPE_BACK_DOOR, newRoom, roomObj.transform, roomPrefab.size.x, xPos);
			}
			if(roomPrefab.doorSpawnRight && // The next clause ensures an even total number of doors
				(GS.ROOMS.Count < maxRooms || GS.DOORS.Count % 2 == 1)) {
				spawnDoor(Data_Door.TYPE_RIGHT_SIDE, newRoom, roomObj.transform, roomPrefab.size.x, 0);
			}
			newRoom.env.loadGameState(GS, newRoom.INDEX);
		}

		// TODO: Connect doors properly
		// TODO: Must ensure that side doors never connect to the opposite sides, or it will look weird and cause trouble with room transitions
		int connectionCounter = 0;
		foreach(Data_Door d in GS.DOORS.Values) {
			if(d.connectsTo == null) {
				Data_Door targetDoor;
				do {
					targetDoor = GS.getDoorByIndex(UnityEngine.Random.Range(0, GS.DOORS.Count));
				} while(targetDoor.connectsTo != null); // TODO || targetDoor.isIn == d.isIn
				d.connectTo(targetDoor);
				connectionCounter += 2;
				if((connectionCounter + 2) > GS.DOORS.Count) {
					break;
				}
			}
		}

		initializeCharacters();
	}

	// Loads a fake prefab for the ritual room that already exists in the game space from the start
	private void initializeTheRitualRoom() {
		// Instantiate the ritual room itself
		GameObject ritualRoomGameObject = GameObject.Find("Ritual Room");
		Factory_PrefabRooms.RoomPrefab ritualRoomPrefab = prefabFactory.getRoomPrefabDetails("[prefab00]");
		Data_Room ritualRoom = new Data_Room(GS.ROOMS.Count, ritualRoomGameObject, ritualRoomPrefab);
		GS.addRoom(ritualRoom);
		// Instantiate the ritual room doors
		spawnDoor(Data_Door.TYPE_LEFT_SIDE, ritualRoom, ritualRoomGameObject.transform, ritualRoomPrefab.size.x, 0);
		spawnDoor(Data_Door.TYPE_BACK_DOOR, ritualRoom, ritualRoomGameObject.transform, ritualRoomPrefab.size.x, ritualRoomPrefab.doorSpawns[0]);
		spawnDoor(Data_Door.TYPE_RIGHT_SIDE, ritualRoom, ritualRoomGameObject.transform, ritualRoomPrefab.size.x, 0);
		// Load the ennviornment
		ritualRoom.env.loadGameState(GS, 0);
	}

	// Initializes all characters on a new game
	private void initializeCharacters() {
		// INITIALIZE CADAVER
		GS.setCadaverCharacter("Cadaver");
		GS.getCadaver().updatePosition(GS.getRoomByIndex(0), -7, 0); // move the cadaver out of sight at first

		// INITIALIZE PLAYER CHARACTER
		GS.setPlayerCharacter("PlayerCharacter");
		GS.getCHARA().updatePosition(GS.getRoomByIndex(0), 0, 0); // default: starting position is center of pentagram
		GS.getCHARA().control.loadGameState(GS);

		// INITIALIZE MONSTER
		GS.setMonsterCharacter("Monster");
		GS.getMonster().updatePosition(GS.getRoomByIndex(UnityEngine.Random.Range(1, GS.ROOMS.Count)), 0, 0);
		GS.getMonster().setForbiddenRoomIndex(0);
		GS.getMonster().control.loadGameState(GS);
	}

	// Place a door within both game state and game space
	private Data_Door spawnDoor(int doorType, Data_Room parent, Transform parentTransform, float parentWidth, float xPos) {
		float horizontalRoomMargin = Global_Settings.read("HORIZONTAL_ROOM_MARGIN");
		GameObject doorGameObj;
		// Differentiate by type
		switch(doorType) {
		case Data_Door.TYPE_LEFT_SIDE:
			doorGameObj = prefabFactory.spawnLeftSideDoor(parentTransform, parentWidth);
			xPos = (horizontalRoomMargin - parentWidth)/2; // Overwrite any specified position with a virtual one outside of actual room constraints
			break;
		case Data_Door.TYPE_BACK_DOOR:
			doorGameObj = prefabFactory.spawnBackDoor(parentTransform, xPos);
			break;
		case Data_Door.TYPE_RIGHT_SIDE:
			doorGameObj = prefabFactory.spawnRightSideDoor(parentTransform, parentWidth);
			xPos = (parentWidth - horizontalRoomMargin)/2;
			break;
		default: // C# won't compile otherwise
			doorGameObj = new GameObject();
			break;
		}
		// Initialize the door object and add it
		Data_Door doorObj = new Data_Door(GS.DOORS.Count, doorGameObj, doorType, parent, xPos);
		GS.addDoor(doorObj);
		return doorObj;
	}

	// Places the next item in a random spot
	private Data_Item spawnNextItem() {
		int newItemIndex = GS.ITEMS.Count;
		// Calculate spawn position
		Data_Position spawnPos = GS.getRandomItemSpawn();
		Data_Room parentRoom = GS.getRoomByIndex(spawnPos.RoomId);
		Transform parentRoomTransform = parentRoom.env.transform;
		Vector3 gameObjectPos = new Vector3(spawnPos.X, spawnPos.Y, -0.1f);
		// Spawn a new item from prefabs
		GameObject newItemObj = prefabFactory.spawnRandomItem(parentRoomTransform, gameObjectPos);
		Data_Item newItem = GS.addItem(newItemObj.name);
		// Place the new item
		newItem.updatePosition(parentRoom, spawnPos.X, spawnPos.Y);
		newItem.control.loadGameState(GS, newItemIndex);
		// Update the wall scribbles
		StartCoroutine(updateWallScribbles(1.0f));
		// Save the new game state to disk
		Data_GameState.saveToDisk(GS);
		return newItem;
	}

	// Updates the scribbles on the wall in the ritual room, indicating the next item to find
	private IEnumerator updateWallScribbles(float overTime) {
		SpriteRenderer rend = RITUAL_ROOM_SCRIBBLES.GetComponent<SpriteRenderer>();
		Sprite newSprite = GS.getCurrentItem().control.GetComponent<SpriteRenderer>().sprite;

		// If the time given is zero or less, just replace the sprite and exit
		if(overTime <= 0) { rend.sprite = newSprite; yield break; }

		// Otherwise, first fade out the current scribbles
		float halfTime = overTime/2;
		for(float timeLeft = halfTime; timeLeft > 0; timeLeft -= Time.deltaTime) {
			rend.color -= new Color (0, 0, 0, Time.deltaTime/halfTime);
			yield return null;
		}
		// Then update them and fade back in
		rend.sprite = newSprite;
		for(float timeLeft = halfTime; timeLeft > 0; timeLeft -= Time.deltaTime) {
			rend.color += new Color (0, 0, 0, Time.deltaTime/halfTime);
			yield return null;
		}
	}

	private void triggerEndgame() {
		// TODO: Proper endgame
		Debug.LogError("Congratulations, you've won the game!");
	}

    // This method is called when the New Game button is activated from the main menu
    void onNewGameSelect()
    {
        resetGameState();                   // Reset the game state
        setAdditionalParameters();          // Refocus camera and such
        MAIN_MENU_CANVAS.enabled = false;   // Dismiss the main menu
		GS.SUSPENDED = false;               // Continue playing
    }

    // This method is called when the Continue button is activated from the main menu
    void onContinueSelect()
    {
        // Simply dismiss the main menu to continue playing
        MAIN_MENU_CANVAS.enabled = false;
		GS.SUSPENDED = false;
	}

    // This method is called when the Exit button is activated from the main menu
    void onExitSelect()
    {
        // Save and exit
        Data_GameState.saveToDisk(GS);
        Application.Quit();
    }
}
