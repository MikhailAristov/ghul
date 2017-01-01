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
	public SortedList<int, Data_Item> ITEMS;

    [SerializeField]
    private Data_PlayerCharacter PLAYER_CHARACTER;
    [SerializeField]
    private Data_Monster MONSTER;
	[SerializeField]
	private Data_Cadaver CADAVER;

	[SerializeField]
	private float[,] distanceBetweenTwoDoors;
	[SerializeField]
	private float[,] distanceBetweenTwoRooms;

    private static bool SAVING_DISABLED = false; // For debugging purposes
    private static string FILENAME_SAVE_RESETTABLE = "save1.dat";
    //private static string FILENAME_SAVE_PERMANENT  = "save2.dat";

    // Construct an empty game state
    public Data_GameState()
    {
        ROOMS = new SortedList<int, Data_Room>();
        DOORS = new SortedList<int, Data_Door>();
		ITEMS = new SortedList<int, Data_Item>();
        PLAYER_CHARACTER = null;
        MONSTER = null;
		CADAVER = null;
    }

	// Adds a room object to the game state
	public void addRoom(Data_Room newRoom)
	{
		ROOMS.Add(newRoom.INDEX, newRoom);
	}

	// Adds a room object to the game state
	public void addDoor(Data_Door newDoor)
	{
		DOORS.Add(newDoor.INDEX, newDoor);
	}

	// Connects two doors to each other
	public void connectTwoDoors(int fromIndex, int toIndex) 
	{
		DOORS[fromIndex].connectTo(DOORS[toIndex]);
	}

	// Adds an item to the game state
	public Data_Item addItem(string gameObjectName) 
	{
		int INDEX = ITEMS.Count;
		Data_Item newItem = new Data_Item(gameObjectName);
		ITEMS.Add(INDEX, newItem);
		return newItem;
	}

    // Sets the player character object
    public void setPlayerCharacter(string gameObjectName) {
        PLAYER_CHARACTER = new Data_PlayerCharacter(gameObjectName);
    }

    // Returns the player character object
    public Data_PlayerCharacter getCHARA() {
        return PLAYER_CHARACTER;
    }

    // Sets the monster character object
    public void setMonsterCharacter(string gameObjectName) {
        MONSTER = new Data_Monster(gameObjectName);
    }

    // Returns the monster character object
    public Data_Monster getMonster() {
        return MONSTER;
    }

	// Sets the cadaver character object
	public void setCadaverCharacter(string gameObjectName) {
		CADAVER = new Data_Cadaver(gameObjectName);
	}

	// Returns the cadaver character object
	public Data_Cadaver getCadaver() {
		return CADAVER;
	}

    // Returns a Room object to a given index, if it exists
    public Data_Room getRoomByIndex(int I) {
        if(ROOMS.ContainsKey(I)) {
            return ROOMS[I];
        } else {
            throw new System.ArgumentException("There is no room #" + I);
        }
    }

    // Returns a Door object to a given index, if it exists
    public Data_Door getDoorByIndex(int I) {
        if (DOORS.ContainsKey(I)) {
            return DOORS[I];
        } else {
            throw new System.ArgumentException("There is no door #" + I);
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

	// Returns a random item spawn point
	public Data_Position getRandomItemSpawn() {
		Data_Room room; Data_Position result;
		do { // Find a random room with item spawn points and pick one at random
			room = getRoomByIndex(UnityEngine.Random.Range(0, ROOMS.Count));
			result = room.getRandomItemSpawnPoint();
		} while(result == null);
		return result;
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

	// Precomputes the complete matrix of distances between doors and rooms
	// See https://en.wikipedia.org/wiki/Floyd%E2%80%93Warshall_algorithm
	public void precomputeAllPairsShortestDistances() {
		float doorTransitionCost = 0.0f;
		precomputeDoorDistances(doorTransitionCost);
		precomputeRoomDistances(doorTransitionCost);
	}

	private void precomputeDoorDistances(float doorTransitionCost) {
		// Prepare the door graph
		// Assume that the vertices are already ordered and without gaps
		distanceBetweenTwoDoors = new float[DOORS.Count, DOORS.Count]; // Square distance matrix
		for(int i = 0; i < DOORS.Count; i++) {
			for(int j = 0; j < DOORS.Count; j++) {
				// Distance := 0 if i == j
				if(i == j) { 
					distanceBetweenTwoDoors[i, j] = 0.0f; 
				}
				// Distance := door transition cost, if door i connects to door j
				else if(DOORS[i].connectsTo == DOORS[j]) {
					distanceBetweenTwoDoors[i, j] = doorTransitionCost;
				}
				// Distance := actual spacing, if door i and j are in the same room
				else if(DOORS[i].isIn == DOORS[j].isIn) {
					distanceBetweenTwoDoors[i, j] = Math.Abs(DOORS[i].atPos - DOORS[j].atPos);
				}
				// Otherwise, initialize it to infinity
				else {
					distanceBetweenTwoDoors[i, j] = float.MaxValue;
				}
			}
		}
		// Floyd-Warshall algorithm:
		for(int k = 0; k < DOORS.Count; k++) {
			for(int i = 0; i < DOORS.Count; i++) {
				for(int j = 0; j < DOORS.Count; j++) {
					if(distanceBetweenTwoDoors[i, j] > distanceBetweenTwoDoors[i, k] + distanceBetweenTwoDoors[k, j]) {
						distanceBetweenTwoDoors[i, j] = distanceBetweenTwoDoors[i, k] + distanceBetweenTwoDoors[k, j];
					}
				}
			}
		}
		// TODO output the results
		Debug.Log(JsonUtility.ToJson(distanceBetweenTwoDoors, true));
	}

	private void precomputeRoomDistances(float doorTransitionCost) {

	}
}
