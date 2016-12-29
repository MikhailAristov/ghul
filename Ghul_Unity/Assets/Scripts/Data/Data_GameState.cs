using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;

[Serializable]
public class Data_GameState {

    [NonSerialized] // Setting this flag suspends the game
	public bool SUSPENDED = true;

	[NonSerialized] // Setting this flag makes the game generate a new item
	public bool NEXT_ITEM_PLEASE = true;

    [NonSerialized]
    private Dictionary<string, float> SETTINGS;

    [SerializeField]
    public SortedList<int, Data_Room> ROOMS;
    [SerializeField]
    public SortedList<int, Data_Door> DOORS;
	[SerializeField]
	public SortedList<int, Data_ItemSpawn> ITEM_SPOTS;
	[SerializeField]
	public SortedList<int, Data_Item> ITEMS;

    [SerializeField]
    private Data_PlayerCharacter PLAYER_CHARACTER;
    [SerializeField]
    private Data_Monster MONSTER;
	[SerializeField]
	private Data_Cadaver CADAVER;

    private static bool SAVING_DISABLED = false; // For debugging purposes
    private static string FILENAME_SAVE_RESETTABLE = "save1.dat";
    //private static string FILENAME_SAVE_PERMANENT  = "save2.dat";

    // Construct an empty game state
    public Data_GameState()
    {
        SETTINGS = new Dictionary<string, float>();
        ROOMS = new SortedList<int, Data_Room>();
        DOORS = new SortedList<int, Data_Door>();
		ITEM_SPOTS = new SortedList<int, Data_ItemSpawn>();
		ITEMS = new SortedList<int, Data_Item>();
        PLAYER_CHARACTER = null;
        MONSTER = null;
		CADAVER = null;
    }

    public void loadDefaultSetttings()
    {
        SETTINGS = new Dictionary<string, float>();
        // Screen settings
        SETTINGS.Add("SCREEN_SIZE_HORIZONTAL", 6.4f);   // 640px
        SETTINGS.Add("SCREEN_SIZE_VERTICAL", 4.8f);     // 480px

        // Level generation setttings
        SETTINGS.Add("VERTICAL_ROOM_SPACING", -5.0f);   // Must be bigger than SCREEN_SIZE_VERTICAL

        // Level layout setttings
        SETTINGS.Add("HORIZONTAL_ROOM_MARGIN", 0.9f);   // Prevents movement to screen edge past the margin
        SETTINGS.Add("HORIZONTAL_DOOR_WIDTH", 1.35f);

		// Ritual room settings
		SETTINGS.Add("RITUAL_ROOM_INDEX", 0.0f);		// The index of the room with the ritual pentagram (has to be cast to int)
		SETTINGS.Add("RITUAL_PENTAGRAM_CENTER", 0.05f);	// The center pentagram space (relative to room center)
		SETTINGS.Add("RITUAL_PENTAGRAM_RADIUS", 1.4f);	// The distance between pentagram center and edge in either direction
		SETTINGS.Add("RITUAL_ITEMS_REQUIRED", 5.0f);	// How many items are needed for the ritual (has to be cast to int)

        // Door settings
        SETTINGS.Add("MARGIN_DOOR_ENTRANCE", 0.6f);     // How close a character's center of mass must be to the door's center to use it
        SETTINGS.Add("DOOR_COOLDOWN_DURATION", 0.4f);

        // Character movement settings
        SETTINGS.Add("CHARA_WALKING_SPEED", 5.0f);
        SETTINGS.Add("CHARA_RUNNING_SPEED", 8.0f);

		// Monster settings
		SETTINGS.Add("MONSTER_WALKING_SPEED", 5.2f);
		SETTINGS.Add("MONSTER_SLOW_WALKING_SPEED", 2.5f); // when the monster randomly walks around
		SETTINGS.Add("MONSTER_KILL_RADIUS", 1.0f);      // when the player gets this close to the monster, he dies.
		SETTINGS.Add("TIME_TO_REACT", 0.35f);           // if the player escapes the monster's radius within this timeframe, he isn't killed.

        // Stamina range: 0.0 .. 1.0; increments are applied per second
        SETTINGS.Add("RUNNING_STAMINA_LOSS", -0.2f);    // Must be negative
        SETTINGS.Add("WALKING_STAMINA_GAIN", 0.1f);
		SETTINGS.Add("STANDING_STAMINA_GAIN", 0.4f);

		// Item settings
		SETTINGS.Add("MARGIN_ITEM_COLLECT", 0.3f);		// How close a character's center must be to an item to be able to collect it
		SETTINGS.Add("ITEM_CARRY_ELEVATION", -0.33f);	// Distance from the horizontal center of the room at which items are carried by chara
		SETTINGS.Add("ITEM_FLOOR_LEVEL", -1.8f);		// Distance from the horizontal center of the room at which items are lying on the floor

        // Miscellaneous setttings
        SETTINGS.Add("AUTOSAVE_FREQUENCY", 10.0f);      // In seconds
        SETTINGS.Add("CAMERA_PANNING_SPEED", 9.0f);
		SETTINGS.Add("TOTAL_DEATH_DURATION", 3.0f);     // When deathDuration of Data_Character reaches this value the player resets to the starting room
	}

    // Adds a room to the game state
    public void addRoom(string gameObjectName)
    {
        int INDEX = ROOMS.Count;
        ROOMS.Add(INDEX, new Data_Room(INDEX, gameObjectName));
    }

