using UnityEngine;
using System.Collections;

// Testing of graph creation.
public class Test_Graph : MonoBehaviour {

	private DummyData_Graph graph;
	private Factory_Graph factory;
	int roomIndex;

	// Use this for initialization
	void Start () {
		Debug.Log("GRAPH TESTER START");

		factory = GetComponent<Factory_Graph>();
		graph = new DummyData_Graph();
		roomIndex = 0;

		// Construction of rooms and door spawns (without any connections).
		constructGraphVertices();
		printBasicVertexInformation();

		// Construction of a base graph.
		factory.computePlanarGraph(graph);
		Debug.Log("Graph constructed.");
		graph = factory.GetGraph();
		printCompleteGraphInformation();

		Debug.Log("GRAPH TESTER FINISHED.");
	}

	// Creates the graph object that will be the input for the factory.
	private void constructGraphVertices() {
		graph.addRoom(3); // ritual room always has a degree of 3.
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(2);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);

		roomIndex++;
		graph.addRoom(4);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);

		roomIndex++;
		graph.addRoom(4);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);

		roomIndex++;
		graph.addRoom(2);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(5);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(3);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(4);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(2);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(3);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(5);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);

		roomIndex++;
		graph.addRoom(7);
		graph.addDoorSpawn(roomIndex, true, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, false);
		graph.addDoorSpawn(roomIndex, false, true);
	}

	// Prints for each room the number of door spawns and whether they are on the left/right side
	private void printBasicVertexInformation() {
		string infoText = "Graph information before calling the factory method:\n";
		infoText += "The graph has " + graph.getTotalNumberOfRooms() + " rooms and " 
			+ graph.getTotalNumberOfDoorSpawns() + " door spawns.\n";
		for (int i = 0; i <= roomIndex; i++) {
			DummyData_AbstractRoom room = graph.ABSTRACT_ROOMS[i];
			infoText += "Room " + i + ": " + room.MAX_NUM_OF_DOORS + " door spawns total. Spawns: ";
			foreach (DummyData_DoorSpawn spawn in room.DOOR_SPAWNS.Values) {
				infoText += "" + spawn.INDEX;
				if (spawn.LEFT_SIDE)
					infoText += " LEFT";
				if (spawn.RIGHT_SIDE)
					infoText += " RIGHT";
				infoText += ", ";
			}
			infoText += "\n";
		}

		Debug.Log(infoText);
	}

	// Prints for each room which door spawns are in use and where they connect to.
	private void printCompleteGraphInformation() {
		if (!factory.graphCalculated)
			return;
		string infoText = "Complete Graph Information:\n";
		infoText += "The graph has " + graph.getTotalNumberOfRooms() + " rooms and " 
				+ graph.getTotalNumberOfDoorSpawns() + " door spawns.\n";

		// Calculate the vertex degrees.
		int[] degreeDistribution = new int[] {0,0,0,0,0,0,0,0,0,0};
		int maxDegree = 0;
		for (int k = 0; k < graph.getTotalNumberOfRooms(); k++) {
			int deg = graph.ABSTRACT_ROOMS[k].NUM_OF_DOORS;
			maxDegree = Mathf.Max(maxDegree, deg);
			if (deg <= 0 || deg >= 10) {
				Debug.Log("Room " + k + "has a degree it should not have.");
				return;
			}
			degreeDistribution[deg]++;
		}
		infoText += "Degree Distribution:\n";
		for (int l = 1; l <= maxDegree; l++) {
			infoText += "\tDegree " + l + ": " + degreeDistribution[l] + " rooms.\n";
		}
		infoText += "\n";

		for (int i = 0; i <= roomIndex; i++) {
			DummyData_AbstractRoom room = graph.ABSTRACT_ROOMS[i];
			infoText += "Room " + i + ": " + room.MAX_NUM_OF_DOORS + " door spawns total. " 
				+ room.NUM_OF_DOORS + " connections total.\n";
			foreach (DummyData_DoorSpawn spawn in room.DOOR_SPAWNS.Values) {
				infoText += "\tSpawn " + spawn.INDEX;
				if (spawn.LEFT_SIDE)
					infoText += " LEFT";
				if (spawn.RIGHT_SIDE)
					infoText += " RIGHT";
				infoText += ", ";
				if (!spawn.isConnected())
					infoText += "unconnected";
				else
					infoText += "connected to " + spawn.CONNECTS_TO_SPAWN_ID + " (Room " 
						+ graph.DOOR_SPAWN_IS_IN_ROOM[spawn.CONNECTS_TO_SPAWN_ID] + ")";
				infoText += "\n";
			}
			infoText += "\n";
		}

		Debug.Log(infoText);
	}
}
