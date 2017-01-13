using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;
using System.Linq;

[Serializable]
public class Data_GameState {

    [NonSerialized] // Setting this flag suspends the game
	public bool SUSPENDED = true;

	[NonSerialized] // Setting this flag makes the game generate a new item
	public bool NEXT_ITEM_PLEASE = true;

	[SerializeField]
	public Data_Graph HOUSE_GRAPH;

    [SerializeField]
    public SortedList<int, Data_Room> ROOMS;
    [SerializeField]
    public SortedList<int, Data_Door> DOORS;
	[SerializeField]
	public SortedList<int, Data_Item> ITEMS;

    [SerializeField]
	private Data_PlayerCharacter TONI;
    [SerializeField]
    private Data_Monster MONSTER;
	[SerializeField]
	private Data_Cadaver CADAVER;

	[SerializeField]
	private float[,] distanceBetweenTwoDoors;
	[SerializeField]
	private float[,] distanceBetweenTwoRooms;
	public bool allRoomsReachable {
		get { return ( distanceBetweenTwoRooms.Cast<float>().Max() < (float.MaxValue / 2) ); }
		private set { return; }
	}

    private static bool SAVING_DISABLED = false; // For debugging purposes
    private static string FILENAME_SAVE_RESETTABLE = "save1.dat";
    //private static string FILENAME_SAVE_PERMANENT  = "save2.dat";

    // Construct an empty game state
    public Data_GameState()
    {
		HOUSE_GRAPH = new Data_Graph();
        ROOMS = new SortedList<int, Data_Room>();
        DOORS = new SortedList<int, Data_Door>();
		ITEMS = new SortedList<int, Data_Item>();
        TONI = null;
        MONSTER = null;
		CADAVER = null;
    }

	// Adds a room object to the game state
	public void addRoom(Data_Room newRoom)
	{
		ROOMS.Add(newRoom.INDEX, newRoom);
		// Also update the house graph
		HOUSE_GRAPH.addRoom(newRoom.countAllDoorSpawns);
		if(newRoom.hasLeftSideDoorSpawn) {
			HOUSE_GRAPH.addDoorSpawn(newRoom.INDEX, true, false);
		}
		for(int i = 0; i < newRoom.countBackDoorSpawns; i++) {
			HOUSE_GRAPH.addDoorSpawn(newRoom.INDEX, false, false);
		}
		if(newRoom.hasRightSideDoorSpawn) {
			HOUSE_GRAPH.addDoorSpawn(newRoom.INDEX, false, true);
		}
	}

	// Adds a room object to the game state
	public void addDoor(Data_Door newDoor)
	{
		DOORS.Add(newDoor.INDEX, newDoor);
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
        TONI = new Data_PlayerCharacter(gameObjectName);
    }

