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

	// List of item spots within the current room
	[SerializeField]
	private List<int> _itemSpotIds;
	[NonSerialized]
	public List<Data_ItemSpot> ITEM_SPOTS;
    
    public Data_Room(int I, string gameObjectName)
    {
        INDEX = I;
        _gameObjName = gameObjectName;
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
		_itemSpotIds = new List<int>();
		ITEM_SPOTS = new List<Data_ItemSpot>();
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

	// Adds a door to this room at a specific position. NOTE: The y-value is a local coordinate
	public void addItemSpot(Data_ItemSpot iSpot, float xPos, float localYPos) {
		iSpot.addToRoom(this, xPos, localYPos);
		_itemSpotIds.Add(iSpot.INDEX);
		ITEM_SPOTS.Add(iSpot);
	}

    // Resets game object references, e.g. after a saved state load
    // NOTE: All doors must have their object references fixed before this function is called
    public void fixObjectReferences(Data_GameState GS)
    {
        gameObj = GameObject.Find(_gameObjName);
        env = gameObj.GetComponent<Environment_Room>();
        // Re-associate doors
        DOORS = new List<Data_Door>();
        foreach (int id in _doorIds)
        {
            Data_Door d = GS.getDoorByIndex(id);
            DOORS.Add(d);
            d.isIn = this;
        }
		// Re-associate item spots
		ITEM_SPOTS = new List<Data_ItemSpot>();
		foreach (int id in _itemSpotIds)
		{
			Data_ItemSpot iSpot = GS.getItemSpotByIndex(id);
			ITEM_SPOTS.Add(iSpot);
			iSpot.isIn = this;
		}
    }

	// Returns how many doors are located in this room
	public int getAmountOfDoors() {
		return DOORS.Count;
	}

	// Returns how many item spots are located in this room
	public int getAmountOfItemSpots() {
		return ITEM_SPOTS.Count;
	}
}
