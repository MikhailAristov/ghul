using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// This is the controller class that manages the game state data
public class Control_GameState : MonoBehaviour {
    
	private Data_GameState GS;

	// Before all items are placed for ritual
	public const int STATE_COLLECTION_PHASE = 0;
	// After all items are placed, but before old monster dies
	public const int STATE_TRANSFORMATION = 1;
	// The endgame: After Toni has become the new monster
	public const int STATE_MONSTER_PHASE = 2;
	// After suicidle has played out completely
	public const int STATE_MONSTER_DEAD = 3;

	public Canvas MainMenuCanvas;
	public GameObject NewGameButton;
	public GameObject RitualRoomScribbles;

	private Control_Camera MAIN_CAMERA_CONTROL;
	private Factory_PrefabController prefabFactory;
	private Factory_Graph graphFactory;

	// Global parameters
	private int STARTING_ROOM_INDEX;
	private int TOTAL_NUMBER_OF_ROOMS;
	private float VERTICAL_ROOM_SPACING;
	private float HORIZONTAL_ROOM_MARGIN;
	private float DOOR_TRANSITION_COST;

	private int TOTAL_ITEMS_PLACED;
	private int RITUAL_ITEMS_REQUIRED;
	private float RITUAL_PENTAGRAM_CENTER;

	private float AUTOSAVE_FREQUENCY;
	private float NEXT_AUTOSAVE_IN;
	private bool newGameDisabled;

	// Use this for initialization
	void Awake() {
		STARTING_ROOM_INDEX = (int)Global_Settings.read("RITUAL_ROOM_INDEX");
		TOTAL_NUMBER_OF_ROOMS = (int)Global_Settings.read("TOTAL_NUMBER_OF_ROOMS");
		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		HORIZONTAL_ROOM_MARGIN = Global_Settings.read("HORIZONTAL_ROOM_MARGIN");

		// A rough estimation of what "distance" lies between two sides of a door for the all-pairs shortest distance calculation
		DOOR_TRANSITION_COST = Global_Settings.read("CHARA_WALKING_SPEED") * Global_Settings.read("DOOR_TRANSITION_DURATION");

		TOTAL_ITEMS_PLACED = (int)Global_Settings.read("TOTAL_NUMBER_OF_ITEMS_PLACED");
		RITUAL_ITEMS_REQUIRED = (int)Global_Settings.read("RITUAL_ITEMS_REQUIRED");
		RITUAL_PENTAGRAM_CENTER = Global_Settings.read("RITUAL_PENTAGRAM_CENTER");
			
		AUTOSAVE_FREQUENCY = Global_Settings.read("AUTOSAVE_FREQUENCY");
		NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;
	}

	void Start() {
		MAIN_CAMERA_CONTROL = Camera.main.GetComponent<Control_Camera>();
		// Initialize factories
		prefabFactory = GetComponent<Factory_PrefabController>();
		graphFactory = GetComponent<Factory_Graph>();
		// Load the game if possible
		continueFromSavedGameState();
		if(GS != null) {
			setAdditionalParameters();
			GS.SUSPENDED = true; // Suspend the game while in the main menu initially

			// If the game has transitioned into the endgame, disable the New Game Button
			if(GS.OVERALL_STATE > STATE_COLLECTION_PHASE) {
				disableNewGameButton();
			}
		}
	}

	private void disableNewGameButton() {
		newGameDisabled = true;
		NewGameButton.GetComponent<Image>().color = new Color(100f / 255f, 0f, 0f);
	}

	private void reenableNewGameButton() {
		newGameDisabled = false;
		NewGameButton.GetComponent<Image>().color = new Color(136f / 255f, 136f / 255f, 136f / 255f);
	}

