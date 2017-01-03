using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Factory_Graph : MonoBehaviour {

	private DummyData_Graph graph;
	public bool graphCalculated;

	void Start() {
		graphCalculated = false;
	}

	// Returns the complete graph or null if it's not computed yet.
	public DummyData_Graph GetGraph() {
		if (graphCalculated)
			return graph;
		else
			return null;
	}

	// Removes a pre-existing graph. Use this before computing a new one if a graph already exists.
	public void deleteGraph() {
		graph = null;
		graphCalculated = false;
	}

	// Generates the planar graph given a basic edge-less graph. (i.e. no door spawns are connected)
	public void computePlanarGraph(DummyData_Graph g) {
		if (graphCalculated) {
			Debug.Log("Trying to generate a graph but there is already one computed. Use deleteGraph() before generating a new one.");
			return;
		}

		if (g == null) {
			Debug.Log("Cannot build a planar graph. No input graph given.");
			return;
		} else {
			graph = g;
		}

		// Step 1: Select a basic planar graph as a starting point.
		int rand = UnityEngine.Random.Range(1,6);
		formBasicGraph(rand);

		// Step 2: Connect vertices to the connected graph such that the resulting graph is planar again.
		connectAllRooms();

		// Step 3: Make sure two left doors and two right doors never connect. Rotate rooms if necessary.
		// These connections can't be prevented in all scenarios but two checks should get rid of most.
		checkSideConnections();
		checkSideConnections();

		if (!checkWhetherUnconnectedRoomExists()) {
			graphCalculated = true;
		} else {
			Debug.Log("Failed at connecting all rooms.");
		}
	}

	// Returns true if every room has at least one connection. Does not check whether the graph has only one connected component.
	private bool checkWhetherUnconnectedRoomExists() {
		for (int i = 0; i < graph.getTotalNumberOfRooms(); i++) {
			if (graph.ABSTRACT_ROOMS[i].NUM_OF_DOORS == 0) {
				return true;
			}
		}
		return false;
	}

	/*
	Creates a basic graph out of 4 or 5 vertices. Depending on the parameter, one of five graphs will be selected.
	The starting room (ritual room) has degree three (and index 0) and must not be a graph separator.

	Graphs:

	1	1------		2	1------2	3	1------		4	---1---		5	---1---
		|      |		|      |		|      |		|  |   |		|  |   |
		S------2		S------3		S--3---2		S--3---2		S--3---2
		|      |		|      |		|      |		|      |		|  |   |
		3------			4------			4------			4------			---4---

	The numbers in the images may not be the actual room index if the room with the shown index has too few door spawns.
	*/
	private void formBasicGraph(int graphNr) {
		int roomNr = 0;
		int room1, room2, room3, room4;

		switch (graphNr) {
		case 1:
			roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			if (roomNr == -1)
				return;
			room1 = roomNr;

			while (roomNr == room1) {
				roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			}
			if (roomNr == -1)
				return;
			room2 = roomNr;

			while (roomNr == room1 || roomNr == room2) {
				roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			}
			if (roomNr == -1)
				return;
			room3 = roomNr;

			// don't try this at home. Carefully handcrafted clockwise connections.
			graph.connectRooms(0, room1);
			graph.connectRooms(0, room2);
			graph.connectRooms(0, room3);
			graph.connectRooms(room1, room2);
			graph.connectRooms(room3, room2);

			break;

		case 2:
			roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			if (roomNr == -1)
				return;
			room1 = roomNr;

			while (roomNr == room1) {
				roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			}
			if (roomNr == -1)
				return;
			room2 = roomNr;

			while (roomNr == room1 || roomNr == room2) {
				roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			}
			if (roomNr == -1)
				return;
			room3 = roomNr;

			while (roomNr == room1 || roomNr == room2 || roomNr == room3) {
				roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			}
			if (roomNr == -1)
				return;
			room4 = roomNr;

			// don't try this at home. Carefully handcrafted clockwise connections.
			graph.connectRooms(0, room1);
			graph.connectRooms(0, room3);
			graph.connectRooms(0, room4);
			graph.connectRooms(room1, room2);
			graph.connectRooms(room2, room3);
			graph.connectRooms(room3, room4);

			break;

		case 3:
			roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			if (roomNr == -1)
				return;
			room1 = roomNr;

			while (roomNr == room1) {
				roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			}
			if (roomNr == -1)
				return;
			room2 = roomNr;

			while (roomNr == room1 || roomNr == room2) {
				roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			}
			if (roomNr == -1)
				return;
			room3 = roomNr;

			while (roomNr == room1 || roomNr == room2 || roomNr == room3) {
				roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			}
			if (roomNr == -1)
				return;
			room4 = roomNr;

			// don't try this at home. Carefully handcrafted clockwise connections.
			graph.connectRooms(0, room1);
			graph.connectRooms(0, room3);
			graph.connectRooms(0, room4);
			graph.connectRooms(room1, room2);
			graph.connectRooms(room2, room4);
			graph.connectRooms(room2, room3);

			break;

		case 4:
			roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			if (roomNr == -1)
				return;
			room1 = roomNr;

			while (roomNr == room1) {
				roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			}
			if (roomNr == -1)
				return;
			room2 = roomNr;

			while (roomNr == room1 || roomNr == room2) {
				roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			}
			if (roomNr == -1)
				return;
			room3 = roomNr;

			while (roomNr == room1 || roomNr == room2 || roomNr == room3) {
				roomNr = findRandomRoomWithDegreeAtLeast(3); // one extra slot
			}
			if (roomNr == -1)
				return;
			room4 = roomNr;

			// don't try this at home. Carefully handcrafted clockwise connections.
			graph.connectRooms(0, room1);
			graph.connectRooms(0, room3);
			graph.connectRooms(0, room4);
			graph.connectRooms(room1, room2);
			graph.connectRooms(room1, room3);
			graph.connectRooms(room2, room4);
			graph.connectRooms(room2, room3);

			break;

		case 5:
			roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			if (roomNr == -1)
				return;
			room1 = roomNr;

			while (roomNr == room1) {
				roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			}
			if (roomNr == -1)
				return;
			room2 = roomNr;

			while (roomNr == room1 || roomNr == room2) {
				roomNr = findRandomRoomWithDegreeAtLeast(4);
			}
			if (roomNr == -1)
				return;
			room3 = roomNr;

			while (roomNr == room1 || roomNr == room2 || roomNr == room3) {
				roomNr = findRandomRoomWithDegreeAtLeast(4); // one extra slot
			}
			if (roomNr == -1)
				return;
			room4 = roomNr;

			// don't try this at home. Carefully handcrafted clockwise connections.
			graph.connectRooms(0, room1);
			graph.connectRooms(0, room3);
			graph.connectRooms(0, room4);
			graph.connectRooms(room1, room2);
			int spawnIndex = graph.connectRooms(room1, room3);
			graph.connectRooms(room3, room4);
			graph.connectRooms(room2, room4);

			// in order to not break the clockwise order, the last edge needs to be placed between existing ones
			graph.connectRoomsAfterConnection(room2, room3, spawnIndex);

			break;

		default:
			Debug.Log("Entered an invalid number to select the base graph for graph generation.");
			break;
		}
	}

	// Go through all unconnected rooms and connect them to the connected section of the graph. The result must be planar as well.
	private void connectAllRooms() {
		for (int i = 1; i < graph.getTotalNumberOfRooms(); i++) {
			if (!graph.ABSTRACT_ROOMS[i].hasConnectedDoorSpawns()) {
				// This room is unconnected.

				// Decide randomly, how to connect.
				int rand = UnityEngine.Random.Range(1,4);
				switch (rand) {
				case 1:
				// Case 1: Connect it as a degree-1 vertex to any not-full room.
					int otherRoomID = findRandomRoomNotFullNotEmpty();
					if (!(otherRoomID == -1)) {
						graph.connectRoomsFullyRandomly(i, otherRoomID);
						break;
					}
					// If no other room has a free spot left, we must use case 2 (therefore no break here).
					goto case 2;

				case 2:
				// Case 2: Select an arbitrary edge and place it as a degree-2 vertex in the middle of it
					int room1ID = findRandomRoomNotEmpty();
					if (room1ID == -1) {
						Debug.Log("This should never happen. Cannot find a connected room.");
						return;
					}
					DummyData_DoorSpawn spawn1 = graph.ABSTRACT_ROOMS[room1ID].getRandomConnectedDoorSpawn();
					if (spawn1 == null) {
						Debug.Log("Tried to find a connected door spawn from non-empty room " + room1ID + ", but failed.");
						return;
					}
					int spawn2ID = spawn1.CONNECTS_TO_SPAWN_ID;
					int room2ID = graph.DOOR_SPAWN_IS_IN_ROOM[spawn2ID];

					// Now we've got two connected door spawn and room ids. We break open the connection and insert the room inbetween.
					graph.connectRoomsSemiRandomly(room1ID, i, spawn1.INDEX);
					graph.connectRoomsSemiRandomly(room2ID, i, spawn2ID);

					break;

				case 3:
				// Case 3: Select an arbitrary face. Place the new vertex in the "middle" of it.
				//		   Connect it to a number of vertices incident to the face.
					if (graph.ABSTRACT_ROOMS[i].MAX_NUM_OF_DOORS == 1)
						goto case 1;

					// We find a cycle by always using the leftest edge.
					// We store the ingoing ends of edges because we use the "connectRoomsAFTERConnection"-method later on
					List<int> spawnIDsOnCycle = new List<int>();
					List<int> roomIDsOnCycle = new List<int>();
					DummyData_DoorSpawn spawn = graph.getRandomConnectedDoorSpawn();
					if (spawn == null) {
						goto case 2;
					}
					int spawnID = spawn.INDEX;
					spawnIDsOnCycle.Add(spawnID);
					roomIDsOnCycle.Add(graph.DOOR_SPAWN_IS_IN_ROOM[spawnID]);
					spawn = graph.getNextLeftSpawn(spawnID); // left of the first one, in the same room.
					spawnID = spawn.CONNECTS_TO_SPAWN_ID; // ingoing door spawn in the next room

					while (spawnID != spawnIDsOnCycle[0]) {
						spawnIDsOnCycle.Add(spawnID);
						roomIDsOnCycle.Add(graph.DOOR_SPAWN_IS_IN_ROOM[spawnID]);

						spawn = graph.getNextLeftSpawn(spawnID);
						spawnID = spawn.CONNECTS_TO_SPAWN_ID;
					}

					// The cycle is found. Compute how many new connections are feasible.
					int maxPossibleConnections = graph.getRoomsMaxDoors(i);
					int count = 0;
					foreach (int r in roomIDsOnCycle) {
						if (!graph.isRoomFullyConnected(r))
							count++;
					}
					maxPossibleConnections = Mathf.Min(maxPossibleConnections, count);
					if (maxPossibleConnections <= 1) {
						int otherRand = UnityEngine.Random.Range(1, 3);
						if (otherRand == 1)
							goto case 1;
						else
							goto case 2;
					}

					// Select random (non-full) rooms on the cycle to use for new connections
					int numOfNewConnections = UnityEngine.Random.Range(2, maxPossibleConnections + 1);
					List<int> randomRoomIDs = new List<int>();
					count = 0;
					while (count < numOfNewConnections) {
						int randomRoomID = roomIDsOnCycle[UnityEngine.Random.Range(0, roomIDsOnCycle.Count)];
						if (!randomRoomIDs.Contains(randomRoomID)
						    	&& graph.ABSTRACT_ROOMS[randomRoomID].hasEmptyDoorSpawns()) {
							randomRoomIDs.Add(randomRoomID);
							count++;
						}
					}

					// Connect the new room to the selected ones in clockwise order. (backwards from how they were found)
					for (int j = roomIDsOnCycle.Count - 1; j >= 0; j--) {
						int rID = roomIDsOnCycle[j];
						if (randomRoomIDs.Contains(rID)) {
							graph.connectRoomsAfterConnection(i, rID, spawnIDsOnCycle[j]);
						}
					}

					break;

				default:
					break;
				}
			}
		}
	}

	// Finds random (unused) room's id which has at least the specified degree (max door spawns).
	private int findRandomRoomWithDegreeAtLeast(int minDegree) {
		int maxPatience = 1000;
		int roomNr = UnityEngine.Random.Range(1, graph.getTotalNumberOfRooms());
		int count = 0;

		while (graph.getRoomsMaxDoors(roomNr) < minDegree || graph.ABSTRACT_ROOMS[roomNr].hasConnectedDoorSpawns()) { 
			count++;
			if (count > maxPatience) {
				Debug.Log("Cannot find an unused room with degree at least " + minDegree);
				return -1;
			}

			roomNr = UnityEngine.Random.Range(1, graph.getTotalNumberOfRooms());
		}

		return roomNr;
	}

	// Finds a room which is already connected to the graph and has at least one free door spawn. The room's id is returned.
	private int findRandomRoomNotFullNotEmpty() {
		// prevent endless searching
		bool possible = false;
		for (int i = 1; i < graph.getTotalNumberOfRooms(); i++) {
			if (graph.ABSTRACT_ROOMS[i].hasEmptyDoorSpawns() && graph.ABSTRACT_ROOMS[i].hasConnectedDoorSpawns()) {
				possible = true;
			}
		}
		if (!possible)
			return -1;

		int roomIndex = UnityEngine.Random.Range(1, graph.getTotalNumberOfRooms());
		while (!graph.ABSTRACT_ROOMS[roomIndex].hasEmptyDoorSpawns() || !graph.ABSTRACT_ROOMS[roomIndex].hasConnectedDoorSpawns()) {
			roomIndex = UnityEngine.Random.Range(1, graph.getTotalNumberOfRooms());
		}
		return roomIndex;
	}

	// Finds a room which is already connected to the graph. The room's id is returned.
	private int findRandomRoomNotEmpty() {
		// prevent endless searching
		bool possible = false;
		for (int i = 1; i < graph.getTotalNumberOfRooms(); i++) {
			if (graph.ABSTRACT_ROOMS[i].hasConnectedDoorSpawns()) {
				possible = true;
			}
		}
		if (!possible)
			return -1;

		int roomIndex = UnityEngine.Random.Range(1, graph.getTotalNumberOfRooms());
		while (!graph.ABSTRACT_ROOMS[roomIndex].hasConnectedDoorSpawns()) {
			roomIndex = UnityEngine.Random.Range(1, graph.getTotalNumberOfRooms());
		}
		return roomIndex;
	}

	// Checks whether two left sides or two right sides are connected. If true, rotates rooms.
	// This can't solve every possible case. In an unlucky situations a room may be connected only to left side doors.
	private void checkSideConnections() {
		DummyData_AbstractRoom room, otherRoom;
		DummyData_DoorSpawn spawn, otherSpawn;

		for (int i = 0; i < graph.getTotalNumberOfRooms(); i++) {
			// Iterate over all rooms
			room = graph.ABSTRACT_ROOMS[i];

			for (int j = 0; j < room.MAX_NUM_OF_DOORS; j++) {
				// Iterate over all door spawns
				spawn = room.DOOR_SPAWNS.Values[j];
				if (spawn.isConnected() && (spawn.LEFT_SIDE || spawn.RIGHT_SIDE)) {
					int otherSpawnID = spawn.CONNECTS_TO_SPAWN_ID;
					int otherRoomID = graph.DOOR_SPAWN_IS_IN_ROOM[otherSpawnID];
					otherRoom = graph.ABSTRACT_ROOMS[otherRoomID];
					otherSpawn = otherRoom.DOOR_SPAWNS[otherSpawnID];

					if ((spawn.LEFT_SIDE && otherSpawn.LEFT_SIDE) || (spawn.RIGHT_SIDE && otherSpawn.RIGHT_SIDE)) {
						// Unwanted connection. Rotate otherRoom
						otherRoom.rotate();
					}
				}
			}
		}
	}

}
