using UnityEngine;
using System.Collections.Generic;

public class Data_GameState : MonoBehaviour {

    private Dictionary<string, float> SETTINGS;

    private SortedList<int, Data_Room> ROOMS;
    private SortedList<int, Data_Door> DOORS;

    public Data_Character PLAYER_CHARACTER;
	public Data_Monster MONSTER;
    private Control_Camera MAIN_CAMERA_CONTROL;

    // Initialization at the start of the game
    void Start () {
        // Initialize game settings
        SETTINGS = new Dictionary<string, float>();
        SETTINGS.Add("SCREEN_SIZE_HORIZONTAL", 6.4f);   // 640px
        SETTINGS.Add("SCREEN_SIZE_VERTICAL", 4.8f);     // 480px
        SETTINGS.Add("VERTICAL_ROOM_SPACING", -5.0f);    // must be bigger than SCREEN_SIZE_VERTICAL

        SETTINGS.Add("HORIZONTAL_ROOM_MARGIN", 0.9f);   // prevents movement to screen edge past the margin
        SETTINGS.Add("HORIZONTAL_DOOR_WIDTH", 1.35f);
        SETTINGS.Add("MARGIN_DOOR_ENTRANCE", 0.6f);     // How close a character's center of mass must be to the door's center to use it

        SETTINGS.Add("CHARA_WALKING_SPEED", 5.0f);
        SETTINGS.Add("CHARA_RUNNING_SPEED", 8.0f);
		SETTINGS.Add("MONSTER_WALKING_SPEED", 5.2f);
		SETTINGS.Add("MONSTER_SLOW_WALKING_SPEED", 2.5f); // when the monster randomly walks around
		SETTINGS.Add("MONSTER_KILL_RADIUS", 1.0f); // when the player gets this close to the monster, he dies.
		SETTINGS.Add("TIME_TO_REACT", 0.35f); // if the player escapes the monster's radius within this timeframe, he isn't killed.

        // Stamina range: 0.0 .. 1.0; increments are applied per second
        SETTINGS.Add("RUNNING_STAMINA_LOSS", -0.2f);   // Must be negative
        SETTINGS.Add("WALKING_STAMINA_GAIN", 0.1f);    
        SETTINGS.Add("STANDING_STAMINA_GAIN", 0.4f);

        SETTINGS.Add("DOOR_COOLDOWN_DURATION", 0.3f);
        SETTINGS.Add("CAMERA_PANNING_SPEED", 9.0f);

		SETTINGS.Add("TOTAL_DEATH_DURATION", 3.0f); // When deathDuration of Data_Character reaches this value the player resets to the starting room

        // INITIALIZE ROOMS
        ROOMS = new SortedList<int, Data_Room>();
        ROOMS.Add(0, new Data_Room(0, GameObject.Find("Room00")));
        ROOMS.Add(1, new Data_Room(1, GameObject.Find("Room01")));
        ROOMS.Add(2, new Data_Room(2, GameObject.Find("Room02")));
        ROOMS.Add(3, new Data_Room(3, GameObject.Find("Room03")));

        // INITIALIZE DOORS
        DOORS = new SortedList<int, Data_Door>();
        DOORS.Add(0, new Data_Door(0, GameObject.Find("Door0-1")));
        DOORS.Add(1, new Data_Door(1, GameObject.Find("Door0-2")));
        DOORS.Add(2, new Data_Door(2, GameObject.Find("Door0-3")));
        DOORS.Add(3, new Data_Door(3, GameObject.Find("Door1-1")));
        DOORS.Add(4, new Data_Door(4, GameObject.Find("Door1-2")));
        DOORS.Add(5, new Data_Door(5, GameObject.Find("Door2-1")));
        DOORS.Add(6, new Data_Door(6, GameObject.Find("Door3-1")));
        DOORS.Add(7, new Data_Door(7, GameObject.Find("Door3-2")));

        // ADD DOORS TO ROOMS
        ROOMS[0].addDoor(DOORS[0], DOORS[0].gameObj.transform.position.x);
        ROOMS[0].addDoor(DOORS[1], DOORS[1].gameObj.transform.position.x);
        ROOMS[0].addDoor(DOORS[2], DOORS[2].gameObj.transform.position.x);
        ROOMS[1].addDoor(DOORS[3], DOORS[3].gameObj.transform.position.x);
        ROOMS[1].addDoor(DOORS[4], DOORS[4].gameObj.transform.position.x);
        ROOMS[2].addDoor(DOORS[5], DOORS[5].gameObj.transform.position.x);
        ROOMS[3].addDoor(DOORS[6], DOORS[6].gameObj.transform.position.x);
        ROOMS[3].addDoor(DOORS[7], DOORS[7].gameObj.transform.position.x);

        // CONNECT DOORS
        DOORS[0].connectTo(DOORS[5]);
        DOORS[1].connectTo(DOORS[3]);
        DOORS[2].connectTo(DOORS[6]);
        DOORS[4].connectTo(DOORS[7]);
        // TODO: Must ensure that side doors never connect to the opposite sides, or it will look weird and cause trouble with room transitions

        // LINK GAME OBJECTS TO GAME STATE
        foreach (Data_Room r in ROOMS.Values)
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
		PLAYER_CHARACTER.startingRoom = ROOMS[0];
        PLAYER_CHARACTER.control.loadGameState(this);
        InvokeRepeating("updatePlayerCharacterPosition", 0.0f, 10.0f); // update CHARA's position in the game state every 10 seconds
		InvokeRepeating("updateMonsterPosition", 5.0f, 10.0f);

		// INITIALIZE MONSTER
		GameObject monsterObj = GameObject.FindGameObjectWithTag("Monster");
		MONSTER = new Data_Monster("MONSTER", monsterObj);
		MONSTER.moveToRoom(ROOMS[2]); // TODO: Finding a proper place for the monster to spawn.
		MONSTER.control.loadGameState(this);

        // FOCUS CAMERA ON PLAYER CHARACTER
        MAIN_CAMERA_CONTROL = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();
        MAIN_CAMERA_CONTROL.loadGameState(this);
        MAIN_CAMERA_CONTROL.setFocusOn(playerObj, PLAYER_CHARACTER.isIn.env);
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

	// Update the monster's position
	public void updateMonsterPosition() {
		this.MONSTER.updatePosition(this.MONSTER.gameObj.transform.position.x);
		Debug.Log (MONSTER + " is in room #" + MONSTER.isIn + " at position " + MONSTER.pos);
	}

    // Returns the value of a game setting
    public float getSetting(string Name)
    {
        if (this.SETTINGS.ContainsKey(Name)) {
            return SETTINGS[Name];
        } else {
            throw new System.ArgumentException("Setting " + Name + " is not defined", "original");
        }
    }

    // Returns a Room object to a given index, if it exists
    public Data_Room getRoomByIndex(int I)
    {
        if(ROOMS.ContainsKey(I)) {
            return ROOMS[I];
        } else {
            throw new System.ArgumentException("There is no room #" + I, "original");
        }
    }
}