	// Update is called once per frame
	void Update() {
		if(GS == null || GS.SUSPENDED) {
			return;
		}

		// Timed autosave
		NEXT_AUTOSAVE_IN -= Time.deltaTime;
		if(NEXT_AUTOSAVE_IN <= 0.0f) {
			NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;
			Control_Persistence.saveToDisk(GS);
		}

		// Open main menu if the player presses Esc
		if(Input.GetButton("Cancel")) {
			GS.SUSPENDED = true;
			MainMenuCanvas.enabled = true;
		}

		// For the purpose of debugging, allow game reset hotkey
		if(Debug.isDebugBuild && Input.GetButtonDown("Reset Game State")) {
			GS.SUSPENDED = true;
			GS.OVERALL_STATE = STATE_COLLECTION_PHASE;
			reenableNewGameButton();
			onNewGameSelect();
		}

		switch(GS.OVERALL_STATE) {
		case STATE_COLLECTION_PHASE:
			updateDuringCollectionPhase();
			break;
		case STATE_TRANSFORMATION:
			// Switch to next state as soon as the monster dies
			if(GS.MONSTER_KILLED) {
				GS.OVERALL_STATE = STATE_MONSTER_PHASE;
				GS.getMonster().control.setupEndgame();
			}
			break;
		case STATE_MONSTER_PHASE:
			// The only way Toni can die in the endgame is by suicidle
			if(GS.TONI_KILLED) {
				GS.OVERALL_STATE = STATE_MONSTER_DEAD;
				Control_Persistence.saveToDisk(GS);
			}
			break;
		default:
		case STATE_MONSTER_DEAD:
			// Do nothing
			break;
		}
	}

	// Normal update routine before all items are placed
	// Can trigger STATE_TRANSFORMATION
	private void updateDuringCollectionPhase() {
		// Check if player died to trigger house mix up
		if(GS.TONI_KILLED == true) {
			GS.TONI_KILLED = false;
			houseMixup(GS.TONI.deaths);
		}

		// Check if all items have been placed
		if(GS.numItemsPlaced >= RITUAL_ITEMS_REQUIRED) {
			GS.OVERALL_STATE = STATE_TRANSFORMATION;
			triggerEndgame(GS.OVERALL_STATE);
		} else { // Otherwise, check if wall scribbles need to be updated
			if(GS.ANOTHER_ITEM_PLEASE) {
				GS.indexOfSearchedItem = pickAnotherItemToSearchFor();
				GS.ANOTHER_ITEM_PLEASE = false;
				StartCoroutine(updateWallScribbles(1.0f));
			}
		}
	}

	// This method updates parameters after loading or resetting the game
	private void setAdditionalParameters() {
		// Train the camera on the main character
		MAIN_CAMERA_CONTROL.loadGameState(GS);
		MAIN_CAMERA_CONTROL.setFocusOn(GS.getToni().pos);

		// Initialize the sound system
		GetComponent<Control_Sound>().loadGameState(GS);
	}

	// This method loads the saved game state to memory
	private void continueFromSavedGameState() {
		// Load the game state from the disk
		GS = Control_Persistence.loadFromDisk<Data_GameState>();
		// If no game state has been found, stop right here
		if(GS == null) {
			return;
		}

		// Fix all the room object references, including the door and item spot assignments
		foreach(Data_Room r in GS.ROOMS.Values) {
			r.fixObjectReferences(GS, prefabFactory);
			r.env.loadGameState(GS, r.INDEX); // While we are on it, load game state into room environment scripts 
		}

		// Now fix all door object references
		foreach(Data_Door d in GS.DOORS.Values) {
			d.fixObjectReferences(GS, prefabFactory);
		}

		// Fix the character, cadaver and monster object references
		GS.getToni().fixObjectReferences(GS);
		GS.getToni().control.loadGameState(GS);
		GS.getMonster().fixObjectReferences(GS);
		GS.getMonster().control.loadGameState(GS);
		GS.getCadaver().fixObjectReferences(GS);

		// Placing the cadaver sprite in the location they used to be
		Data_Cadaver cadaver = GS.getCadaver();
		Vector3 positionOfCadaver = new Vector3(cadaver.atPos, cadaver.isIn.gameObj.transform.position.y, 0);
		cadaver.gameObj.transform.Translate(positionOfCadaver - cadaver.gameObj.transform.position);
		cadaver.updatePosition(cadaver.isIn, cadaver.atPos);

		// Fix the items
		for(int i = 0; i < GS.ITEMS.Count; i++) {
			Data_Item item = GS.getItemByIndex(i);
			item.fixObjectReferences(GS, prefabFactory);
			item.control.loadGameState(GS, i);
			item.control.updateGameObjectPosition();
		}
		StartCoroutine(updateWallScribbles(0));

		// Re-trigger the endgame if necessary
		if(GS.OVERALL_STATE > STATE_COLLECTION_PHASE) {
			triggerEndgame(GS.OVERALL_STATE);
		}
		// Guide monster Toni
		if(GS.OVERALL_STATE == STATE_MONSTER_PHASE) {
			GS.getToni().control.guideMonsterToniToIntruders();
		}
	}

