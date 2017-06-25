using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Control_GraphMixup : MonoBehaviour {

	private const int MAX_EVILNESS = 4;

	// Changes the graph layout slightly to frustrate the player, mwhahaha.
	// degreeOfEvilness = number of potential changes. (upper bound)
	public static void MixUpGraph(ref Data_Graph graph, int degreeOfEvilness) {
		degreeOfEvilness = Mathf.Clamp(degreeOfEvilness, 0, MAX_EVILNESS);

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

			// DEBUG
			//techniqueNr = 3; // <- Wenn nicht auskommentiert, wird der Zusammenhangsfehler wahrscheinlicher (zum Testen).

			switch (techniqueNr) {
			case 1:
				// Remove the connection (if possible)
				Control_GraphMixup.removeConnection(chosenSpawn, ref graph);
				break;
			case 2:
				// Rotate the room
				Control_GraphMixup.rotateRoom(chosenSpawn, ref graph);
				break;
			case 3:
				// Reconnect the spawn to another one (if possible)
				Control_GraphMixup.reconnection(chosenSpawn, ref graph);
				break;
			default:
				break;
			}


		}
		// Check whether a left side is connected to another left side (or the same for the right side) and rotating rooms accordingly.
		Control_GraphMixup.checkSideConnections(ref graph);
		//TODO: Get this working again
		//Control_GraphMixup.dijkstraConnectionTest(ref graph);

		// DEBUG
		//graph.printCompleteGraphInformation();
		// END OF DEBUG
	}

	private static void dijkstraConnectionTest(ref Data_Graph graph) {
		int[] status = new int[graph.ABSTRACT_ROOMS.Count]; // status: 0 = not discovered, 1 = discovered, 2 = relaxations completed
		status[0] = 1;
		for (int i = 1; i < status.Length; i++) {
			status[i] = 0;
		}
		bool repeat = true;
		bool error = false;

		int safetyCounter = 0;
		while (repeat && safetyCounter < 1000) {
			repeat = false;
			safetyCounter++;

			for (int i = 0; i < graph.ABSTRACT_ROOMS.Count; i++) {
				// Go through the rooms and relax outgoing edges from those with status 1
				if (status[i] == 1) {
					repeat = true;
					Data_GraphRoomVertice room = graph.ABSTRACT_ROOMS[i];
					for (int j = 0; j < room.DOOR_SPAWNS.Count; j++) {
						Data_GraphDoorSpawn spawn = room.DOOR_SPAWNS.Values[j];
						if (spawn.isConnected()) {
							// Relax edge
							int otherRoomID = graph.DOOR_SPAWN_IS_IN_ROOM[spawn.CONNECTS_TO_SPAWN_ID];
							if (status[otherRoomID] == 0) {
								status[otherRoomID] = 1;
							}
						}
					}
					// Room finished
					status[i] = 2;
				}
			}

			if (safetyCounter >= 1000) {
				Debug.Log("Endless Loop in Disjkstra Connection Check (1)");
				error = true;
				break;
			}
		}

		bool disconnect = false;
		int disconnectedRoom = -1;
		if (!error) {
			// If there's still a status 0 room, there's a disconnection.
			for (int i = 0; i < status.Length; i++) {
				if (status[i] == 0) {
					disconnect = true;
					disconnectedRoom = i;
					break;
				}
			}

			// Try reconnecting the room to the graph.
			if (disconnect) {
				// Find a room to connect to that is reachable from the ritual room.
				int otherRoomId = -1;
				int safety = 0;
				while (otherRoomId == -1 || status[otherRoomId] != 2) {
					safety++;
					otherRoomId = Factory_Graph.findRandomRoomNotFullNotEmpty(ref graph);

					if (safety >= 1000) {
						Debug.Log("Endless Loop in Disjkstra Connection Check (2)");
						return;
					}
				}
				// Connecting the two rooms
				if (otherRoomId != -1) {
					Data_GraphRoomVertice room = graph.ABSTRACT_ROOMS[disconnectedRoom];
					Data_GraphDoorSpawn spawn = room.getRandomEmptyDoorSpawn();
					if (spawn == null) {
						// The disconnected room doesn't have free door spawns. Cutting one of them.
						spawn = room.getRandomConnectedDoorSpawn();
						spawn.disconnect();
					}
					Data_GraphRoomVertice otherRoom = graph.ABSTRACT_ROOMS[otherRoomId];
					Data_GraphDoorSpawn otherSpawn = otherRoom.getRandomEmptyDoorSpawn();
					spawn.connectTo(otherSpawn.INDEX);
					otherSpawn.connectTo(spawn.INDEX);
					Debug.Log("Dijkstra-Check discovered a disconnection. Connected spawns (" + spawn.INDEX + ", " + otherSpawn.INDEX + ") in rooms (" + room.INDEX + ", " + otherRoom.INDEX + ")");
				}

				// Rerun the Side check
				Control_GraphMixup.checkSideConnections(ref graph);

				// Rerun the Disjkstra check
				Control_GraphMixup.dijkstraConnectionTest(ref graph);
			}
		}
	}

	// Removes the spawn's connection if:
	// 1. It isn't connected to the ritual room
	// 2. The removal doesn't divide the graph into multiple components
	// 3. The removal doesn't potentially leave the ritual room as a 1-separator
	private static void removeConnection(Data_GraphDoorSpawn spawn, ref Data_Graph graph) {
		// Reset the number of uses for this connection (also reset when mix up fails to avoid reselecting this connection too often)
		spawn.resetNumUses();
		Data_GraphDoorSpawn otherSpawn = graph.DOOR_SPAWNS[spawn.CONNECTS_TO_SPAWN_ID];
		otherSpawn.resetNumUses();

		// Connection to ritual room detected.
		if (graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX] == 0 || graph.DOOR_SPAWN_IS_IN_ROOM[spawn.CONNECTS_TO_SPAWN_ID] == 0) {
			return;
		}

		bool connectionIsSeparator = false;
		bool ritualRoomPotentiallySeparator = false;
		bool otherError = false;
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
			if (iteratorSpawn.CONNECTS_TO_SPAWN_ID == -1) {
				// Error, got an unconnected spawn.
				Debug.Log("Tried accessing a connected spawn during mixup but got an unconnected.");
				otherError = true;
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

			if (i >= 100) {
				// Search takes too long.
				otherError = true;
			}
		}
			
		if (connectionIsSeparator || ritualRoomPotentiallySeparator || otherError) {
			// Removal failed
			spawn.connectTo(otherSpawn.INDEX);
			otherSpawn.connectTo(spawn.INDEX);
			//Debug.Log("Cannot remove connection between door spawns " + spawn.INDEX + " and " + otherSpawn.INDEX);
			//Debug.Log("connection is separator: " + connectionIsSeparator + ", ritualRoomNearby: " + ritualRoomPotentiallySeparator + ", other Error: " + otherError);
		} else {
			foreach (Data_GraphRoomVertice vertex in graph.ABSTRACT_ROOMS.Values) {
				vertex.updateNumDoors();
			}
			Debug.Log("Removed connection between door spawns " + spawn.INDEX + " and " + otherSpawn.INDEX);
		}
	}

	// Rotates the room the spawn is located in.
	private static void rotateRoom(Data_GraphDoorSpawn spawn, ref Data_Graph graph) {
		Data_GraphRoomVertice room = graph.ABSTRACT_ROOMS[graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX]];

		// Reset the number of uses for all of the room's connections
		foreach (Data_GraphDoorSpawn s in room.DOOR_SPAWNS.Values) {
			s.resetNumUses();
			if (s.CONNECTS_TO_SPAWN_ID != -1) {
				graph.DOOR_SPAWNS[s.CONNECTS_TO_SPAWN_ID].resetNumUses();
			}
		}

		int numOfRotations = (int)UnityEngine.Random.Range(1.0f, room.MAX_NUM_OF_DOORS);
		bool rotationSuccessful = true;
		for (int i = 0; i < numOfRotations; i++) {
			rotationSuccessful = room.rotate();
			if (!rotationSuccessful) {
				break;
			}
		}

		if (rotationSuccessful) {
			foreach (Data_GraphRoomVertice vertex in graph.ABSTRACT_ROOMS.Values) {
				vertex.updateNumDoors();
			}
			Debug.Log("Rotated room " + room.INDEX + " exactly " + numOfRotations + " times.");
		}
	}

	// Reconnect the two door spawns to different ones (if possible without complications)
	private static void reconnection(Data_GraphDoorSpawn spawn, ref Data_Graph graph) {
		// Reset the number of uses for this connection (also reset when mix up fails to avoid reselecting this connection too often)
		spawn.resetNumUses();
		Data_GraphDoorSpawn otherSpawn = graph.DOOR_SPAWNS[spawn.CONNECTS_TO_SPAWN_ID];
		otherSpawn.resetNumUses();

		// Connection to ritual room detected.
		if (graph.DOOR_SPAWN_IS_IN_ROOM[spawn.INDEX] == 0 || graph.DOOR_SPAWN_IS_IN_ROOM[spawn.CONNECTS_TO_SPAWN_ID] == 0) {
			return;
		}

		bool connectionIsSeparator = false;
		bool ritualRoomPotentiallySeparator = false;
		bool selfConnectedRoom = false;
		bool otherError = false;

		if (spawn.roomId == otherSpawn.roomId) {
			selfConnectedRoom = true;
		}

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
			if (iteratorSpawn.CONNECTS_TO_SPAWN_ID == -1) {
				// Error, got an unconnected spawn.
				Debug.Log("Tried accessing a connected spawn during mixup but got an unconnected.");
				otherError = true;
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

			if (i >= 100) {
				// Search takes too long
				otherError = true;
			}
		}

		if (connectionIsSeparator || ritualRoomPotentiallySeparator || selfConnectedRoom || otherError) {
			// Removal failed
			spawn.connectTo(otherSpawn.INDEX);
			otherSpawn.connectTo(spawn.INDEX);
			//Debug.Log("Cannot reconnect door spawns " + spawn.INDEX + " and " + otherSpawn.INDEX + " with other spawns.");
			//Debug.Log("connection is separator: " + connectionIsSeparator + ", ritualRoomNearby: " + ritualRoomPotentiallySeparator + 
			//	", selfConnectedRoom: " + selfConnectedRoom + ", other Error: " + otherError);
		} else {
			// Select a random connection on the cycle.
			int r = (int)UnityEngine.Random.Range(0.0f, outgoingSpawnIdsOnCycle.Count);
			int firstSpawnID = outgoingSpawnIdsOnCycle[r];
			Data_GraphDoorSpawn firstSpawn = graph.DOOR_SPAWNS[firstSpawnID];
			Data_GraphDoorSpawn secondSpawn = graph.DOOR_SPAWNS[firstSpawn.CONNECTS_TO_SPAWN_ID];

			// Not allowed: Connecting a room with itself. Can absolutely screw up the graph.
			if (firstSpawn.roomId == spawn.roomId || secondSpawn.roomId == otherSpawn.roomId) {
				// Removal failed
				spawn.connectTo(otherSpawn.INDEX);
				otherSpawn.connectTo(spawn.INDEX);
				//Debug.Log("Tried reconnecting the door spawn connection (" + spawn.INDEX + ", " + otherSpawn.INDEX + "), but would have connected room to itself. Aborted.");
				return;
			}

			firstSpawn.connectTo(spawn.INDEX);
			spawn.connectTo(firstSpawn.INDEX);
			secondSpawn.connectTo(otherSpawn.INDEX);
			otherSpawn.connectTo(secondSpawn.INDEX);
			firstSpawn.resetNumUses();
			secondSpawn.resetNumUses();

			foreach (Data_GraphRoomVertice vertex in graph.ABSTRACT_ROOMS.Values) {
				vertex.updateNumDoors();
			}
			Debug.Log("Reconnection: (" + spawn.INDEX + "," + otherSpawn.INDEX + "), (" + firstSpawn.INDEX + "," + secondSpawn.INDEX + ") -> ("
				+ spawn.INDEX + "," + firstSpawn.INDEX + "), (" + otherSpawn.INDEX + "," + secondSpawn.INDEX + "). These are door spawn IDs.");
		}
	}

	// Checks whether two left sides or two right sides are connected. If true, rotates rooms.
	private static void checkSideConnections(ref Data_Graph graph) {
		Data_GraphRoomVertice room, otherRoom;
		Data_GraphDoorSpawn spawn, otherSpawn;
		bool rotationNeeded = false;

		int safetyCounterOuter = 0;
		for (int i = 0; i < graph.getTotalNumberOfRooms(); i++) {
			// Iterate over all rooms
			room = graph.ABSTRACT_ROOMS[i];

			int safetyCounterInner = 0;
			for (int j = 0; j < room.MAX_NUM_OF_DOORS; j++) {
				// Iterate over all door spawns
				spawn = room.DOOR_SPAWNS.Values[j];
				if (spawn.isConnected() && (spawn.LEFT_SIDE || spawn.RIGHT_SIDE)) {
					int otherSpawnID = spawn.CONNECTS_TO_SPAWN_ID;
					int otherRoomID = graph.DOOR_SPAWN_IS_IN_ROOM[otherSpawnID];
					otherRoom = graph.ABSTRACT_ROOMS[otherRoomID];
					otherSpawn = otherRoom.DOOR_SPAWNS[otherSpawnID];

					if ((spawn.LEFT_SIDE && otherSpawn.LEFT_SIDE) || (spawn.RIGHT_SIDE && otherSpawn.RIGHT_SIDE)) {
						// Rotate otherRoom. If we rotate room, all doors may be connected to side doors which make it impossible to find a fitting constellation.
						otherRoom.rotate();
						rotationNeeded = true;
						// Iterate over all rooms again
						i = 0;
						j = 0;
						break;
					}
				}

				safetyCounterInner++;
				if (safetyCounterInner >= 1000) {
					// Room rotates over and over again but has no position that meets our goals
					Debug.Log("checkSideConnections (Mixup): Endless Loop detected (1)");
					break;
				}
			}

			safetyCounterOuter++;
			if (safetyCounterOuter >= 200) {
				// This shouldn't even be possible but you never know.
				Debug.Log("checkSideConnections (Mixup): Endless Loop detected (2)");
				break;
			}
		}

		if (rotationNeeded) {
			Debug.Log("Left-Left or Right-Right connection after House Mixup. Rotated to correct it.");
		}
	}
}
