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
        ROOMS = new SortedList<int, Data_Room>();
        DOORS = new SortedList<int, Data_Door>();
		ITEM_SPOTS = new SortedList<int, Data_ItemSpawn>();
		ITEMS = new SortedList<int, Data_Item>();
        PLAYER_CHARACTER = null;
        MONSTER = null;
		CADAVER = null;
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
		if(!SAVING_DISABLED && GS.getCHARA().etherialCooldown < 0.1f) // The second clause is just to avoid saving weird in-between states
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
