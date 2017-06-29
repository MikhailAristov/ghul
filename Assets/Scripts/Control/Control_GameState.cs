using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// This is the controller class that manages the game state data
public class Control_GameState : MonoBehaviour {
    
	// In the exposition builds, it is always possible to reset the game state 
	// on restarting the game, and there is no continue button
	public bool InExpoMode;

	private Data_GameState GS;
	private Data_PlayerCharacter TONI;
	private Data_Monster MONSTER;

	// Before all items are placed for ritual
	public const int STATE_COLLECTION_PHASE = 0;
	// After all items are placed, but before old monster dies
	public const int STATE_TRANSFORMATION = 1;
	// The endgame: After Toni has become the new monster
	public const int STATE_MONSTER_PHASE = 2;
	// After suicidle has played out completely
	public const int STATE_MONSTER_DEAD = 3;

	public Control_MainMenu MainMenuControl;
	public Text MonsterDistanceText;
	public Control_CorpsePool CorpsePoolControl;
	public Control_Music JukeBox;
	public SpriteRenderer RitualRoomScribbles;
	public RectTransform CreditsCanvas;
	public TextAsset CreditsText;

	private Control_Camera MAIN_CAMERA_CONTROL;
	private Factory_PrefabController prefabFactory;
	private Factory_Graph graphFactory;

	// Global parameters
	private int RITUAL_ROOM_INDEX;
	private int TOTAL_NUMBER_OF_ROOMS;
	private float VERTICAL_ROOM_SPACING;
	private float HORIZONTAL_ROOM_MARGIN;
	private float DOOR_TRANSITION_COST;

	private int TOTAL_ITEMS_PLACED;
	private int RITUAL_ITEMS_REQUIRED;
	private float RITUAL_PENTAGRAM_CENTER;

	private float AUTOSAVE_FREQUENCY;
	private float NEXT_AUTOSAVE_IN;
	public bool newGameDisabled {
		get { return (GS != null && GS.OVERALL_STATE > STATE_COLLECTION_PHASE); }
	}
	public int currentChapter {
		get { return (GS != null && GS.OVERALL_STATE == STATE_COLLECTION_PHASE) ? (GS.numItemsPlaced + 1) : 0; }
	}

	// Use this for initialization
	void Awake() {
		RITUAL_ROOM_INDEX = Global_Settings.readInt("RITUAL_ROOM_INDEX");
		TOTAL_NUMBER_OF_ROOMS = Global_Settings.readInt("TOTAL_NUMBER_OF_ROOMS");
		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		HORIZONTAL_ROOM_MARGIN = Global_Settings.read("HORIZONTAL_ROOM_MARGIN");

		// A rough estimation of what "distance" lies between two sides of a door for the all-pairs shortest distance calculation
		DOOR_TRANSITION_COST = Global_Settings.read("CHARA_WALKING_SPEED") * Global_Settings.read("DOOR_TRANSITION_DURATION");

		TOTAL_ITEMS_PLACED = Global_Settings.readInt("TOTAL_NUMBER_OF_ITEMS_PLACED");
		RITUAL_ITEMS_REQUIRED = Global_Settings.readInt("RITUAL_ITEMS_REQUIRED");
		RITUAL_PENTAGRAM_CENTER = Global_Settings.read("RITUAL_PENTAGRAM_CENTER");
			
		AUTOSAVE_FREQUENCY = Global_Settings.read("AUTOSAVE_FREQUENCY");
		NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;

		// Force full screen
		Screen.SetResolution(640, 480, true);
	}

	void Start() {
		MAIN_CAMERA_CONTROL = Camera.main.GetComponent<Control_Camera>();
		// Initialize factories
		prefabFactory = GetComponent<Factory_PrefabController>();
		graphFactory = GetComponent<Factory_Graph>();
		// Load the game if possible
		if(!InExpoMode) {
			continueFromSavedGameState();
			if(GS != null) {
				setAdditionalParameters(newGame:false);
				GS.SUSPENDED = true; // Suspend the game while in the main menu initially
			}
		}
	}

	void FixedUpdate() {
		if(GS != null && !GS.SUSPENDED) {
			GS.DISTANCE_TONI_TO_MONSTER = GS.getDistance(TONI.pos, MONSTER.pos);
		}
	}

