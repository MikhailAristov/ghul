using UnityEngine;
using System;
using System.Collections.Generic;

public class Data_GameState : MonoBehaviour {

    private Dictionary<string, float> SETTINGS;

    private SortedList<int, Data_Room> ROOMS;
    private SortedList<int, Data_Door> DOORS;

    public Data_Character PLAYER_CHARACTER;
    private Camera MAIN_CAMERA;

    // Initialization at the start of the game
    void Start () {
        // Initialize game settings
        SETTINGS = new Dictionary<string, float>();
        SETTINGS.Add("SCREEN_SIZE_HORIZONTAL", 6.4f);   // 640px
        SETTINGS.Add("SCREEN_SIZE_VERTICAL", 4.8f);     // 480px
        SETTINGS.Add("VERTICAL_ROOM_SPACING", 5.0f);    // must be bigger than SCREEN_SIZE_VERTICAL

        SETTINGS.Add("HORIZONTAL_ROOM_MARGIN", 0.9f);   // prevents movement to screen edge past the margin
        SETTINGS.Add("HORIZONTAL_DOOR_WIDTH", 1.35f);
        SETTINGS.Add("MARGIN_DOOR_ENTRANCE", 0.6f);     // How close a character's center of mass must be to the door's center to use it

        SETTINGS.Add("CHARA_WALKING_SPEED", 5.0f);
        SETTINGS.Add("CHARA_RUNNING_SPEED", 10.0f);
        SETTINGS.Add("CAMERA_PANNING_SPEED", 9.0f);

        // INITIALIZE ROOMS
        int ir = 0; ROOMS = new SortedList<int, Data_Room>();
        foreach(GameObject r in GameObject.FindGameObjectsWithTag("Room"))
        {
            Data_Room o = new Data_Room(ir, r);
            ROOMS.Add(ir, o);
            ir += 1;
        }

        // INITIALIZE DOORS
        int id = 0; DOORS = new SortedList<int, Data_Door>();
        foreach (GameObject d in GameObject.FindGameObjectsWithTag("Door"))
        {
            Data_Door o = new Data_Door(id, d);
            DOORS.Add(id, o);
            id += 1;
        }

        // ADD DOORS TO ROOMS
        ROOMS[0].addDoor(DOORS[0], DOORS[0].gameObj.transform.position.x);
        ROOMS[0].addDoor(DOORS[1], DOORS[1].gameObj.transform.position.x);
        ROOMS[0].addDoor(DOORS[2], DOORS[2].gameObj.transform.position.x);
        ROOMS[1].addDoor(DOORS[3], DOORS[3].gameObj.transform.position.x);
        ROOMS[1].addDoor(DOORS[4], DOORS[4].gameObj.transform.position.x);
        ROOMS[2].addDoor(DOORS[5], DOORS[5].gameObj.transform.position.x);

        // CONNECT DOORS
        DOORS[0].connectTo(DOORS[5]);
        DOORS[1].connectTo(DOORS[4]);
        DOORS[2].connectTo(DOORS[3]);

        // CONNECT GAME OBJECTS TO GAME STATE
        foreach(Data_Room r in ROOMS.Values)
        {
            // Load rooms
            r.env.loadGameState(this, r.INDEX);
            Debug.Log("Room #" + r + " contains " + r.DOORS.Count + " doors:");
            // Load doors
            foreach(Data_Door d in r.DOORS)
            {
                Debug.Log("  Door #" + d + " at position " + d.atPos + " in Room #" + r+ " connects to Door #" + d.connectsTo + " in Room #" + d.connectsTo.isIn);
            }
        }

        // INITIALIZE PLAYER CHARACTER
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        PLAYER_CHARACTER = new Data_Character("CHARA", playerObj);
        PLAYER_CHARACTER.moveToRoom(ROOMS[0]); // put CHARA in the starting room
        PLAYER_CHARACTER.control.loadGameState(this);
        InvokeRepeating("updatePlayerCharacterPosition", 0.0f, 10.0f); // update CHARA's position in the game state every 10 seconds

        // FOCUS CAMERA ON PLAYER CHARACTER
        Control_Camera mainCameraControl = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();
        mainCameraControl.loadGameState(this);
        mainCameraControl.setFocusOn(playerObj, ROOMS[0].env);
    }

    // Update is called once per frame
    void Update () {
        return;
	}

    // Update the player character's position
    public void updatePlayerCharacterPosition()
    {
        this.PLAYER_CHARACTER.updatePosition(this.PLAYER_CHARACTER.gameObj.transform.position.x);
        Debug.Log(PLAYER_CHARACTER + " is in room #" + PLAYER_CHARACTER.isIn + " at position " + PLAYER_CHARACTER.pos);
    }

    // Returns the value of a game setting
    public float getSetting(string Name)
    {
        if (this.SETTINGS.ContainsKey(Name))
        {
            return SETTINGS[Name];
        }
        else
        {
            throw new System.ArgumentException("Setting " + Name + " is not defined", "original");
        }
    }

    // Returns a Room object to a given index, if it exists
    public Data_Room getRoomByIndex(int I)
    {
        if(ROOMS.ContainsKey(I))
        {
            return ROOMS[I];
        }
        else
        {
            throw new System.ArgumentException("There is no room #" + I, "original");
        }
    }
}
