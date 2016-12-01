using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// This is the controller class that manages the game state data
public class Control_GameState : MonoBehaviour {

    private Data_GameState GS;

    private const string FILENAME_SAVE_RESETTABLE = "save1.dat";
    private const string FILENAME_SAVE_PERMANENT  = "save2.dat";

    private Control_Camera MAIN_CAMERA_CONTROL;

    // Use this for initialization
    void Start ()
    {
        // Initialize game settings
        GS = new Data_GameState();
        GS.loadDefaultSetttings();

        // INITIALIZE ROOMS
        GS.addRoom(GameObject.Find("Room00"));
        GS.addRoom(GameObject.Find("Room01"));
        GS.addRoom(GameObject.Find("Room02"));
        GS.addRoom(GameObject.Find("Room03"));

        // INITIALIZE DOORS
        GS.addDoor(GameObject.Find("Door0-1"), 0);
        GS.addDoor(GameObject.Find("Door0-2"), 0);
        GS.addDoor(GameObject.Find("Door0-3"), 0);
        GS.addDoor(GameObject.Find("Door1-1"), 1);
        GS.addDoor(GameObject.Find("Door1-2"), 1);
        GS.addDoor(GameObject.Find("Door2-1"), 2);
        GS.addDoor(GameObject.Find("Door3-1"), 3);
        GS.addDoor(GameObject.Find("Door3-2"), 3);

        // CONNECT DOORS
        GS.connectTwoDoors(0, 5);
        GS.connectTwoDoors(1, 3);
        GS.connectTwoDoors(2, 6);
        GS.connectTwoDoors(4, 7);
        // TODO: Must ensure that side doors never connect to the opposite sides, or it will look weird and cause trouble with room transitions

        // LINK GAME OBJECTS TO GAME STATE
        foreach (Data_Room r in GS.ROOMS.Values)
        {
            // Load rooms
            r.env.loadGameState(GS, r.INDEX);
            Debug.Log("Room #" + r + " contains " + r.DOORS.Count + " doors:");
            // Load doors
            foreach (Data_Door d in r.DOORS)
            {
                Debug.Log("  Door #" + d + " at position " + d.atPos + " in Room #" + r + " connects to Door #" + d.connectsTo + " in Room #" + d.connectsTo.isIn);
            }
        }

        // INITIALIZE PLAYER CHARACTER
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        GS.setPlayerCharacter("CHARA", playerObj);
        GS.getCHARA().moveToRoom(GS.getRoomByIndex(0)); // put CHARA in the starting room
        GS.getCHARA().control.loadGameState(GS);
        //InvokeRepeating("updatePlayerCharacterPosition", 0.0f, 10.0f); // update CHARA's position in the game state every 10 seconds

        // FOCUS CAMERA ON PLAYER CHARACTER
        MAIN_CAMERA_CONTROL = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();
        MAIN_CAMERA_CONTROL.loadGameState(GS);
        MAIN_CAMERA_CONTROL.setFocusOn(playerObj, GS.getCHARA().isIn.env);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Saves the current game state to disk
    public void saveToDisk()
    {
        // Set the save file paths
        string resettableFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_RESETTABLE;
        string permanentFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_PERMANENT;

        // Prepare writing file
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(resettableFilePath);

        // Write the game state to file and close it
        // bf.Serialize(file, GS);
        file.Close();
    }
}
