using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Factory_Graph : MonoBehaviour {

	private DummyData_Graph graph;
	private bool graphCalculated;

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

		// Step 3: Adjust degrees.

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
			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
			if (roomNr == -1)
				return;
			room1 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room2 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
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
			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
			if (roomNr == -1)
				return;
			room1 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
			if (roomNr == -1)
				return;
			room2 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room3 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
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
			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
			if (roomNr == -1)
				return;
			room1 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room2 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
			if (roomNr == -1)
				return;
			room3 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
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
			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room1 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room2 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room3 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 2);
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
			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room1 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
			if (roomNr == -1)
				return;
			room2 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 4);
			if (roomNr == -1)
				return;
			room3 = roomNr;

			roomNr = findNextRoomWithDegreeAtLeast(roomNr, 3);
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
				// DEBUG -----------------------
				rand = UnityEngine.Random.Range(1,3);
				// END OF DEBUG ----------------
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
				// Case 3: Select an arbitrary face (may require reselection if face doesn't provide free spots). Place the new vertex in the "middle" of it.
				//		   Connect it to a number of vertices incident to the face.

					break;

				default:
					break;
				}
			}
		}
	}

	// Finds the next room's id which has at least the specified degree (max door spawns).
	private int findNextRoomWithDegreeAtLeast(int startingID, int minDegree) {
		int infiniteLoopPrevention = startingID + 1;
		int roomNr = startingID + 1;
		if (roomNr >= graph.getTotalNumberOfRooms()) {
			infiniteLoopPrevention = 1;
			roomNr = 1;
		}

		while (graph.getRoomsMaxDoors(roomNr) < minDegree || graph.ABSTRACT_ROOMS[roomNr].hasConnectedDoorSpawns()) { 
			roomNr++;
			if (roomNr >= graph.getTotalNumberOfRooms()) {
				roomNr = 1;
			}
			if (roomNr == infiniteLoopPrevention) {
				Debug.Log("Impossible to construct the desired graph.");
				return -1;
			}
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
}