	// TODO: Proper endgame
	private void triggerEndgame(int overallState) {
		if(overallState > STATE_COLLECTION_PHASE) {
			// Disable new game button
			disableNewGameButton();
			// Transform Toni
			GS.getToni().control.setupEndgame();
		}
		if(overallState > STATE_TRANSFORMATION) {
			GS.getMonster().control.setupEndgame();
		}
	}

	// This method initializes the game state back to default
	private void resetGameState() {
		// Update the weights before resetting the complete game state
		if(GS != null) {
			GS.getMonster().worldModel.playerParameters.updateWalkingDistanceWeights(GS.getToni().roomHistory);
		}

		// Remove all doors, rooms and items
		StopAllCoroutines();
		List<GameObject> oldGameObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("Item"));
		oldGameObjects.AddRange(GameObject.FindGameObjectsWithTag("Door"));
		oldGameObjects.AddRange(GameObject.FindGameObjectsWithTag("Room"));
		foreach(GameObject go in oldGameObjects) {
			Destroy(go);
		}
		// Reset the prefab generator counters
		prefabFactory.resetAllCounters();

		// Create an new game state
		GS = new Data_GameState();
		GS.SUSPENDED = true;

		// Initialize all rooms, starting with the ritual room
		initializeTheRitualRoom();
		spawnAllOtherRooms(TOTAL_NUMBER_OF_ROOMS);

		// Create the house graph
		graphFactory.deleteGraph();
		graphFactory.computePlanarGraph(GS.HOUSE_GRAPH);

		// Spawn and connect the doors in pairs
		respawnAndConnectAllDoors();

		// Precompute all-pairs shortest distances
		precomputeAllDistances();

		// Initialize all the characters
		initializeCharacters();