	// Update is called once per frame
	void Update() {
		if(GS == null) {
			return;
		}
		// Main menu handling
		bool escapeButtonPressed = Input.GetButton("Cancel");
		if(GS.SUSPENDED && escapeButtonPressed) {
			// Hide menu if suspended
			MainMenuControl.hide();
			GS.SUSPENDED = false;
			Input.ResetInputAxes();
		} else if(!GS.SUSPENDED && escapeButtonPressed) {
			// Show menu if not suspended
			GS.SUSPENDED = true;
			MainMenuControl.show();
			Input.ResetInputAxes();
		} else if(GS.SUSPENDED) {
			// If suspended and nothing pressed, just do nothing
			return;
		}

		// Timed autosave
		NEXT_AUTOSAVE_IN -= Time.deltaTime;
		if(NEXT_AUTOSAVE_IN <= 0.0f) {
			NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;
			Control_Persistence.saveToDisk(GS);
		}

		// Roll credits on demand in debug mode
		if(Input.GetButton("Roll Credits") && Debug.isDebugBuild && !CreditsCanvas.gameObject.activeSelf) {
			StartCoroutine(rollCredits());
		}

		// For the purpose of debugging, allow game reset hotkey
		if(Debug.isDebugBuild && Input.GetButtonDown("Reset Game State")) {
			GS.SUSPENDED = true;
			GS.OVERALL_STATE = STATE_COLLECTION_PHASE;
			startNewGame();
		}

		// Check whether monster and Toni have met
		if(!MONSTER.worldModel.hasMetToniSinceLastMilestone && GS.monsterSeesToni && MAIN_CAMERA_CONTROL.canSeeObject(MONSTER.gameObj, 0.4f)
			&& !TONI.control.isGoingThroughADoor && !MONSTER.control.isGoingThroughADoor) {
			MONSTER.worldModel.hasMetToniSinceLastMilestone = true;
		}

		switch(GS.OVERALL_STATE) {
		case STATE_COLLECTION_PHASE:
			updateDuringCollectionPhase();
			break;
		case STATE_TRANSFORMATION:
			// Switch to next state as soon as the monster dies
			if(GS.MONSTER_KILLED) {
				GS.OVERALL_STATE = STATE_MONSTER_PHASE;
				updateRoomLocksForToni(GS.ROOMS.Count, 0);
				MONSTER.control.setupEndgame();
				MONSTER.worldModel.hasMetToniSinceLastMilestone = false;
				GS.MONSTER_KILLED = false;
			}
			break;
		case STATE_MONSTER_PHASE:
			// The only way Toni can die in the endgame is by suicidle
			if(GS.TONI_KILLED) {
				GS.OVERALL_STATE = STATE_MONSTER_DEAD;
				Control_Persistence.saveToDisk(GS);
				// Roll the credits
				StartCoroutine(rollCredits(2));
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
		// Check if Toni meets the monster for the first time
		if(MONSTER.worldModel.hasMetToniSinceLastMilestone && !MONSTER.worldModel.hasMetToni) {
			// Update the monster's knowledge of meeting Toni
			MONSTER.worldModel.hasMetToni = true;
			// Make both characters face each other
			TONI.control.setSpriteFlip(TONI.atPos > MONSTER.atPos);
			MONSTER.control.setSpriteFlip(TONI.atPos < MONSTER.atPos);
			// Put both in equally long cooldown
			float duration = Global_Settings.read("ENCOUNTER_JINGLE_DURATION");
			TONI.control.halt();
			TONI.control.activateCooldown(duration);
			MONSTER.control.activateCooldown(duration);
			// Play a scary sound
			JukeBox.playEncounterJingle();
			// Make the monster attack as soon as its cooldown ends
			MONSTER.state = Control_Monster.STATE_PURSUING;
		}

		// Check if player died to trigger house mix-up
		if(GS.TONI_KILLED == true) {
			GS.TONI_KILLED = false;
			houseMixup(TONI.deaths);
		}

		// Check if all items have been placed
		if(GS.numItemsPlaced >= RITUAL_ITEMS_REQUIRED) {
			GS.OVERALL_STATE = STATE_TRANSFORMATION;
			triggerEndgame(GS.OVERALL_STATE, cutscene:true);
		} else { // Otherwise, check if wall scribbles need to be updated
			if(GS.ANOTHER_ITEM_PLEASE) {
				updateRoomLocksForToni(getAccessibleRoomCountInCurrentChapter(), GS.numItemsPlaced + 1);
				GS.indexOfSearchedItem = pickAnotherItemToSearchFor();
				GS.ANOTHER_ITEM_PLEASE = false;
				StartCoroutine(updateWallScribbles(GS.numItemsPlaced == 0 ? 0 : 1f));
			}
		}
	}

	// This method updates parameters after loading or resetting the game
	private void setAdditionalParameters(bool newGame) {
		// Train the camera on the main character
		MAIN_CAMERA_CONTROL.loadGameState(GS);
		if(!newGame) {
			MAIN_CAMERA_CONTROL.setFocusOn(TONI.pos);
		}

		// Initialize the jukebox
		JukeBox.loadGameState(GS);
		GetComponent<Control_Noise>().loadGameState(GS);

		// Reset the distance display
		StopCoroutine("updateToniMonsterDistanceDisplay");
		StartCoroutine("updateToniMonsterDistanceDisplay");
	}

	// This method loads the saved game state to memory
	private void continueFromSavedGameState() {
		// Load the game state from the disk
		GS = Control_Persistence.loadFromDisk<Data_GameState>();
		// If no game state has been found, stop right here
		if(GS == null) {
			return;
		}
		TONI = GS.getToni();
		MONSTER = GS.getMonster();

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
		TONI.fixObjectReferences(GS);
		TONI.control.loadGameState(GS);
		MONSTER.fixObjectReferences(GS);
		MONSTER.control.loadGameState(GS);
		MONSTER.fixObjectReferences(GS);

		// Placing the cadaver sprite in the location where it used to be (only if moved from the initial placing)
		Data_Position cadaverPos = GS.getCadaverPosition();
		if(cadaverPos != null) {
			// Place a monster corpse after suicidle, a human corpse otherwise
			if(GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_DEAD) {
				CorpsePoolControl.placeMonsterCorpse(GS.getRoomByIndex(cadaverPos.RoomId).env.gameObject, cadaverPos.asLocalVector(), GS.isCadaverFlipped());
			} else {
				CorpsePoolControl.placeHumanCorpse(GS.getRoomByIndex(cadaverPos.RoomId).env.gameObject, cadaverPos.asLocalVector(), GS.isCadaverFlipped());
			}
		}

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
			TONI.control.guideMonsterToniToIntruders();
		}
	}

	// The endgame scenario
	private void triggerEndgame(int overallState, bool cutscene = false) {
		// Place the cutscene if triggered in the regular gameplay, otherwise just update the sprites and locks
		if(cutscene) {
			StartCoroutine(playTransformationCutscene());
		} else {
			updateRoomLocksForToni(overallState == STATE_TRANSFORMATION ? 1 : GS.ROOMS.Count, RITUAL_ITEMS_REQUIRED);
			TONI.control.setupEndgame();
			MONSTER.control.setupEndgame();
		}
	}

	// This method initializes the game state back to default
	private void resetGameState() {
		// Update the weights before resetting the complete game state
		if(GS != null) {
			MONSTER.worldModel.playerParameters.updateWalkingDistanceWeights(TONI.roomHistory);
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
		// Hide all corpses
		CorpsePoolControl.resetAll();

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
		if(Debug.isDebugBuild || InExpoMode) {
			MONSTER.worldModel.hasMetToni = false;
			MONSTER.worldModel.hasMetToniSinceLastMilestone = false;
		}

		// Spawn all items
		for(int i = 0; i < TOTAL_ITEMS_PLACED; i++) {
			spawnNextItem();
		}
		// Setting this flag implicitly calls updateRoomLocksForToni() on the next Update() frame:
		GS.ANOTHER_ITEM_PLEASE = true;

		// Save the new game, overwriting the previous one
		Control_Persistence.saveToDisk(GS);
	}

	// Loads a fake prefab for the ritual room that already exists in the game space from the start
	private void initializeTheRitualRoom() {
		// Instantiate the ritual room itself
		GameObject ritualRoomGameObject = GameObject.Find("Ritual Room");
		Factory_PrefabRooms.RoomPrefab ritualRoomPrefab = prefabFactory.getRoomPrefabDetails("[prefab00]");
		Data_Room ritualRoom = new Data_Room(GS.ROOMS.Count, ritualRoomGameObject, ritualRoomPrefab);
		GS.addRoom(ritualRoom);
		// Load the environment
		ritualRoom.env.loadGameState(GS, RITUAL_ROOM_INDEX);
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
		Data_Room ritualRoom = GS.getRoomByIndex(RITUAL_ROOM_INDEX);

		// INITIALIZE PLAYER CHARACTER
		GS.setPlayerCharacter("PlayerCharacter");
		TONI = GS.getToni();
		TONI.updatePosition(ritualRoom, RITUAL_PENTAGRAM_CENTER, 0);
		TONI.resetRoomHistory();
		TONI.control.loadGameState(GS);

		// INITIALIZE MONSTER
		GS.setMonsterCharacter("Monster");
		MONSTER = GS.getMonster();
		MONSTER.updatePosition(GS.getRoomFurthestFrom(RITUAL_ROOM_INDEX), 0, 0);
		MONSTER.resetWorldModel(GS);
		MONSTER.control.loadGameState(GS);
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

	// Locks the rooms of the house, so that only the specified number of room is reachable from the ritual room
	// More rooms may be unlocked than specified IFF if the specified number of rooms does not contain any items
	private void updateRoomLocksForToni(int minRoomsToLeaveUnlocked, int reachableItemsNeeded) {
		//Debug.Log("unlocking " + minRoomsToLeaveUnlocked + " rooms so that " + reachableItemsNeeded + " items are reachable");
		// Find the highest degree of separation of any room from the ritual room
		int roomCount = GS.ROOMS.Values.Count, maxDegreeOfSeparation = 0;
		for(int i = 0; i < roomCount; i++) {
			if(maxDegreeOfSeparation < GS.separationBetweenTwoRooms[RITUAL_ROOM_INDEX, i]) {
				maxDegreeOfSeparation = GS.separationBetweenTwoRooms[RITUAL_ROOM_INDEX, i];
			}
		}
		// Count the items currently in each room
		int[] itemCountInRoom = new int[roomCount];
		foreach(Data_Item item in GS.ITEMS.Values) {
			itemCountInRoom[item.pos.RoomId]++;
		}
		// Re-lock all rooms
		int unlockedRoomCount = 0;
		foreach(Data_Room room in GS.ROOMS.Values) {
			room.ToniCannotEnter = true;
		}
		// Unlock the minimum number of rooms, so that the minimum required items are reachable
		int reachableItemsCount = 0;
		for(int curSep = 0; curSep <= maxDegreeOfSeparation; curSep++) {
			foreach(Data_Room room in GS.ROOMS.Values) {
				if(GS.separationBetweenTwoRooms[RITUAL_ROOM_INDEX, room.INDEX] == curSep) {
					room.ToniCannotEnter = false;
					unlockedRoomCount += 1;
					reachableItemsCount += itemCountInRoom[room.INDEX];
					//Debug.Log("unlocked room " + room);
					// Check if the minimum required number of rooms has been unlocked, and quit if that's the case
					if(unlockedRoomCount >= minRoomsToLeaveUnlocked && reachableItemsCount >= reachableItemsNeeded) {
						Debug.Log("Unlocked " + unlockedRoomCount + " rooms so that " + reachableItemsCount + " items are reachable");
						return;
					} 
				}
			}
		}
	}

	// Returns how many doors should be unlocked at any given chapter
	private int getAccessibleRoomCountInCurrentChapter() {
		switch(Mathf.Clamp(GS.numItemsPlaced, 0, 4)) {
		case 0:
			return Global_Settings.readInt("ROOMS_UNLOCKED_AT_ZERO_ITEMS");
		case 1:
			return Global_Settings.readInt("ROOMS_UNLOCKED_AFTER_ONE_ITEM");
		case 2:
			return Global_Settings.readInt("ROOMS_UNLOCKED_AFTER_TWO_ITEMS");
		default:
			return GS.ROOMS.Count;
		}
	}

	// Places the next item in a random spot
	private Data_Item spawnNextItem() {
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

		// Place the new item
		newItem.updatePosition(parentRoom, spawnPos.X, spawnPos.Y);
		newItem.control.loadGameState(GS, newItem.INDEX);
		newItem.control.updateGameObjectPosition();

		return newItem;
	}

	// Randomly picks a new item for Toni to look for
	private int pickAnotherItemToSearchFor() {
		Data_Position pentagramCenter = new Data_Position(RITUAL_ROOM_INDEX, RITUAL_PENTAGRAM_CENTER);
		// First, create a list of all items and weigh them with the cubed distance to the pentagram
		// Items that are already placed or are in unreachable rooms are weighed at 0
		float[] weights = new float[GS.ITEMS.Count];
		foreach(Data_Item item in GS.ITEMS.Values) {
			if(item.state != Data_Item.STATE_PLACED && !item.isIn.ToniCannotEnter) {
				float dist = GS.getDistance(pentagramCenter, item.pos);
				weights[item.INDEX] = dist * dist * dist;
			} else {
				weights[item.INDEX] = 0;
			}
			//Debug.Log(item + "'s index is " + item.INDEX + " and weight is " + weights[item.INDEX]);
		}
		// Now pick a random number within the range and pick the corresponding item
		return AI_Util.pickRandomWeightedElement(weights);
	}

	// Updates the scribbles on the wall in the ritual room, indicating the next item to find
	private IEnumerator updateWallScribbles(float overTime) {
		// If the time given is zero or less, just replace the sprite and exit
		if(overTime <= 0) {
			RitualRoomScribbles.sprite = GS.getCurrentItem().control.BloodyScribble;
			yield break;
		}

		// Otherwise, first fade out the current scribbles
		float timeStep = overTime / 100;
		float waitUntil = Time.timeSinceLevelLoad;
		while(RitualRoomScribbles.color.a > 0.001f) {
			RitualRoomScribbles.color -= new Color(0, 0, 0, 1f/50f);
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);
		}
		// Then update them and fade back in
		RitualRoomScribbles.sprite = GS.getCurrentItem().control.BloodyScribble;
		while(RitualRoomScribbles.color.a < 0.999f) {
			RitualRoomScribbles.color += new Color(0, 0, 0, 1f/50f);
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);
		}
	}

	private void houseMixup(int deaths) {
		int itemCount = GS.numItemsPlaced;
		float evilness = Mathf.Max(1.0f, (float)itemCount * 0.5f + ((float)deaths * 0.2f));
		Control_GraphMixup.MixUpGraph(ref GS.HOUSE_GRAPH, (int)evilness);
		respawnAndConnectAllDoors();
		// Recalculate all distances and re-lock the rooms
		precomputeAllDistances();
		updateRoomLocksForToni(getAccessibleRoomCountInCurrentChapter(), GS.numItemsPlaced + 1);
		// Update the monster's world model, too
		MONSTER.resetWorldModel(GS);
		MONSTER.control.nextDoorToGoThrough = null;
	}

	// This method is called when the New Game button is activated from the main menu
	public void startNewGame() {
		// Can only be clicked if not transformed into a monster yet
		if(!newGameDisabled) {
			resetGameState();				// Reset the game state
			setAdditionalParameters(newGame:true);	// Refocus camera and such
			StartCoroutine(playOpeningCutscene());
			GS.SUSPENDED = false;			// Continue playing
		}
	}

	// This method is called when the Continue button is activated from the main menu
	public void continueOldGame() {
		// If no game state has been loaded before, create a new one
		if(GS == null) {
			resetGameState();
			setAdditionalParameters(newGame:true);
		}
		GS.SUSPENDED = false;
	}

	// This method is called when the Exit button is activated from the main menu
	public void quitGame() {
		// Save and exit
		if(GS != null) {
			Control_Persistence.saveToDisk(GS);
		}
		Screen.SetResolution(640, 480, false);
		Application.Quit();
		Debug.Log("Quitting game...");
	}

	// Continuously recalculates the proximity of monster to chara
	private IEnumerator updateToniMonsterDistanceDisplay() {
		while(true) {
			if(GS != null && !GS.SUSPENDED) {
				if(Debug.isDebugBuild) {
					MonsterDistanceText.text = string.Format("{0:0.0} m", GS.DISTANCE_TONI_TO_MONSTER);
				} else if(MonsterDistanceText.text.Length > 0) {
					MonsterDistanceText.text = "";
				}
			}
			yield return new WaitForSeconds(0.2f);
		}
	}

	// Rolls the credits over the screen
	private IEnumerator rollCredits(float delay = 0) {
		CreditsCanvas.gameObject.SetActive(true);
		// Wait a couple of seconds
		float waitUntil = Time.timeSinceLevelLoad + delay;
		yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		// Adjust the position of the credits text
		Transform ct = CreditsCanvas.transform;
		CreditsCanvas.GetComponent<Text>().text = CreditsText.text;
		float textHeight = LayoutUtility.GetPreferredHeight(CreditsCanvas);
		CreditsCanvas.sizeDelta = new Vector2(LayoutUtility.GetPreferredWidth(CreditsCanvas), textHeight);
		ct.localPosition = new Vector2(0, -(240 + textHeight / 2));
		// Start scrolling the text upwards
		float timeStep = 0.01f, scrollSpeed = 0.7f;
		while(ct.localPosition.y < (240 + textHeight / 2)) {
			if(!GS.SUSPENDED) {
				ct.Translate(0, scrollSpeed * timeStep, 0);
			}
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		}
		CreditsCanvas.gameObject.SetActive(false);
	}

	// Play the opening cutscene of the game
	private IEnumerator playOpeningCutscene() {
		// Put Toni on cooldown
		TONI.control.activateCooldown(3f);
		// Focus camera on the blood scribbles
		MAIN_CAMERA_CONTROL.setPanningSpeedFactor(0.25f);
		MAIN_CAMERA_CONTROL.setFocusOn(new Data_Position(RITUAL_ROOM_INDEX, RitualRoomScribbles.transform.localPosition.x), snapToPosition:true);
		// Place the skeletal corpse in the ritual room
		CorpsePoolControl.placeHumanSkeleton(RitualRoomScribbles.transform.parent.gameObject, new Vector2(1.2375f, 0), false);
		// Wait until Toni comes out of the cooldown
		yield return new WaitUntil(() => TONI.cooldown <= 0);
		// Make it look like Toni has just walked in through the right door (next to the pentagram)
		TONI.control.enterTheHouse();
		// Wait some more
		yield return new WaitUntil(() => TONI.cooldown <= 0);
		// Swing the camera over to him
		TONI.control.activateCooldown(2.3f);
		MAIN_CAMERA_CONTROL.setFocusOn(TONI.pos);
		// Wait until the cooldown wears off before resetting the camera panning speed
		yield return new WaitUntil(() => TONI.cooldown <= 0);
		MAIN_CAMERA_CONTROL.setPanningSpeedFactor(1f);
	}

	// Play the transformation cutscene after triggering the endgame
	private IEnumerator playTransformationCutscene() {
		updateRoomLocksForToni(1, RITUAL_ITEMS_REQUIRED);
		// Toni walks to the center of the pentagram and looks left
		float cooldown = (Global_Settings.read("RITUAL_PLACEMENT_MARGIN") * 2f) / Global_Settings.read("CHARA_WALKING_SPEED");
		TONI.control.activateCooldown(cooldown);
		TONI.control.moveTo(RITUAL_PENTAGRAM_CENTER);
		yield return new WaitUntil(() => TONI.cooldown <= 0);
		TONI.control.setSpriteFlip(true);
		// Trigger the transformation 
		cooldown = Global_Settings.read("TRANSFORMATION_DURATION");
		TONI.control.activateCooldown(cooldown + 1f);
		TONI.control.transformIntoMonster();
		// Have the monster enter after a couple seconds into the transformation
		float waitUntil = Time.timeSinceLevelLoad + 17f;
		yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);
		Data_Door monsterEntry = GS.getRoomByIndex(RITUAL_ROOM_INDEX).getDoorAtSpawn(1);
		MONSTER.control.teleportToRitualRoom(monsterEntry);
		yield return new WaitUntil(() => MONSTER.cooldown <= 0);
		MONSTER.control.setSpriteFlip(false);
		MONSTER.control.activateCooldown(60f);
		yield return new WaitUntil(() => TONI.cooldown <= 0);
		// While the camera is on the monster, switch the Toni sprite
		MONSTER.control.activateCooldown(0);
		TONI.control.activateCooldown(1f);
		MAIN_CAMERA_CONTROL.setFocusOn(MONSTER.pos);
		yield return new WaitUntil(() => TONI.cooldown <= 0);
		TONI.control.setupEndgame();
		// Swing the camera back to Toni
		MAIN_CAMERA_CONTROL.setFocusOn(TONI.pos);
		Control_Persistence.saveToDisk(GS);
	}
}
