using UnityEngine;

// This is the controller class that manages the game state data
public class Control_GameState : MonoBehaviour {
    
    private Data_GameState GS;

    private Control_Camera MAIN_CAMERA_CONTROL;

    private float AUTOSAVE_FREQUENCY;
    private float NEXT_AUTOSAVE_TIME;

    // Use this for initialization
    void Start ()
    {
        if(true)
        {
            continueFromSavedGameState();
        } else
        {
            resetGameState();
        }

        // Train the camera on the main character
        MAIN_CAMERA_CONTROL = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();
        MAIN_CAMERA_CONTROL.loadGameState(GS);
        MAIN_CAMERA_CONTROL.setFocusOn(GS.getCHARA().pos);

        // Initialize autosave
        AUTOSAVE_FREQUENCY = GS.getSetting("AUTOSAVE_FREQUENCY");
        NEXT_AUTOSAVE_TIME = Time.timeSinceLevelLoad;
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

    // Update is called once per frame
    void Update()
    {   
        // Timed autosave
        if(Time.timeSinceLevelLoad > NEXT_AUTOSAVE_TIME) {
            NEXT_AUTOSAVE_TIME = Time.timeSinceLevelLoad + AUTOSAVE_FREQUENCY;
            Data_GameState.saveToDisk(GS);
        }
    }
}
