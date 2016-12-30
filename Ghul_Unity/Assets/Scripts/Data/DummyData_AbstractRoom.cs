using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DummyData_AbstractRoom {

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

	// Actual number of doors that exist. NUM_OF_DOORS <= MAX_NUM_OF_DOORS.
	/*[SerializeField]
	private int _NUM_OF_DOORS;
	public int NUM_OF_DOORS {
		get { return _NUM_OF_DOORS; }
		private set { _NUM_OF_DOORS = value;  }
	}*/

	[SerializeField]
	public SortedList<int, DummyData_DoorSpawn> DOOR_SPAWNS;

	public DummyData_AbstractRoom(int I, int MAX_DOORS) {
		INDEX = I;
		MAX_NUM_OF_DOORS = MAX_DOORS;
		DOOR_SPAWNS = new SortedList<int, DummyData_DoorSpawn>();
	}

	// - Note - When does the linking of doors to door spawns exactly happen?

	// Adds a door spawn to the list
	public void addDoorSpawn(int index, bool leftSide, bool rightSide) {
		DOOR_SPAWNS.Add(index, new DummyData_DoorSpawn(index, leftSide, rightSide));
	}
}
