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

	[SerializeField] // For possible values, see Control_GameState
	public int OVERALL_STATE;

	// INDIVIDUAL CONTROL FLAGS
    [NonSerialized] // Setting this flag suspends the game
	public bool SUSPENDED = true;
	[NonSerialized] // Setting this flag makes the game generate a new item
	public bool NEXT_ITEM_PLEASE = true;
	[NonSerialized] // Setting this flag activates the house graph mix up
	public bool TONI_KILLED = false;
	[NonSerialized] // This lets the game state catch the precise moment the monster dies for the first time in the endgame
	public bool MONSTER_KILLED = false;

	[SerializeField]
	public Data_Graph HOUSE_GRAPH;

    [SerializeField]
    public SortedList<int, Data_Room> ROOMS;
    [SerializeField]
    public SortedList<int, Data_Door> DOORS;
	[SerializeField]
	public SortedList<int, Data_Item> ITEMS;

	[SerializeField]
	public int numItemsPlaced;
	[SerializeField]
	public int indexOfSearchedItem;

    [SerializeField]
	public Data_PlayerCharacter TONI;
    [SerializeField]
    private Data_Monster MONSTER;
	[SerializeField]
	private Data_Cadaver CADAVER;
	public bool monsterSeesToni {
		get { return (TONI.isIn == MONSTER.isIn && !TONI.isInvulnerable); }
		private set { return; }
	}
	public float distanceToToni {
		get { return ((TONI.isIn == MONSTER.isIn) ? (TONI.atPos - MONSTER.atPos) : float.NaN); }
		private set { return; }
	}

	[SerializeField]
	public float[,] distanceBetweenTwoDoors;
	[SerializeField]
	public float[,] distanceBetweenTwoRooms;
	public bool allRoomsReachable {
		get { return ( distanceBetweenTwoRooms.Cast<float>().Max() < (float.MaxValue / 2) ); }
		private set { return; }
	}

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
		OVERALL_STATE = Control_GameState.STATE_COLLECTION_PHASE;
		numItemsPlaced = 0;
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

	// Adds a door object to the game state
	public void addDoor(Data_Door newDoor)
	{
		DOORS.Add(newDoor.INDEX, newDoor);
	}

	// Completely removes all door objects from the game state,
	// so that they can generated anew
	public void removeAllDoors() {
		foreach(Data_Room r in ROOMS.Values) {
			r.removeAllDoors();
		}
		DOORS = new SortedList<int, Data_Door>();
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

	// Returns the item the player has to find at the moment (or bring back to the ritual room, if already in possession)
	public Data_Item getCurrentItem() {
		return ITEMS[indexOfSearchedItem];
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
			foreach(Data_Door startingDoor in getRoomByIndex(a.RoomId).DOORS.Values) {
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
			foreach(Data_Door targetDoor in getRoomByIndex(pos.RoomId).DOORS.Values) {
				float dist = distanceBetweenTwoDoors[door.INDEX, targetDoor.INDEX] + Math.Abs(targetDoor.atPos - pos.X);
				if(dist < result) {
					result = dist;
				}
			}
			return result;
		}
	}

}
