﻿using UnityEngine;
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

    private float AUTOSAVE_FREQUENCY;
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
        if (NEXT_AUTOSAVE_IN <= 0.0f) {
            NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;
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
        NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;
    }

    // This method loads the saved game state to memory
    private void continueFromSavedGameState()
    {
        // Load the game state from the disk
        GS = Data_GameState.loadFromDisk();
        // If no game state has been found, initialize it instead
        if(GS == null) { resetGameState(); return; }

        // Fix door object references first, because Data_Room.fixObjectReferences() relies on them being set
        foreach (Data_Door d in GS.DOORS.Values) {
            d.fixObjectReferences(GS);
        }
        // Now fix all the room object references, including the door and item spot assignments
        foreach (Data_Room r in GS.ROOMS.Values) {
            r.fixObjectReferences(GS);
            r.env.loadGameState(GS, r.INDEX); // While we are on it, load game state into room environment scripts 
        }

        // Fix the character, cadaver and monster object references
        GS.getCHARA().fixObjectReferences(GS);
        GS.getCHARA().control.loadGameState(GS);
        GS.getMonster().fixObjectReferences(GS);
        GS.getMonster().control.loadGameState(GS);
		GS.getCadaver().fixObjectReferences(GS);

		// Placing the cadaver and item sprites in the location they used to be
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
        // Initialize game settings
        GS = new Data_GameState();

        // INITIALIZE ROOMS
		GS.addRoom("Ritual Room");
        GS.addRoom("Room01");
        GS.addRoom("Room02");
        GS.addRoom("Room03");

        // INITIALIZE DOORS
        GS.addDoor("Door0-1", 0);
        GS.addDoor("Door0-2", 0);
        GS.addDoor("Door0-3", 0);
        GS.addDoor("Door1-1", 1);
        GS.addDoor("Door1-2", 1);
        GS.addDoor("Door2-1", 2);
        GS.addDoor("Door3-1", 3);
        GS.addDoor("Door3-2", 3);

        // CONNECT DOORS
        GS.connectTwoDoors(0, 5);
        GS.connectTwoDoors(1, 3);
        GS.connectTwoDoors(2, 6);
        GS.connectTwoDoors(4, 7);
        // TODO: Must ensure that side doors never connect to the opposite sides, or it will look weird and cause trouble with room transitions

		List<string> prefabNames = new List<string>(new string[] {"Room04: Foyer [prefab02]", "Room05: Foyer [prefab00]"});
		foreach(string n in prefabNames) {
			// TODO: GameObject nextRoom = prefabFactory.spawnRandomRoom(Global_Settings.read("VERTICAL_ROOM_SPACING"));
			//prefabFactory.spawnDoorsInARoom(gameObj.transform, roomPrefab.size.x, roomPrefab.doorSpawnLeft, new float[0], roomPrefab.doorSpawnRight);

			// Generate the room game object from prefabs
			GameObject roomObj = prefabFactory.spawnRoomFromName(n, Global_Settings.read("VERTICAL_ROOM_SPACING"));
			Factory_PrefabRooms.RoomPrefab roomPrefab = prefabFactory.getRoomPrefabDetails(roomObj.name);
			// Load the prefab details into the data object
			Data_Room newRoom = new Data_Room(GS.ROOMS.Count, roomObj, roomPrefab);
			GS.addRoom(newRoom);
			// Add doors
			if(roomPrefab.doorSpawnLeft && true) {
				GameObject lsdObj = prefabFactory.spawnLeftSideDoor(roomObj.transform, roomPrefab.size.x);
				Data_Door lsd = new Data_Door(GS.DOORS.Count, lsdObj, Data_Door.TYPE_LEFT_SIDE, newRoom, -roomPrefab.size.x); // Dummy position outside of actual room constraints
				GS.addDoor(lsd);
			}
			foreach(float xPos in roomPrefab.doorSpawns) {
				GameObject bdObj = prefabFactory.spawnBackDoor(roomObj.transform, xPos);
				Data_Door bd = new Data_Door(GS.DOORS.Count, bdObj, Data_Door.TYPE_BACK_DOOR, newRoom, xPos);
				GS.addDoor(bd);
				break;
			}
			if(roomPrefab.doorSpawnRight && false) {
				GameObject rsdObj = prefabFactory.spawnRightSideDoor(roomObj.transform, roomPrefab.size.x);
				Data_Door rsd = new Data_Door(GS.DOORS.Count, rsdObj, Data_Door.TYPE_RIGHT_SIDE, newRoom, roomPrefab.size.x); // Dummy position, ditto
				GS.addDoor(rsd);
			}
		}

		// TODO: Connect doors properly
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

		// INITIALIZE ITEM SPOTS
		GS.addItemSpot("ItemSpot1-1", 1);
		GS.addItemSpot("ItemSpot1-2", 1);
		GS.addItemSpot("ItemSpot2-1", 2);
		GS.addItemSpot("ItemSpot2-2", 2);
		GS.addItemSpot("ItemSpot2-3", 2);
		GS.addItemSpot("ItemSpot2-4", 2);
		GS.addItemSpot("ItemSpot3-1", 3);
		GS.addItemSpot("ItemSpot3-2", 3);
		GS.addItemSpot("ItemSpot3-3", 3);

        // Load game state into room environment scripts
        foreach (Data_Room r in GS.ROOMS.Values) {
            r.env.loadGameState(GS, r.INDEX);
		}

		// INITIALIZE CADAVER
		GS.setCadaverCharacter("Cadaver");
		GS.getCadaver().updatePosition(GS.getRoomByIndex(0), GS.getCadaver().gameObj.transform.position.x);

        // INITIALIZE PLAYER CHARACTER
        GS.setPlayerCharacter("PlayerCharacter");
        GS.getCHARA().updatePosition(GS.getRoomByIndex(5), 0); // default: starting position is center of pentagram
		GS.getCHARA().startingPos = new Data_Position(0, 0);
        GS.getCHARA().control.loadGameState(GS);

        // INITIALIZE MONSTER
        GS.setMonsterCharacter("Monster");
        GS.getMonster().updatePosition(GS.getRoomByIndex(1), GS.getMonster().gameObj.transform.position.x);
		GS.getMonster().control.loadGameState(GS);
		GS.getMonster().setForbiddenRoomIndex(GS.getCHARA().isIn.INDEX);
    	
		// Placing the cadaver sprite out of sight
		Vector3 nirvana = new Vector3 (-100, 0, 0);
		GS.getCadaver().gameObj.transform.Translate(nirvana - GS.getCadaver().gameObj.transform.position);
		GS.getCadaver().updatePosition(-100);
	}

	// Places the next item in a random spot
	private void spawnNextItem() {
		int newItemIndex = GS.ITEMS.Count;
		int newSpawnIndex = Random.Range(0, GS.ITEM_SPOTS.Count);
		// Calculate spawn position
		Data_ItemSpawn spawnPos = GS.getItemSpawnPointByIndex(newSpawnIndex);
		Transform parentRoom = GS.getRoomByIndex(spawnPos.RoomId).env.transform;
		Vector3 gameObjectPos = new Vector3(spawnPos.X, spawnPos.Y, -0.1f);
		// Spawn a new item from prefabs
		GameObject newItemObj = prefabFactory.spawnRandomItem(parentRoom, gameObjectPos);
		Data_Item newItem = GS.addItem(newItemObj.name);
		GS.setItemSpawnPoint(newItemIndex, newSpawnIndex);
		// Place the new item
		newItem.control.loadGameState(GS, newItemIndex);
		newItem.control.resetToSpawnPosition();
		// Update the wall scribbles
		StartCoroutine(updateWallScribbles(1.0f));
		// Save the new game state to disk
		Data_GameState.saveToDisk(GS);  
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
