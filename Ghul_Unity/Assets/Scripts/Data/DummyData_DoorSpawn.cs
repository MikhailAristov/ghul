using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class DummyData_DoorSpawn {

	[SerializeField]
	private int _INDEX;
	public int INDEX {
		get { return _INDEX; }
		private set { _INDEX = value;  }
	}

	// true if the door is on the left end of the room
	[SerializeField]
	private bool _LEFT_SIDE;
	public bool LEFT_SIDE {
		get { return _LEFT_SIDE; }
		private set { _LEFT_SIDE = value;  }
	}

	// true if the door is on the right end of the room
	[SerializeField]
	private bool _RIGHT_SIDE;
	public bool RIGHT_SIDE {
		get { return _RIGHT_SIDE; }
		private set { _RIGHT_SIDE = value;  }
	}

	// index of connected door spawn. if there is no connection, the value is -1
	[SerializeField]
	private int _CONNECTS_TO_SPAWN_ID;
	public int CONNECTS_TO_SPAWN_ID {
		get { return _CONNECTS_TO_SPAWN_ID; }
		private set { _CONNECTS_TO_SPAWN_ID = value;  }
	}

	public DummyData_DoorSpawn(int I, bool LEFT, bool RIGHT) {
		INDEX = I;
		LEFT_SIDE = LEFT;
		RIGHT_SIDE = RIGHT;
		CONNECTS_TO_SPAWN_ID = -1;
	}

	// returns true if there is a connection to another door spawn.
	public bool isConnected() {
		return (CONNECTS_TO_SPAWN_ID >= 0);
	}

	public void connectTo(int otherID) {
		CONNECTS_TO_SPAWN_ID = otherID;
	}

	public void disconnect() {
		CONNECTS_TO_SPAWN_ID = -1;
	}
}
