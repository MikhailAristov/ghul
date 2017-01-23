using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Data_GraphRoomVertice {

	[NonSerialized]
	private Data_Graph graph;

	[SerializeField]
	private int _INDEX;
	public int INDEX {
		get { return _INDEX; }
		private set { _INDEX = value;  }
	}

	// maximum number of doors this room can have. In other words: number of door spawns
	[SerializeField]
	private int _MAX_NUM_OF_DOORS;
	public int MAX_NUM_OF_DOORS {
		get { return _MAX_NUM_OF_DOORS; }
		private set { _MAX_NUM_OF_DOORS = value;  }
	}

	// Actual number of connections that exist. NUM_OF_DOORS <= MAX_NUM_OF_DOORS.
	[SerializeField]
	private int _NUM_OF_DOORS;
	public int NUM_OF_DOORS {
		get { return _NUM_OF_DOORS; }
		private set { _NUM_OF_DOORS = value;  }
	}

	[SerializeField]
	public SortedList<int, Data_GraphDoorSpawn> DOOR_SPAWNS;

	public Data_GraphRoomVertice(int I, int MAX_DOORS, Data_Graph g) {
		INDEX = I;
		MAX_NUM_OF_DOORS = MAX_DOORS;
		DOOR_SPAWNS = new SortedList<int, Data_GraphDoorSpawn>();
		graph = g;
	}

	// Adds a door spawn to the list
	public Data_GraphDoorSpawn addDoorSpawn(int index, bool leftSide, bool rightSide) {
		Data_GraphDoorSpawn spawn = new Data_GraphDoorSpawn(index, leftSide, rightSide);
		spawn.roomId = INDEX; // The room's index, not the door's!
		DOOR_SPAWNS.Add(index, spawn);
		return spawn;
	}

	// returns true if at least one door spawn is connected.
	public bool hasConnectedDoorSpawns() {
		foreach (Data_GraphDoorSpawn dSpawn in DOOR_SPAWNS.Values) {
			if (dSpawn.isConnected()) { return true; }
		}
		return false;
	}

	// returns true if at least one doorspawn isn't connected yet.
	public bool hasEmptyDoorSpawns() {
		foreach (Data_GraphDoorSpawn dSpawn in DOOR_SPAWNS.Values) {
			if (!dSpawn.isConnected()) { return true; }
		}
		return false;
	}

	// Returns the first door spawn that isn't connected yet or null otherwise.
	public Data_GraphDoorSpawn getEmptyDoorSpawn() {
		foreach (Data_GraphDoorSpawn dSpawn in DOOR_SPAWNS.Values) {
			if (!dSpawn.isConnected()) { return dSpawn; }
		}
		return null;
	}

	// Returns a random empty door spawn or null if none exists.
	public Data_GraphDoorSpawn getRandomEmptyDoorSpawn() {
		if (!hasEmptyDoorSpawns())
			return null;
		int rand = UnityEngine.Random.Range(0, DOOR_SPAWNS.Count);
		Data_GraphDoorSpawn spawn = DOOR_SPAWNS.Values[rand];
		while (spawn.isConnected()) {
			rand = UnityEngine.Random.Range(0, DOOR_SPAWNS.Count);
			spawn = DOOR_SPAWNS.Values[rand];
		}
		return spawn;
	}

	// Returns a random connected door spawn or null if none exists.
	public Data_GraphDoorSpawn getRandomConnectedDoorSpawn() {
		if (!hasConnectedDoorSpawns())
			return null;
		int rand = UnityEngine.Random.Range(0, DOOR_SPAWNS.Count);
		Data_GraphDoorSpawn spawn = DOOR_SPAWNS.Values[rand];
		while (!spawn.isConnected()) {
			rand = UnityEngine.Random.Range(0, DOOR_SPAWNS.Count);
			spawn = DOOR_SPAWNS.Values[rand];
		}
		return spawn;
	}

	// Returns the door spawn after a specified other door spawn. The one returned must be empty.
	// If that is not the case, all the following ones must be pushed to the back.
	public Data_GraphDoorSpawn getEmptyDoorSpawnAfterConnection(int index) {
		if (!hasEmptyDoorSpawns())
			return null;
		if (!hasDoorSpawnWithIndex(index))
			return null;

		// Get the door spawn after the one with the specified index. It will be used for the new connection.
		int followUpListPlace = DOOR_SPAWNS.IndexOfKey(index) + 1; 	// a bit confusing. the spawn index is the key of the list.
																	// IndexOfKey gets the index of the element in the list
		if (followUpListPlace >= DOOR_SPAWNS.Count) {
			followUpListPlace = 0;
		}
		Data_GraphDoorSpawn newConnectionSpawn = DOOR_SPAWNS.Values[followUpListPlace];

		// Reconnecting everything in the list from that point on.
		if (!newConnectionSpawn.isConnected()) return newConnectionSpawn; // no work. Was empty anyways

		Data_GraphDoorSpawn iteratingSpawn = new Data_GraphDoorSpawn(newConnectionSpawn);
		int spawnIndexThereOld = iteratingSpawn.CONNECTS_TO_SPAWN_ID;
		bool reachedEmptySpawn = false;

		while (!reachedEmptySpawn) {
			int roomIndexThereOld = graph.DOOR_SPAWN_IS_IN_ROOM[spawnIndexThereOld];
			Data_GraphRoomVertice roomThereOld = graph.ABSTRACT_ROOMS[roomIndexThereOld];
			Data_GraphDoorSpawn spawnThereOld = roomThereOld.DOOR_SPAWNS[spawnIndexThereOld];

			followUpListPlace++;
			if (followUpListPlace == DOOR_SPAWNS.Count) { followUpListPlace = 0; }
			iteratingSpawn = DOOR_SPAWNS.Values[followUpListPlace];
			int spawnIndexHereNew = iteratingSpawn.INDEX;

			spawnThereOld.connectTo(spawnIndexHereNew); // Connect spawn from other room to the next one here
			int spawnIndexThereOldTMP = spawnIndexThereOld;
			spawnIndexThereOld = iteratingSpawn.CONNECTS_TO_SPAWN_ID; // for the next loop
			if (spawnIndexThereOld == -1) { reachedEmptySpawn = true; }
			iteratingSpawn.connectTo(spawnIndexThereOldTMP); // Connect next one here to the spawn from other room
		}

		return newConnectionSpawn;
	}

	// Moves all spawn door connections by one.
	public void rotate() {
		if (MAX_NUM_OF_DOORS <= 1)
			return;
		int listPlace = 0;
		Data_GraphDoorSpawn iteratingSpawn = DOOR_SPAWNS.Values[listPlace];
		int spawnIndexThereOld = iteratingSpawn.CONNECTS_TO_SPAWN_ID;
		bool fullCircle = false;
		int roomIndexThereOld = -1;
		Data_GraphRoomVertice roomThereOld = null;
		Data_GraphDoorSpawn spawnThereOld = null;

		while (!fullCircle) {
			if (spawnIndexThereOld != -1) {
				roomIndexThereOld = graph.DOOR_SPAWN_IS_IN_ROOM[spawnIndexThereOld];
				roomThereOld = graph.ABSTRACT_ROOMS[roomIndexThereOld];
				spawnThereOld = roomThereOld.DOOR_SPAWNS[spawnIndexThereOld];
			}

			listPlace++;
			if (listPlace == DOOR_SPAWNS.Count) { 
				listPlace = 0; 
				fullCircle = true;
			}
			iteratingSpawn = DOOR_SPAWNS.Values[listPlace];
			int spawnIndexHereNew = iteratingSpawn.INDEX;

			if (spawnIndexThereOld != -1) {
				spawnThereOld.connectTo(spawnIndexHereNew); // Connect spawn from other room to the next one here
			}
			int spawnIndexThereOldTMP = spawnIndexThereOld;
			spawnIndexThereOld = iteratingSpawn.CONNECTS_TO_SPAWN_ID; // for the next loop
			iteratingSpawn.connectTo(spawnIndexThereOldTMP); // Connect next one here to the spawn from other room
		}
	}

	// Calculates how many door spawns are connected.
	public void updateNumDoors() {
		int counter = 0;
		foreach (Data_GraphDoorSpawn spawn in DOOR_SPAWNS.Values) {
			if (spawn.isConnected()) { counter++; }
		}
		NUM_OF_DOORS = counter;
	}

	// true, if DOOR_SPAWNS contains an object with the specified index.
	public bool hasDoorSpawnWithIndex(int index) {
		foreach (Data_GraphDoorSpawn spawn in DOOR_SPAWNS.Values) {
			if (spawn.INDEX == index)
				return true;
		}
		return false;
	}

	// Returns the next connected door spawn after the one given as a parameter
	public Data_GraphDoorSpawn getNextConnectedSpawn(int spawnID) {
		if (!hasDoorSpawnWithIndex(spawnID))
			return null;
		int endlessLoopPrevention = DOOR_SPAWNS.IndexOfKey(spawnID);
		int position = DOOR_SPAWNS.IndexOfKey(spawnID) + 1;
		if (position >= DOOR_SPAWNS.Count) { position = 0; }

		while (position != endlessLoopPrevention) {
			if (DOOR_SPAWNS.Values[position].isConnected()) {
				return DOOR_SPAWNS.Values[position];
			}
			position++;
			if (position >= DOOR_SPAWNS.Count) { position = 0; }
		}
		return DOOR_SPAWNS[spawnID]; // There's only one connected spawn here. Go back to where you came from.
	}

	// Returns the local index of the door spawn given its global index
	public int getLocalSpawnIndex(int globalIndex) {
		int i = 0;
		foreach(Data_GraphDoorSpawn ds in DOOR_SPAWNS.Values) {
			if(ds.INDEX == globalIndex) {
				return i;
			} else {
				i += 1;
			}
		}
		throw new IndexOutOfRangeException("GRAPH: The spawn point with global index #" + globalIndex + " is no in the room vertice #" + _INDEX);
	}
}