    // Adds a door to the game state, as well as to its containing room
    public void addDoor(string gameObjectName, int RoomIndex)
    {
        int INDEX = DOORS.Count;
        DOORS.Add(INDEX, new Data_Door(INDEX, gameObjectName));
        ROOMS[RoomIndex].addDoor(DOORS[INDEX], DOORS[INDEX].gameObj.transform.position.x);
    }

	// Adds an item spot to the game state, as well as to its containing room
	public void addItemSpot(string gameObjectName, int RoomIndex) 
	{
		int index = ITEM_SPOTS.Count;
		GameObject gameObj = GameObject.Find(gameObjectName);
		float relativeX = gameObj.transform.localPosition.x;
		float relativeY = gameObj.transform.localPosition.y;

		Data_ItemSpawn spot = new Data_ItemSpawn(index, RoomIndex, relativeX, relativeY);
		ITEM_SPOTS.Add(index, spot);
		ROOMS[RoomIndex].addItemSpot(spot);
	}

	// Adds an item to the game state
	public Data_Item addItem(string gameObjectName) 
	{
		int INDEX = ITEMS.Count;
		Data_Item newItem = new Data_Item(gameObjectName);
		ITEMS.Add(INDEX, newItem);
		return newItem;
	}

    // Connects two doors to each other
	public void connectTwoDoors(int fromIndex, int toIndex) 
	{
        DOORS[fromIndex].connectTo(DOORS[toIndex]);
    }

	// Assigns an item to its spawn point
	public void setItemSpawnPoint(int itemIndex, int spotIndex) 
	{
		Data_Item curItem = getItemByIndex(itemIndex);
		curItem.itemSpotIndex = spotIndex;
	}

	// TODO Removes an item from an item spot if there is one
	public void removeItemFromSpot(int spotIndex) 
	{
		ITEM_SPOTS[spotIndex].removeItem();
	}

	// TODO Places an item sprite somewhere where it can't be seen
	public void removeItemSprite(int itemIndex) 
	{
		Vector3 nirvana = new Vector3 (-90, 0, 0);
		ITEMS[itemIndex].gameObj.transform.Translate(nirvana - ITEMS[itemIndex].gameObj.transform.position);
	}

    // Sets the player character object
    public void setPlayerCharacter(string gameObjectName)
    {
        PLAYER_CHARACTER = new Data_PlayerCharacter(gameObjectName);
    }

    // Returns the player character object
    public Data_PlayerCharacter getCHARA()
    {
        return PLAYER_CHARACTER;
    }

    // Sets the monster character object
    public void setMonsterCharacter(string gameObjectName)
    {
        MONSTER = new Data_Monster(gameObjectName);
    }

    // Returns the monster character object
    public Data_Monster getMonster()
    {
        return MONSTER;
    }

	// Sets the cadaver character object
	public void setCadaverCharacter(string gameObjectName)
	{
		CADAVER = new Data_Cadaver(gameObjectName);
	}

	// Returns the cadaver character object
	public Data_Cadaver getCadaver()
	{
		return CADAVER;
	}

    // Returns the value of a game setting
    public float getSetting(string Name)
    {
        if (SETTINGS.ContainsKey(Name)) {
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
            throw new System.ArgumentException("There is no room #" + I);
        }
    }

    // Returns a Door object to a given index, if it exists
    public Data_Door getDoorByIndex(int I)
    {
        if (DOORS.ContainsKey(I)) {
            return DOORS[I];
        } else {
            throw new System.ArgumentException("There is no door #" + I);
        }
    }

	// Returns an ItemSpot object to a given index, if it exists
	public Data_ItemSpawn getItemSpawnPointByIndex(int I)
	{
		if (ITEM_SPOTS.ContainsKey(I)) {
			return ITEM_SPOTS[I];
		} else {
			throw new System.ArgumentException("There is no item spawn point #" + I);
		}
	}

	// Returns an Item object to a given index, if it exists
	public Data_Item getItemByIndex(int I)
	{
		if (ITEMS.ContainsKey(I)) {
			return ITEMS[I];
		} else {
			throw new System.ArgumentException("There is no item #" + I);
		}
	}

	// Returns the most recent item that has been spawned
	public Data_Item getCurrentItem() {
		return ITEMS[ITEMS.Count - 1];
	}

    // Saves the current game state to disk
    [MethodImpl(MethodImplOptions.Synchronized)] // Synchronized to avoid simultaneous calls from parallel threads
    public static void saveToDisk(Data_GameState GS)
    {
        if(!SAVING_DISABLED)
        {
            // Set the save file paths
            string resettableFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_RESETTABLE;
            //string permanentFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_PERMANENT;

            Debug.Log("Saving game to " + resettableFilePath);

            // Prepare writing file
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(resettableFilePath);

            // Write the game state to file and close it
            bf.Serialize(file, GS);
            file.Close();
        }
    }

    // Returns a game state from disk; returns null if no saved state is found
    public static Data_GameState loadFromDisk()
    {
        // Set the save file paths
        string resettableFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_RESETTABLE;
        //string permanentFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_PERMANENT;
        if(!File.Exists(resettableFilePath))
        {
            Debug.Log("No game state found in: " + resettableFilePath);
            return null;
        }

        Debug.Log("Loading game from " + resettableFilePath);
        
        // Prepare opening the file
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(resettableFilePath, FileMode.Open);

        // Read the file to memory and close it
        Data_GameState result = (Data_GameState)bf.Deserialize(file);
        file.Close();
        return result;        
    }
}
