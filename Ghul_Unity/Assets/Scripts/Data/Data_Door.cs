using UnityEngine;
using System;

// Game state object representing doors (connections between rooms) in the house
[Serializable]
public class Data_Door : IComparable<Data_Door> {

	[NonSerialized]
	public Control_Door control;

	// Door types
	public const int TYPE_LEFT_SIDE = -1;
	public const int TYPE_BACK_DOOR = 0;
	public const int TYPE_RIGHT_SIDE = 1;

	// Door states
	public const int STATE_CLOSED = 0;
	public const int STATE_OPEN = 1;
	public const int STATE_HELD = 2;

	// Index of the room in the global registry
	[SerializeField]
	private int _INDEX;

	public int INDEX {
		get { return _INDEX; }
		private set { _INDEX = value; }
	}

	// Type of the door (left side, right side, back)
	[SerializeField]
	private int _type;

	public int type {
		get { return _type; }
	}

	// State of the door
	[NonSerialized]
	public int state;

	// Unique string identifier of the container game object
	[SerializeField]
	private string _gameObjName;
	// Pointer to the container game object
	[NonSerialized]
	public GameObject gameObj;

	// Position of the room within game space
	[SerializeField]
	private Data_Position _pos;

	public Data_Position pos {
		get { return _pos.clone(); } // Door positions shouldn't be manipulated, hence cloning
	}

	[NonSerialized]
	public Data_Room isIn;

	public float atPos {
		get { return pos.X; }
	}

	public float visiblePos {
		get { return Mathf.Clamp(atPos, isIn.leftWalkBoundary, isIn.rightWalkBoundary); }
	}

	// Connecting door ("other side")
	[SerializeField]
	private int _connectsToDoorIndex;
	[NonSerialized]
	public Data_Door connectsTo;

	public Data_Door(int I, string gameObjectName) {
		INDEX = I;
		_gameObjName = gameObjectName;
		gameObj = GameObject.Find(gameObjectName);
		Debug.AssertFormat(gameObj != null, "Cannot find door game object {0}!", gameObj.name);
		control = gameObj.GetComponent<Control_Door>();
		Debug.AssertFormat(control != null, "Cannot find door control object for {0}!", gameObj.name);
		control.loadGameState(this);
	}

	public Data_Door(int I, GameObject go, int doorType, Data_Room parentRoom, float xPos) {
		INDEX = I;
		_type = doorType;
		// Set the game object references
		_gameObjName = go.name;
		gameObj = go;
		control = go.GetComponent<Control_Door>();
		Debug.AssertFormat(control != null, "Cannot find door control object for {0}!", gameObj.name);
		control.loadGameState(this);
		// Set the room object references
		_pos = new Data_Position(parentRoom.INDEX, xPos);
		Debug.Assert(parentRoom != null);
		isIn = parentRoom;
		parentRoom.addDoor(this);
	}

	public int CompareTo(Data_Door other) {
		return INDEX.CompareTo(other.INDEX);
	}

	public override string ToString() {
		return INDEX.ToString();
	}

	// Connects the door to another door
	public void connectTo(Data_Door D) {
		if(D.connectsTo == null) { // The other door is not connected to any other yet
			_connectsToDoorIndex = D.INDEX;
			connectsTo = D;
			D.connectTo(this);
		} else if(D.connectsTo == this) { // The other door is connected to this one already
			_connectsToDoorIndex = D.INDEX;
			connectsTo = D;
		} else {
			throw new System.ArgumentException("Cannot connect door #" + this + " to #" + D + ": #" + D + " already connects to #" + D.connectsTo, "original");
		}
	}

	// Resets game object references, e.g. after a saved state load
	// MUST be called after its respective room had its references fixed!!
	public void fixObjectReferences(Data_GameState GS, Factory_PrefabController prefabFactory) {
		// Relocate or respawn the game object
		gameObj = GameObject.Find(_gameObjName);
		if(gameObj == null) {
			switch(type) {
			case TYPE_LEFT_SIDE:
				gameObj = prefabFactory.spawnLeftSideDoor(isIn.env.transform, isIn.width);
				break;
			case TYPE_BACK_DOOR:
				gameObj = prefabFactory.spawnBackDoor(isIn.env.transform, pos.X);
				break;
			case TYPE_RIGHT_SIDE:
				gameObj = prefabFactory.spawnRightSideDoor(isIn.env.transform, isIn.width);
				break;
			}
			_gameObjName = gameObj.name;
		}
		control = gameObj.GetComponent<Control_Door>();
		Debug.AssertFormat(control != null, "Cannot find door control object for {0}!", gameObj.name);
		control.loadGameState(this);
		// Connect to the other door
		connectsTo = GS.getDoorByIndex(_connectsToDoorIndex);
		// Room relations are set in the Data_Room.fixObjectReferences()
	}
		
}