    // Returns the player character object
    public Data_PlayerCharacter getToni() {
        return TONI;
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

	// Returns the room that is the furthest from the one with the given index
	public Data_Room getRoomFurthestFrom(int roomIndex) {
		int result = roomIndex;
		for(int i = 0; i < ROOMS.Count; i++) {
			if(distanceBetweenTwoRooms[roomIndex, i] > distanceBetweenTwoRooms[roomIndex, result]) {
				result = i;
			}
		}
		return ROOMS[result];
	}

	// Get a random room
	public Data_Room getRandomRoom(bool includeRitualRoom) {
		return getRoomByIndex(UnityEngine.Random.Range((includeRitualRoom ? 0 : 1), ROOMS.Count));
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
		Data_Position result;
		do { // Find a random room with item spawn points and pick one at random
			Data_Room room = getRandomRoom(false);
			result = room.getRandomItemSpawnPoint();
		} while(result == null);
		return result;
	}

    // Saves the current game state to disk
    [MethodImpl(MethodImplOptions.Synchronized)] // Synchronized to avoid simultaneous calls from parallel threads
    public static void saveToDisk(Data_GameState GS)
    {
		if(!SAVING_DISABLED && GS.getToni().etherialCooldown < 0.1f) // The second clause is just to avoid saving weird in-between states
        {
            // Set the save file paths
            string resettableFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_RESETTABLE;
            //string permanentFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_PERMANENT;

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

		try {
			Debug.Log("Loading game from " + resettableFilePath);

			// Prepare opening the file
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(resettableFilePath, FileMode.Open);

			// Read the file to memory and close it
			Data_GameState result = (Data_GameState)bf.Deserialize(file);
			file.Close();
			return result;
		} catch(SerializationException) {
			Debug.LogWarning("The saved game " + resettableFilePath + " is corrupted, starting a new game instead");
			return null;
		}  
	}

	// Calculates all-pairs shortest distances between all doors and all rooms,
	// wherein the shortest distance between two rooms is the shortest door-to-door conection between them,
	// and stores both as matrices within the game state
	// See https://en.wikipedia.org/wiki/Floyd%E2%80%93Warshall_algorithm
	public void precomputeAllPairsShortestDistances(float doorTransitionCost) {
		// Prepare the door graph after assuming that the vertices are already ordered and without gaps
		distanceBetweenTwoDoors = new float[DOORS.Count, DOORS.Count]; // Square distance matrix
		for(int i = 0; i < DOORS.Count; i++) {
			// Plausibility check
			if(DOORS[i].connectsTo == null) {
				throw new IndexOutOfRangeException(DOORS[i].gameObj.name + " doesn't connect to anything!");
			}
			// Initialize the distance to every other door
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
				// Otherwise, initialize it to half infinity (only half to avoid float overflows)
				else {
					distanceBetweenTwoDoors[i, j] = float.MaxValue / 2;
				}
			}
		}
		// Also prepare the room graph under the same assumption
		distanceBetweenTwoRooms = new float[ROOMS.Count, ROOMS.Count];
		for(int i = 0; i < ROOMS.Count; i++) {
			for(int j = 0; j < ROOMS.Count; j++) {
				distanceBetweenTwoRooms[i, j] = (i == j) ? 0.0f : float.MaxValue / 2;
			}
		}
		// Floyd-Warshall algorithm (extended)
		for(int k = 0; k < DOORS.Count; k++) {
			for(int i = 0; i < DOORS.Count; i++) {
				int iRoom = DOORS[i].isIn.INDEX;
				for(int j = 0; j < DOORS.Count; j++) {
					int jRoom = DOORS[j].isIn.INDEX;
					// Update the door distance if necessary
					if(distanceBetweenTwoDoors[i, j] > distanceBetweenTwoDoors[i, k] + distanceBetweenTwoDoors[k, j]) {
						distanceBetweenTwoDoors[i, j] = distanceBetweenTwoDoors[i, k] + distanceBetweenTwoDoors[k, j];
					}
					// Also update the rooms with the new door distance if necessary
					if(distanceBetweenTwoRooms[iRoom, jRoom] > distanceBetweenTwoDoors[i, j]) {
						distanceBetweenTwoRooms[iRoom, jRoom] = distanceBetweenTwoDoors[i, j];
					}
				}
			}
		}
	}

	// Find the (horizontal) distance between two arbitrary positions
	public float getDistance(Data_Position a, Data_Position b) {
		// Simple case: same room
		if(a.RoomId == b.RoomId) {
			return Math.Abs(a.X - b.X);
		} else {
			// Different rooms: check door distances
			float result = float.MaxValue;
			foreach(Data_Door startingDoor in getRoomByIndex(a.RoomId).DOORS) {
				float dist = Math.Abs(startingDoor.atPos - a.X) + getDistance(startingDoor, b); // Recycling of the other function
				if(dist < result) {
					result = dist;
				}
			}
			return result;
		}
	}

	// Find the (horizontal) distance between a particular door and a target position
	public float getDistance(Data_Door door, Data_Position pos) {
		// Simple case: same room
		if(door.isIn.INDEX == pos.RoomId) {
			return Math.Abs(door.atPos - pos.X);
		} else {
			// Different rooms: check door distances
			float result = float.MaxValue;
			foreach(Data_Door targetDoor in getRoomByIndex(pos.RoomId).DOORS) {
				float dist = distanceBetweenTwoDoors[door.INDEX, targetDoor.INDEX] + Math.Abs(targetDoor.atPos - pos.X);
				if(dist < result) {
					result = dist;
				}
			}
			return result;
		}
	}
}
