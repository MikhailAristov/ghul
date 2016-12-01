using UnityEngine;
using System;
using System.Collections.Generic;

// Game state object representing rooms in the house
[Serializable]
public class Data_Room : IComparable<Data_Room> {

    // Index of the room in the global registry
    [SerializeField]
    private int _INDEX;
    public int INDEX {
        get { return _INDEX; }
        private set { _INDEX = value;  }
    }

    // Unique string identifier of the container game object
    [SerializeField]
    private string _gameObjName;
    // Pointer to the container game object
    [NonSerialized]
    public GameObject gameObj;
    // Pointer to the environment behavior aspect of the container object
    [NonSerialized]
    public Environment_Room env;

    // Horizontal span of the room (generally equals its spite width)
    [SerializeField]
    private float _width;
    public float width
    {
        get { return _width; }
        private set { _width = value; }
    }

    // List of doors within the current room
    [SerializeField]
    private List<int> _doorIds;
    [NonSerialized]
    public List<Data_Door> DOORS;
    
    public Data_Room(int I, string gameObjectName)
    {
        INDEX = I;
        gameObj = GameObject.Find(gameObjectName);
        if(gameObj != null)
        {
            env = gameObj.GetComponent<Environment_Room>();
            Transform background = gameObj.transform.FindChild("Background");
            Renderer bgRenderer = background.GetComponent<Renderer>();
            width = bgRenderer.bounds.size[0];
        }
        else
        {
            throw new ArgumentException("Cannot find room: " + gameObjectName);
        }
        _doorIds = new List<int>();
        DOORS = new List<Data_Door>();
    }

    public int CompareTo(Data_Room other) { return INDEX.CompareTo(other.INDEX); }
    public override string ToString() { return INDEX.ToString(); }

    // Adds a door to this room at a specific position
    public void addDoor(Data_Door D, float xPos)
    {
        D.addToRoom(this, xPos);
        _doorIds.Add(D.INDEX);
        DOORS.Add(D);
    }

    // Resets game object references, e.g. after a saved state load
    public void fixObjectReferences(Data_GameState GS)
    {
        gameObj = GameObject.Find(_gameObjName);
        env = gameObj.GetComponent<Environment_Room>();
        DOORS = new List<Data_Door>();
        foreach(int id in _doorIds) {
            DOORS.Add(GS.getDoorByIndex(id));
        }
    }
}
