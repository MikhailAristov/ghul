using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DummyData_Graph {

	[SerializeField]
	public SortedList<int, DummyData_AbstractRoom> ABSTRACT_ROOMS;
	[SerializeField]
	public SortedList<int, int> DOOR_SPAWN_IS_IN_ROOM; // given a door spawn index, this saves the room index of the room the spawn is in.

	public DummyData_Graph() {
		ABSTRACT_ROOMS = new SortedList<int, DummyData_AbstractRoom>();
		DOOR_SPAWN_IS_IN_ROOM = new SortedList<int, int>();
	}

	// Adds an abstract room to the list which has maxDoors door spawns.
	// IMPORTANT: The first room added should be the ritual room!
	public void addRoom(int maxDoors)
	{
		int INDEX = ABSTRACT_ROOMS.Count;
		ABSTRACT_ROOMS.Add(INDEX, new DummyData_AbstractRoom(INDEX, maxDoors, this));
	}

	// Adds a door spawn to the specified room. left/rightSide determine whether the door is located on the left or right end of the room.
	// IMPORTANT: The door spawn index doesn't know how many spawns there are in other rooms so insert them in order!
	public void addDoorSpawn(int roomIndex, bool leftSide, bool rightSide) {
		int doorSpawnIndex = DOOR_SPAWN_IS_IN_ROOM.Count;
		DOOR_SPAWN_IS_IN_ROOM.Add(doorSpawnIndex, roomIndex);
		ABSTRACT_ROOMS[roomIndex].addDoorSpawn(doorSpawnIndex, leftSide, rightSide);
	}

	public int getTotalNumberOfRooms() {
		return ABSTRACT_ROOMS.Count;
	}
	public int getTotalNumberOfDoorSpawns() {
		return DOOR_SPAWN_IS_IN_ROOM.Count;
	}

	// Returns how many doors a room can support.
	public int getRoomsMaxDoors(int roomIndex) {
		return ABSTRACT_ROOMS[roomIndex].MAX_NUM_OF_DOORS;
	}
	// Returns how many door spawns of the room are already connected.
	public int getRoomsCurrentNumDoors(int roomIndex) {
		return ABSTRACT_ROOMS[roomIndex].NUM_OF_DOORS;
	}
	// true, if all of the rooms door spawns are connected
	public bool isRoomFullyConnected(int roomIndex) {
		return (getRoomsMaxDoors(roomIndex) - getRoomsCurrentNumDoors(roomIndex) == 0);
	}

	// Given two room ids, this method connects two free door spawns. If impossible, returns false.
	// Use this for first connections that don't depend on specific topology (i.e. having the new edge between two specific ones).
	// ...or CAREFULLY connect them in clockwise order if that's even possible.
	// return value: door spawn index of room 2 that was used.
	public int connectRooms(int id1, int id2) {
		DummyData_AbstractRoom room1 = ABSTRACT_ROOMS[id1];
		DummyData_AbstractRoom room2 = ABSTRACT_ROOMS[id2];
		if (!room1.hasEmptyDoorSpawns() || !room2.hasEmptyDoorSpawns()) { 
			Debug.Log("Tried to connect rooms " + room1.INDEX + " and " + room2.INDEX + ", but at least one has no empty door spawn.");
			return -1;
		}

		DummyData_DoorSpawn doorSpawn1 = room1.getEmptyDoorSpawn(); //goes through the spawns in order and returns the first free one.
		DummyData_DoorSpawn doorSpawn2 = room2.getEmptyDoorSpawn();
		doorSpawn1.connectTo(doorSpawn2.INDEX);
		doorSpawn2.connectTo(doorSpawn1.INDEX);
		room1.updateNumDoors();
		room2.updateNumDoors();
		return doorSpawn2.INDEX;
	}

	// Given two room ids, this method connects two door spawns.
	// Room 1's door spawn is the next free one, Room 2's is the one that comes after door spawn with index spawnIndex.
	// If that spot is taken, all door spawns after it get moved one spot to the back of the list.
	public bool connectRoomAfterConnection(int id1, int id2, int spawnIndex) {
		DummyData_AbstractRoom room1 = ABSTRACT_ROOMS[id1];
		DummyData_AbstractRoom room2 = ABSTRACT_ROOMS[id2];
		if (!room1.hasEmptyDoorSpawns() || !room2.hasEmptyDoorSpawns()) { 
			Debug.Log("Tried to connect rooms " + room1.INDEX + " and " + room2.INDEX + ", but at least one has no empty door spawn.");
			return false;
		}

		DummyData_DoorSpawn doorSpawn1 = room1.getEmptyDoorSpawn(); //goes through the spawns in order and returns the first free one.
		if (!room2.hasDoorSpawnWithIndex(spawnIndex)) {
			Debug.Log("Tried to connect rooms " + room1.INDEX + " and " + room2.INDEX +
				" after door spawn " + spawnIndex + ", but it doesn't exist in room " + room2.INDEX + ".");
			return false;
		}
		DummyData_DoorSpawn doorSpawn2 = room2.getEmptyDoorSpawnAfterConnection(spawnIndex);

		doorSpawn1.connectTo(doorSpawn2.INDEX);
		doorSpawn2.connectTo(doorSpawn1.INDEX);
		room1.updateNumDoors();
		room2.updateNumDoors();
		return true;
	}
}
