using UnityEngine;
using System;

// Game state object representing doors (connections between rooms) in the house
[Serializable]
public class Data_Door : IComparable<Data_Door> {

	// Possible item states:
	public const int TYPE_LEFT_SIDE = -1;
	public const int TYPE_BACK_DOOR = 0;
	public const int TYPE_RIGHT_SIDE = 1;

    // Index of the room in the global registry
    [SerializeField]
    private int _INDEX;
    public int INDEX {
        get { return _INDEX; }
        private set { _INDEX = value; }
	}

	// Type of the door (left, right side, back)
	[SerializeField]
	private int _type;
	public int type {
		get { return _type; }
		private set { return; }
	}

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
        private set { return; }
    }
    [NonSerialized]
    public Data_Room isIn;
    public float atPos {
        get { return pos.X; }
        private set { return; }
    }

    // Connecting door ("other side")
    [SerializeField]
    private int _connectsToDoorIndex;
    [NonSerialized]
    public Data_Door connectsTo;

    public Data_Door(int I, string gameObjectName)
    {
        INDEX = I;
        _gameObjName = gameObjectName;
        gameObj = GameObject.Find(gameObjectName);
        if (gameObj == null) {
            throw new ArgumentException("Cannot find door: " + gameObjectName);
        }
    }

	public Data_Door(int I, GameObject go, int doorType, Data_Room parentRoom, float xPos) {
		INDEX = I;
		_type = doorType;
		// Set the game object references
		_gameObjName = go.name;
		gameObj = go;
		// Set the room object references
		_pos = new Data_Position(parentRoom.INDEX, xPos);
		isIn = parentRoom;
		parentRoom.addDoor(this);
	}

    public int CompareTo(Data_Door other) { return INDEX.CompareTo(other.INDEX); }
    public override string ToString() { return INDEX.ToString(); }

    // Adds this door to a specific room at a particular position, with backreference
    public void addToRoom(Data_Room R, float xPos)
    {
        if (isIn == null) // The door's location is not set yet
        {
            _pos = new Data_Position(R.INDEX, xPos);
            isIn = R;
        }
        else
        {
            throw new System.ArgumentException("Cannot add door #" + this + " to room #" + R + ": door is already in room #" + isIn, "original");
        }
    }

    // Connects the door to another door
    public void connectTo(Data_Door D)
    {
        if (D.connectsTo == null) // The other door is not connected to any other yet
        {
            _connectsToDoorIndex = D.INDEX;
            connectsTo = D;
            D.connectTo(this);
        }
        else if (D.connectsTo == this) // The other door is connected to this one already
        {
            _connectsToDoorIndex = D.INDEX;
            connectsTo = D;
        }
        else
        {
            throw new System.ArgumentException("Cannot connect door #" + this + " to #" + D + ": #" + D + " already connects to #" + D.connectsTo, "original");
        }
    }

    // Resets game object references, e.g. after a saved state load
    public void fixObjectReferences(Data_GameState GS)
    {
        gameObj = GameObject.Find(_gameObjName);
        // Connect to the other door
        connectsTo = GS.getDoorByIndex(_connectsToDoorIndex);
        // Room relations are set in the Data_Room.fixObjectReferences()
    }
}
