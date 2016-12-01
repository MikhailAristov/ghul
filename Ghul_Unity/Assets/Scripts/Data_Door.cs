using UnityEngine;
using System;

// Game state object representing doors (connections between rooms) in the house
[Serializable]
public class Data_Door : IComparable<Data_Door> {

    // Index of the room in the global registry
    [SerializeField]
    private int _INDEX;
    public int INDEX
    {
        get { return _INDEX; }
        private set { _INDEX = value; }
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
    public Data_Position pos
    {
        get { return _pos.clone(); } // Door positions shouldn't be manipulated, hence cloning
        private set { return; }
    }
    [NonSerialized]
    public Data_Room isIn;
    public float atPos
    {
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
        gameObj = GameObject.Find(gameObjectName);
        if (gameObj == null) {
            throw new ArgumentException("Cannot find room: " + gameObjectName);
        }
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
        isIn = GS.getRoomByIndex(pos.RoomId);
        connectsTo = GS.getDoorByIndex(_connectsToDoorIndex);
    }
}
