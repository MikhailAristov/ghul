using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Control_GraphMixup : MonoBehaviour {

	private const int MAX_EVILNESS = 4;

	// Changes the graph layout slightly to frustrate the player, mwhahaha.
	// degreeOfEvilness = number of potential changes. (upper bound)
	public static Data_Graph MixUpGraph(Data_Graph g, int degreeOfEvilness) {
		Data_Graph graph = g;
		if (degreeOfEvilness <= 0) { return graph; }
		if (degreeOfEvilness > MAX_EVILNESS) { degreeOfEvilness = MAX_EVILNESS; }

		// DEBUG ---------------------
		degreeOfEvilness = MAX_EVILNESS;

		for (int i = 0; i < degreeOfEvilness; i++) {
			// Find a connection that has been used the most, but don't take the ritual room's doors too often.
			int maxUses = 0;
			Data_GraphDoorSpawn chosenSpawn = null;
			bool ritualRoomDoorChosen = false;

			foreach (Data_GraphDoorSpawn spawn in graph.DOOR_SPAWNS.Values) {
				int uses = spawn.NUM_USES;
				int roomID = graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX];

				if (spawn.CONNECTS_TO_SPAWN_ID == -1) {
					spawn.resetNumUses();
					continue;
				}

				int otherRoomID = graph.DOOR_SPAWN_IS_IN_ROOM[spawn.CONNECTS_TO_SPAWN_ID];
				bool ritualRoomInvolved = false;
				if (roomID == 0 || otherRoomID == 0) { ritualRoomInvolved = true; }

				if (uses > maxUses) {
					if (!ritualRoomInvolved) {
						maxUses = uses;
						chosenSpawn = spawn;
						ritualRoomDoorChosen = false;
					} else {
						// only rarely change ritual room doors
						if (UnityEngine.Random.Range(0.0f, 1.0f) >= 0.75f) {
							maxUses = uses;
							chosenSpawn = spawn;
							ritualRoomDoorChosen = true;
						}
					}
				} else if (uses == maxUses && !ritualRoomInvolved) {
					// 50% chance of selecting the new spawn in ties
					float rand = UnityEngine.Random.Range(0.0f, 2.0f);
					if (rand >= 1.0f) { 
						chosenSpawn = spawn;
						ritualRoomDoorChosen = false;
					}
				}
			}
			if (chosenSpawn == null) {
				Debug.Log("Failed at selecting a door for mixup.");
				break;
			}

			// Select a mix up technique.
			int techniqueNr;
			if (!ritualRoomDoorChosen) { techniqueNr = (int)UnityEngine.Random.Range(1.0f, 4.0f); } // Possible values: 1,2,3
			else { techniqueNr = 2; } // Only rotation

			// DEBUG ------------------------------------------
			techniqueNr = 1;
			Debug.Log("Chosen Spawn: " + chosenSpawn.INDEX + ", uses: " + chosenSpawn.NUM_USES);

			switch (techniqueNr) {
			case 1:
				// Remove the connection (if possible)
				graph = Control_GraphMixup.removeConnection(chosenSpawn, graph);
				break;
			case 2:
				// Rotate the room
				graph = Control_GraphMixup.rotateRoom(chosenSpawn, graph);
				break;
			case 3:
				// Reconnect the spawn to another one (if possible)
				graph = Control_GraphMixup.reconnection(chosenSpawn, graph);
				break;
			default:
				break;
			}
		}

		return graph;
	}

	// Removes the spawn's connection if:
	// 1. It isn't connected to the ritual room
	// 2. The removal doesn't divide the graph into multiple components
	// 3. The removal doesn't potentially leave the ritual room as a 1-separator
	private static Data_Graph removeConnection(Data_GraphDoorSpawn spawn, Data_Graph graph) {
		// Reset the number of uses for this connection (also reset when mix up fails to avoid reselecting this connection too often)
		spawn.resetNumUses();
		Data_GraphDoorSpawn otherSpawn = graph.DOOR_SPAWNS[spawn.CONNECTS_TO_SPAWN_ID];
		otherSpawn.resetNumUses();

		// Connection to ritual room detected.
		if (graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX] == 0 || graph.DOOR_SPAWN_IS_IN_ROOM[spawn.CONNECTS_TO_SPAWN_ID] == 0) {
			return graph;
		}

		bool connectionIsSeparator = false;
		bool ritualRoomPotentiallySeparator = false;
		// Remove the connection. If the mix up fails, reconnect it afterwards.
		spawn.disconnect();
		otherSpawn.disconnect();

		Data_GraphDoorSpawn iteratorSpawn = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX]].getNextConnectedSpawn(spawn.INDEX);
		if (iteratorSpawn.INDEX == spawn.INDEX) {
			connectionIsSeparator = true; // The room has only one door. Can't remove it.
		}
		Data_GraphDoorSpawn checkerSpawn = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[otherSpawn.INDEX]].getNextConnectedSpawn(otherSpawn.INDEX);
		if (checkerSpawn.INDEX == otherSpawn.INDEX) {
			connectionIsSeparator = true; // The other room has only one door. Can't remove it.
		}

		// Go in a circle
		int i = 0; // Stop if the search takes too long.
		while (i < 100) {
			i++;
			if (graph.DOOR_SPAWN_IS_IN_ROOM[iteratorSpawn.INDEX] == 0) {
				ritualRoomPotentiallySeparator = true;
				break;
			}
			Debug.Log("iteratorspawn.connectsTo: " + iteratorSpawn.CONNECTS_TO_SPAWN_ID); //----------------------------------------
			Debug.Log("is in room: " + graph.DOOR_SPAWN_IS_IN_ROOM[iteratorSpawn.CONNECTS_TO_SPAWN_ID]);
			Data_GraphRoomVertice nextRoom = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[iteratorSpawn.CONNECTS_TO_SPAWN_ID]];
			if (nextRoom.hasDoorSpawnWithIndex(otherSpawn.INDEX)) {
				// Found a different path to the other room.
				break;
			}
			if (nextRoom.hasDoorSpawnWithIndex(spawn.INDEX)) {
				// Returned to the first spawn without finding the other one. Graph disconnected.
				connectionIsSeparator = true;
				break;
			}
			iteratorSpawn = nextRoom.getNextConnectedSpawn(iteratorSpawn.CONNECTS_TO_SPAWN_ID);
		}
			
		if (connectionIsSeparator || ritualRoomPotentiallySeparator) {
			// Removal failed
			spawn.connectTo(otherSpawn.INDEX);
			otherSpawn.connectTo(spawn.INDEX);
			Debug.Log("Cannot remove connection between door spawns " + spawn.INDEX + " and " + otherSpawn.INDEX);
			Debug.Log("connection is separator: " + connectionIsSeparator + ", ritualRoomNearby: " + ritualRoomPotentiallySeparator);
		} else {
			Debug.Log("Removed connection between door spawns " + spawn.INDEX + " and " + otherSpawn.INDEX);
		}

		return graph;
	}

	// Rotates the room the spawn is located in.
	private static Data_Graph rotateRoom(Data_GraphDoorSpawn spawn, Data_Graph graph) {
		Data_GraphRoomVertice room = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX]];

		// Reset the number of uses for all of the room's connections
		foreach (Data_GraphDoorSpawn s in room.DOOR_SPAWNS.Values) {
			s.resetNumUses();
			if (s.CONNECTS_TO_SPAWN_ID != -1) {
				graph.DOOR_SPAWNS[s.CONNECTS_TO_SPAWN_ID].resetNumUses();
			}
		}

		int numOfRotations = (int)UnityEngine.Random.Range(1.0f, room.MAX_NUM_OF_DOORS);
		for (int i = 0; i < numOfRotations; i++) {
			room.rotate();
		}

		Debug.Log("Rotated room " + room.INDEX + " exactly " + numOfRotations + " times.");

		return graph;
	}

	// Reconnect the two door spawns to different ones (if possible without complications)
	private static Data_Graph reconnection(Data_GraphDoorSpawn spawn, Data_Graph graph) {
		// Reset the number of uses for this connection (also reset when mix up fails to avoid reselecting this connection too often)
		spawn.resetNumUses();
		Data_GraphDoorSpawn otherSpawn = graph.DOOR_SPAWNS[spawn.CONNECTS_TO_SPAWN_ID];
		otherSpawn.resetNumUses();

		// Connection to ritual room detected.
		if (graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX] == 0 || graph.DOOR_SPAWN_IS_IN_ROOM[spawn.CONNECTS_TO_SPAWN_ID] == 0) {
			return graph;
		}

		bool connectionIsSeparator = false;
		bool ritualRoomPotentiallySeparator = false;
		// Remove the connection. If the mix up fails, reconnect it afterwards.
		spawn.disconnect();
		otherSpawn.disconnect();

		Data_GraphDoorSpawn iteratorSpawn = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX]].getNextConnectedSpawn(spawn.INDEX);
		if (iteratorSpawn.INDEX == spawn.INDEX) {
			connectionIsSeparator = true; // The room has only one door. Can't remove it.
		}
		Data_GraphDoorSpawn checkerSpawn = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[otherSpawn.INDEX]].getNextConnectedSpawn(otherSpawn.INDEX);
		if (checkerSpawn.INDEX == otherSpawn.INDEX) {
			connectionIsSeparator = true; // The other room has only one door. Can't remove it.
		}

		List<int> outgoingSpawnIdsOnCycle = new List<int>();
		outgoingSpawnIdsOnCycle.Add(iteratorSpawn.INDEX);

		// Go in a circle
		int i = 0; // Stop if the search takes too long.
		while (i < 100) {
			i++;
			if (graph.DOOR_SPAWN_IS_IN_ROOM[iteratorSpawn.INDEX] == 0) {
				ritualRoomPotentiallySeparator = true;
				break;
			}
			Data_GraphRoomVertice nextRoom = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[iteratorSpawn.CONNECTS_TO_SPAWN_ID]];
			if (nextRoom.hasDoorSpawnWithIndex(otherSpawn.INDEX)) {
				// Found a different path to the other room.
				break;
			}
			if (nextRoom.hasDoorSpawnWithIndex(spawn.INDEX)) {
				// Returned to the first spawn without finding the other one. Graph disconnected.
				connectionIsSeparator = true;
				break;
			}
			iteratorSpawn = nextRoom.getNextConnectedSpawn(iteratorSpawn.CONNECTS_TO_SPAWN_ID);
			outgoingSpawnIdsOnCycle.Add(iteratorSpawn.INDEX);
		}

		if (connectionIsSeparator || ritualRoomPotentiallySeparator) {
			// Removal failed
			spawn.connectTo(otherSpawn.INDEX);
			otherSpawn.connectTo(spawn.INDEX);
			Debug.Log("Cannot reconnect door spawns " + spawn.INDEX + " and " + otherSpawn.INDEX + "with other spawns.");
		} else {
			// Select a random connection on the cycle.
			int r = (int)UnityEngine.Random.Range(0.0f, outgoingSpawnIdsOnCycle.Count);
			int firstSpawnID = outgoingSpawnIdsOnCycle[r];
			Data_GraphDoorSpawn firstSpawn = graph.DOOR_SPAWNS[firstSpawnID];
			Data_GraphDoorSpawn secondSpawn = graph.DOOR_SPAWNS[firstSpawn.CONNECTS_TO_SPAWN_ID];

			firstSpawn.connectTo(spawn.INDEX);
			spawn.connectTo(firstSpawn.INDEX);
			secondSpawn.connectTo(otherSpawn.INDEX);
			otherSpawn.connectTo(secondSpawn.INDEX);
			firstSpawn.resetNumUses();
			secondSpawn.resetNumUses();

			Debug.Log("Reconnection: (" + spawn.INDEX + "," + otherSpawn.INDEX + "), (" + firstSpawn.INDEX + "," + secondSpawn.INDEX + ") -> ("
				+ spawn.INDEX + "," + firstSpawn.INDEX + "), (" + otherSpawn.INDEX + "," + secondSpawn.INDEX + "). These are door spawn IDs.");
		}

		return graph;
	}
}
