using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Data_GraphDoorSpawn {

	[SerializeField]
	public int roomId;

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

	// Counts how often the door was used.
	[SerializeField]
	private int _NUM_USES;
	public int NUM_USES {
		get { return _NUM_USES; }
		private set { _NUM_USES = value; }
	}

	// Constructor
	public Data_GraphDoorSpawn(int I, bool LEFT, bool RIGHT) {
		INDEX = I;
		LEFT_SIDE = LEFT;
		RIGHT_SIDE = RIGHT;
		CONNECTS_TO_SPAWN_ID = -1;
		NUM_USES = 0;
	}
	// Constructor for cloning
	public Data_GraphDoorSpawn(Data_GraphDoorSpawn spawn) {
		INDEX = spawn.INDEX;
		LEFT_SIDE = spawn.LEFT_SIDE;
		RIGHT_SIDE = spawn.RIGHT_SIDE;
		CONNECTS_TO_SPAWN_ID = spawn.CONNECTS_TO_SPAWN_ID;
		NUM_USES = spawn.NUM_USES;
	}

	// returns true if there is a connection to another door spawn.
	public bool isConnected() {
		return (CONNECTS_TO_SPAWN_ID >= 0);
	}

	// connects the door spawn to the door spawn with ID otherID.
	public void connectTo(int otherID) {
		CONNECTS_TO_SPAWN_ID = otherID;
	}

	public void disconnect() {
		CONNECTS_TO_SPAWN_ID = -1;
	}

	// Call this when entering the door
	public void increaseNumUses() {
		NUM_USES++;
	}

	public void resetNumUses() {
		NUM_USES = 0;
	}
}
