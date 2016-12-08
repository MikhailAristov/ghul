using UnityEngine;

// This is the controller class that manages the game state data
public class Control_GameState : MonoBehaviour {
    
    private Data_GameState GS;

    private Control_Camera MAIN_CAMERA_CONTROL;
    public Canvas MAIN_MENU_CANVAS;

    private float AUTOSAVE_FREQUENCY;
    private float NEXT_AUTOSAVE_IN;

    // Use this for initialization
    void Start ()
    {
        MAIN_CAMERA_CONTROL = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();

        continueFromSavedGameState();
        setAdditionalParameters();
        GS.SUSPENDED = true; // Suspend the game while in the main menu initially
    }

    // Update is called once per frame
    void Update()
    {
        if (GS.SUSPENDED) { return; }

        // Timed autosave
        NEXT_AUTOSAVE_IN -= Time.deltaTime;
        if (NEXT_AUTOSAVE_IN <= 0.0f)
        {
            NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;
            Data_GameState.saveToDisk(GS);
        }

        // Open main menu if the player presses Esc
        if (Input.GetButton("Cancel"))
        {
            GS.SUSPENDED = true;
            MAIN_MENU_CANVAS.enabled = true;
        }
    }

    // This method updates parameters after loading or resetting the game
    private void setAdditionalParameters()
    {
        // Train the camera on the main character
        MAIN_CAMERA_CONTROL.loadGameState(GS);
        MAIN_CAMERA_CONTROL.setFocusOn(GS.getCHARA().pos);

        // Initialize autosave
        AUTOSAVE_FREQUENCY = GS.getSetting("AUTOSAVE_FREQUENCY");
        NEXT_AUTOSAVE_IN = AUTOSAVE_FREQUENCY;
    }

    // This method loads the saved game state to memory
    private void continueFromSavedGameState()
    {
        // Load the game state from the disk
        GS = Data_GameState.loadFromDisk();
        // If no game state has been found, initialize it instead
        if(GS == null) { resetGameState(); return; }
        GS.loadDefaultSetttings();

		// Fix all item references
		foreach (Data_Item i in GS.ITEMS.Values) {
			i.fixObjectReferences(GS);
		}
		// Fix all item spot references
		foreach (Data_ItemSpot iSpot in GS.ITEM_SPOTS.Values) {
			iSpot.fixObjectReferences(GS);
		}

        // Fix door object references first, because Data_Room.fixObjectReferences() relies on them being set
        foreach (Data_Door d in GS.DOORS.Values) {
            d.fixObjectReferences(GS);
        }
        // Now fix all the room object references, including the door and item spot assignments
        foreach (Data_Room r in GS.ROOMS.Values) {
            r.fixObjectReferences(GS);
            r.env.loadGameState(GS, r.INDEX); // While we are on it, load game state into room environment scripts 
        }

        // Lastly, fix the character, cadaver and monster object references
        GS.getCHARA().fixObjectReferences(GS);
        GS.getCHARA().control.loadGameState(GS);
        GS.getMonster().fixObjectReferences(GS);
        GS.getMonster().control.loadGameState(GS);
		GS.getCadaver().fixObjectReferences(GS);

		//Placing the cadaver and item sprites in the location they used to be
		Data_Cadaver cadaver = GS.getCadaver();
		Vector3 positionOfCadaver = new Vector3 (cadaver.atPos, cadaver.isIn.gameObj.transform.position.y - 1.55f, 0);
		cadaver.gameObj.transform.Translate(positionOfCadaver - cadaver.gameObj.transform.position);
		cadaver.updatePosition(cadaver.isIn, cadaver.atPos);




    }

    // This method initializes the game state back to default
    private void resetGameState()
    {
        // Initialize game settings
        GS = new Data_GameState();
        GS.loadDefaultSetttings();

        // INITIALIZE ROOMS
        GS.addRoom("Room00");
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

		// INITIALIZE ITEMS
		GS.addItem("Item01");
		GS.addItem("Item02");
		GS.addItem("Item03");
		GS.addItem("Item04");
		GS.addItem("Item05");
		GS.addItem("Item06");
		GS.addItem("Item07");
		GS.addItem("Item08");

		// REMOVE ANY PRE-EXISTING ITEMS FROM ITEM SPOTS
		GS.removeItemFromSpot(0);
		GS.removeItemFromSpot(1);
		GS.removeItemFromSpot(2);
		GS.removeItemFromSpot(3);
		GS.removeItemFromSpot(4);
		GS.removeItemFromSpot(5);
		GS.removeItemFromSpot(6);
		GS.removeItemFromSpot(7);
		GS.removeItemFromSpot(8);

		// PUT ITEMS INTO ITEM SPOTS
		// TODO: Randomize the placement
		GS.placeItemInSpot(0,0);
		GS.placeItemInSpot(1,1);
		GS.placeItemInSpot(2,2);
		GS.placeItemInSpot(3,3);
		GS.placeItemInSpot(4,4);
		GS.placeItemInSpot(5,5);
		GS.placeItemInSpot(6,6);
		GS.placeItemInSpot(7,7);
		// last spot empty for now

        // Load game state into room environment scripts
        foreach (Data_Room r in GS.ROOMS.Values) {
            r.env.loadGameState(GS, r.INDEX);
        }

        // INITIALIZE PLAYER CHARACTER
        GS.setPlayerCharacter("PlayerCharacter");
        GS.getCHARA().updatePosition(GS.getRoomByIndex(0), 0); // default: starting position is center of pentagram
        GS.getCHARA().control.loadGameState(GS);

        // INITIALIZE MONSTER
        GS.setMonsterCharacter("Monster");
        GS.getMonster().updatePosition(GS.getRoomByIndex(1), GS.getMonster().gameObj.transform.position.x);
		GS.getMonster().control.loadGameState(GS);
		GS.getMonster().setForbiddenRoomIndex(GS.getCHARA().isIn.INDEX);

		// INITIALIZE CADAVER
		GS.setCadaverCharacter("Cadaver");
		GS.getCadaver().updatePosition(GS.getRoomByIndex (0), GS.getCadaver().gameObj.transform.position.x);
    	
		// Placing the cadaver sprite out of sight
		Vector3 nirvana = new Vector3 (-100, 0, 0);
		GS.getCadaver().gameObj.transform.Translate(nirvana - GS.getCadaver().gameObj.transform.position);
	}

    // This method is called when the New Game button is activated from the main menu
    void onNewGameSelect()
    {
        resetGameState();                   // Reset the game state
        setAdditionalParameters();          // Refocus camera and such
        Data_GameState.saveToDisk(GS);      // Save the new game state to disk
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