		// Spawn all items
		for(int i = 0; i < TOTAL_ITEMS_PLACED; i++) {
			spawnNextItem();
		}
	}

	// Loads a fake prefab for the ritual room that already exists in the game space from the start
	private void initializeTheRitualRoom() {
		// Instantiate the ritual room itself
		GameObject ritualRoomGameObject = GameObject.Find("Ritual Room");
		Factory_PrefabRooms.RoomPrefab ritualRoomPrefab = prefabFactory.getRoomPrefabDetails("[prefab00]");
		Data_Room ritualRoom = new Data_Room(GS.ROOMS.Count, ritualRoomGameObject, ritualRoomPrefab);
		GS.addRoom(ritualRoom);
		// Load the environment
		ritualRoom.env.loadGameState(GS, 0);
	}

	// Spawn all other rooms randomly from prefabs up to a certain count
	private void spawnAllOtherRooms(int totalRoomCount) {
		int minRoomsWith4DoorSpawns = 1; // Graph API prerequisite
		while(GS.ROOMS.Count < totalRoomCount) {
			// Check how many door spawns are required
			int minDoorSpawns = (minRoomsWith4DoorSpawns-- > 0) ? 4 : 0;
			// Spawn the new room
			spawnRandomRoom(minDoorSpawns);
		}
	}

	// Spawns a random new room in the game space (very similar to initializeTheRitualRoom)
	private void spawnRandomRoom(int minDoorSpawns) {
		// Generate the room game object from prefabs
		GameObject roomObj = prefabFactory.spawnRandomRoom(minDoorSpawns, VERTICAL_ROOM_SPACING);
		Factory_PrefabRooms.RoomPrefab roomPrefab = prefabFactory.getRoomPrefabDetails(roomObj.name);
		// Load the prefab details into the data object
		Data_Room newRoom = new Data_Room(GS.ROOMS.Count, roomObj, roomPrefab);
		GS.addRoom(newRoom);
		// Load the environment
		newRoom.env.loadGameState(GS, newRoom.INDEX);
	}

	// Initializes all characters on a new game
	private void initializeCharacters() {
		Data_Room ritualRoom = GS.getRoomByIndex(STARTING_ROOM_INDEX);

		// INITIALIZE CADAVER
		GS.setCadaverCharacter("Cadaver");
		GS.getCadaver().updatePosition(ritualRoom, -7, 0); // move the cadaver out of sight at first
		GS.getCadaver().gameObj.transform.position = new Vector3(-7, 0, 0);

		// INITIALIZE PLAYER CHARACTER
		GS.setPlayerCharacter("PlayerCharacter");
		Data_PlayerCharacter Toni = GS.getToni();
		Toni.updatePosition(ritualRoom, RITUAL_PENTAGRAM_CENTER, 0);
		Toni.resetRoomHistory();

		// Make it look like Toni has just walked in through the right door (next to the pentagram)
		Toni.updatePosition(ritualRoom, ritualRoom.rightWalkBoundary);
		Toni.control.loadGameState(GS);
		ritualRoom.rightmostDoor.control.open();

		// INITIALIZE MONSTER
		GS.setMonsterCharacter("Monster");
		GS.getMonster().updatePosition(GS.getRoomFurthestFrom(STARTING_ROOM_INDEX), 0, 0);
		GS.getMonster().resetWorldModel(GS);
		GS.getMonster().control.loadGameState(GS);
	}

	// Spawns all doors from the door graph
	private void respawnAndConnectAllDoors() {
		// Remove all existing doors
		GS.removeAllDoors();
		foreach(GameObject go in GameObject.FindGameObjectsWithTag("Door")) {
			Destroy(go);
		}
		// Get the spawn-to-spawn connection matrix
		int[,] doorSpawnConnections = GS.HOUSE_GRAPH.exportAllRoom2RoomConnections();
		// Spawn and connect all the doors anew
		for(int i = 0; i < doorSpawnConnections.GetLength(0); i++) {
			// Parse the return matrix
			int thisRoomID = doorSpawnConnections[i, 0];
			int thisDoorSpawnIDrelativeToRoom = doorSpawnConnections[i, 1];
			int thisDoorSpawnType = doorSpawnConnections[i, 2];
			int otherRoomID = doorSpawnConnections[i, 3];
			int otherDoorSpawnIDrelativeToRoom = doorSpawnConnections[i, 4];
			int otherDoorSpawnType = doorSpawnConnections[i, 5];
			// Find the respective game state objects
			Data_Room thisRoom = GS.getRoomByIndex(thisRoomID);
			Data_Room otherRoom = GS.getRoomByIndex(otherRoomID);
			float thisSpawnPos = thisRoom.getDoorSpawnPosition(thisDoorSpawnIDrelativeToRoom);
			float otherSpawnPos = otherRoom.getDoorSpawnPosition(otherDoorSpawnIDrelativeToRoom);
			// Spawn both doors
			Data_Door thisDoor = spawnDoor(thisDoorSpawnType, thisRoom, thisSpawnPos);
			Data_Door otherDoor = spawnDoor(otherDoorSpawnType, otherRoom, otherSpawnPos);
			// Connect the doors
			thisDoor.connectTo(otherDoor);
		}
	}

	// Place a door within both game state and game space
	private Data_Door spawnDoor(int doorType, Data_Room parent, float xPos) {
		Transform parentTransform = parent.gameObj.transform;
		float parentWidth = parent.width;
		GameObject doorGameObj;
		// Differentiate by type
		switch(doorType) {
		case Data_Door.TYPE_LEFT_SIDE:
			doorGameObj = prefabFactory.spawnLeftSideDoor(parentTransform, parentWidth);
			// Overwrite any specified position with a virtual one outside of actual room constraints
			xPos = Data_Position.snapToGrid((HORIZONTAL_ROOM_MARGIN - parentWidth) / 2);
			break;
		default:
		case Data_Door.TYPE_BACK_DOOR:
			doorGameObj = prefabFactory.spawnBackDoor(parentTransform, xPos);
			break;
		case Data_Door.TYPE_RIGHT_SIDE:
			doorGameObj = prefabFactory.spawnRightSideDoor(parentTransform, parentWidth);
			xPos = Data_Position.snapToGrid((parentWidth - HORIZONTAL_ROOM_MARGIN) / 2);
			break;
		}
		// Initialize the door object and add it
		Data_Door doorObj = new Data_Door(GS.DOORS.Count, doorGameObj, doorType, parent, xPos);
		GS.addDoor(doorObj);
		return doorObj;
	}

	// Calls the game state to compute distances between all rooms and doors and checks if some are unreachable
	private void precomputeAllDistances() {
		// Precompute all-pairs shortest distances
		GS.precomputeAllPairsShortestDistances(DOOR_TRANSITION_COST);
		Debug.Assert(GS.allRoomsReachable);
	}

	// Places the next item in a random spot
	private Data_Item spawnNextItem() {
		int newItemIndex = GS.ITEMS.Count;

		// Calculate spawn position that isn't used yet
		Data_Position spawnPos = null;
		bool alreadyTakenPos = true;
		while(alreadyTakenPos) {
			alreadyTakenPos = false;
			spawnPos = GS.getRandomItemSpawn();
			foreach(Data_Item item in GS.ITEMS.Values) {
				if(spawnPos.RoomId == item.pos.RoomId && spawnPos.X == item.pos.X && spawnPos.Y == item.pos.Y) {
					alreadyTakenPos = true;
				}
			}
		}

		Data_Room parentRoom = GS.getRoomByIndex(spawnPos.RoomId);
		Transform parentRoomTransform = parentRoom.env.transform;
		Vector3 gameObjectPos = new Vector3(spawnPos.X, spawnPos.Y, -0.1f);

		// Spawn a new item from prefabs
		GameObject newItemObj = prefabFactory.spawnRandomItem(parentRoomTransform, gameObjectPos);
		Data_Item newItem = GS.addItem(newItemObj.name);
		newItem.INDEX = newItemIndex;

		// Place the new item
		newItem.updatePosition(parentRoom, spawnPos.X, spawnPos.Y);
		newItem.control.loadGameState(GS, newItemIndex);
		newItem.control.updateGameObjectPosition();

		return newItem;
	}

	// Randomly picks a new item for Toni to look for
	private int pickAnotherItemToSearchFor() {
		Data_Position pentagramCenter = new Data_Position(STARTING_ROOM_INDEX, RITUAL_PENTAGRAM_CENTER);
		// First, create a list of all items and weigh them with the cubed distance to the pentagram
		// Items that are already placed are weighed at 0
		float[] weights = new float[GS.ITEMS.Count];
		foreach(Data_Item item in GS.ITEMS.Values) {
			if(item.state != Data_Item.STATE_PLACED) {
				float dist = GS.getDistance(pentagramCenter, item.pos);
				weights[item.INDEX] = dist * dist * dist;
			} else {
				weights[item.INDEX] = 0;
			}
		}
		// Now pick a random number within the range and pick the corresponding item
		return AI_Util.pickRandomWeightedElement(weights);
	}

	// Updates the scribbles on the wall in the ritual room, indicating the next item to find
	private IEnumerator updateWallScribbles(float overTime) {
		SpriteRenderer rend = RitualRoomScribbles.GetComponent<SpriteRenderer>();
		Sprite newSprite = GS.getCurrentItem().control.BloodyScribble;

		// If the time given is zero or less, just replace the sprite and exit
		if(overTime <= 0) {
			rend.sprite = newSprite;
			yield break;
		}

		// Otherwise, first fade out the current scribbles
		float halfTime = overTime / 2;
		for(float timeLeft = halfTime; timeLeft > 0; timeLeft -= Time.deltaTime) {
			rend.color -= new Color(0, 0, 0, Time.deltaTime / halfTime);
			yield return null;
		}
		// Then update them and fade back in
		rend.sprite = newSprite;
		for(float timeLeft = halfTime; timeLeft > 0; timeLeft -= Time.deltaTime) {
			rend.color += new Color(0, 0, 0, Time.deltaTime / halfTime);
			yield return null;
		}
	}

	public void houseMixup(int deaths) {
		int itemCount = GS.numItemsPlaced;
		float evilness = Mathf.Max(1.0f, (float)itemCount * 0.5f + ((float)deaths * 0.2f));
		Control_GraphMixup.MixUpGraph(ref GS.HOUSE_GRAPH, (int)evilness);
		respawnAndConnectAllDoors();
		// Recalculate all distances
		precomputeAllDistances();
		// Update the monster's world model, too
		GS.getMonster().resetWorldModel(GS);
		GS.getMonster().control.nextDoorToGoThrough = null;
	}

	// This method is called when the New Game button is activated from the main menu
	void onNewGameSelect() {
		// Can only be clicked if not transformed into a monster yet.
		if(GS == null || (!newGameDisabled && GS.OVERALL_STATE < STATE_TRANSFORMATION)) {
			resetGameState();				// Reset the game state
			setAdditionalParameters();		// Refocus camera and such
			MainMenuCanvas.enabled = false;	// Dismiss the main menu
			GS.SUSPENDED = false;			// Continue playing
		}
	}

	// This method is called when the Continue button is activated from the main menu
	void onContinueSelect() {
		// If no game state has been loaded before, create a new one
		if(GS == null) {
			resetGameState();
			setAdditionalParameters();
		}
		// Then simply dismiss the main menu to continue playing
		MainMenuCanvas.enabled = false;
		GS.SUSPENDED = false;
	}

	// This method is called when the Exit button is activated from the main menu
	void onExitSelect() {
		// Save and exit
		Control_Persistence.saveToDisk(GS);
		Application.Quit();
	}
}
