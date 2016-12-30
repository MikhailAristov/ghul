using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class Factory_Graph : MonoBehaviour {

	private DummyData_Graph graph;
	private bool graphCalculated;
	private int numOfRoomsConnected; // used when constructing the graph. Goes from 0 to the number of rooms

	void Start() {
		graphCalculated = false;
		numOfRoomsConnected = 0;
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
		numOfRoomsConnected = 0;
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

		// Step 2: Connect vertices to the connected graph such that the resulting is planar again.

		// Step 3: Adjust degrees.

		graphCalculated = true;
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
		int roomNr = 1;
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
			graph.connectRooms(room2, room3);
			graph.connectRooms(room2, room4);

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
			graph.connectRoomAfterConnection(room2, room3, spawnIndex);

			break;

		default:
			Debug.Log("Entered an invalid number to select the base graph for graph generation.");
			break;
		}
	}

	// Finds the next room's id which has at least the specified degree (max door spawns).
	private int findNextRoomWithDegreeAtLeast(int startingID, int minDegree) {
		int infiniteLoopPrevention = startingID;
		int roomNr = startingID;
		while (graph.getRoomsMaxDoors(roomNr) < minDegree) { 
			roomNr++;
			if (roomNr > graph.getTotalNumberOfRooms()) {
				roomNr = 1;
			}
			if (roomNr == infiniteLoopPrevention) {
				Debug.Log("Impossible to construct the desired graph.");
				return -1;
			}
		}
		return roomNr;
	}
}
