using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DummyData_Graph {

	[SerializeField]
	public SortedList<int, DummyData_AbstractRoom> ABSTRACT_ROOMS;

	private int doorSpawnIDcounter;

	public DummyData_Graph() {
		ABSTRACT_ROOMS = new SortedList<int, DummyData_AbstractRoom>();
		doorSpawnIDcounter = 0;
	}

	// Adds an abstract room to the list which has maxDoors door spawns.
	public void addRoom(int maxDoors)
	{
		int INDEX = ABSTRACT_ROOMS.Count;
		ABSTRACT_ROOMS.Add(INDEX, new DummyData_AbstractRoom(INDEX, maxDoors));
	}

	// Adds a door spawn to the specified room. left/rightSide determine whether the door is located on the left or right end of the room
	public void addDoorSpawn(int roomIndex, bool leftSide, bool rightSide) {
		int doorSpawnIndex = doorSpawnIDcounter;
		doorSpawnIDcounter++;
		ABSTRACT_ROOMS[roomIndex].addDoorSpawn(doorSpawnIndex, leftSide, rightSide);
	}
}
