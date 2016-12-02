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

        // Fix door object references first, because Data_Room.fixObjectReferences() relies on them being set
        foreach (Data_Door d in GS.DOORS.Values) {
            d.fixObjectReferences(GS);
        }
        // Now fix all the room object references, including the door assignments
        foreach (Data_Room r in GS.ROOMS.Values) {
            r.fixObjectReferences(GS);
            r.env.loadGameState(GS, r.INDEX); // While we are on it, load game state into room environment scripts 
        }
        // Lastly, fix the character object references
        GS.getCHARA().fixObjectReferences(GS);
        GS.getCHARA().control.loadGameState(GS);
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

        // Load game state into room environment scripts
        foreach (Data_Room r in GS.ROOMS.Values) {
            r.env.loadGameState(GS, r.INDEX);
        }

        // INITIALIZE PLAYER CHARACTER
        GS.setPlayerCharacter("PlayerCharacter");
        GS.getCHARA().updatePosition(GS.getRoomByIndex(0), GS.getCHARA().gameObj.transform.position.x);
        GS.getCHARA().control.loadGameState(GS);
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
