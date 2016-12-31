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

    // TODO: Horizontal span of the room (generally equals its spite width)
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
    
	// TODO
	[SerializeField]
	private List<Data_Position> _itemSpawnPoints;
	public bool hasItemSpawns {
		get { return (_itemSpawnPoints.Count > 0); }
		private set { return; }
	}
	[SerializeField]
	private int _leftSideDoorID;
	[SerializeField]
	private int _rightSideDoorID;
	[SerializeField]
	private List<int> _backDoorIDs;

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
		_itemSpawnPoints = new List<Data_Position>();
    }

	// TODO
	public Data_Room(int I, GameObject go, Factory_PrefabRooms.RoomPrefab prefabDetails) {
		INDEX = I;
		// Set the game object references
		_gameObjName = go.name;
		gameObj = go;
		env = go.GetComponent<Environment_Room>();
		// Get room details from the prefab
		_width = prefabDetails.size.x;
		_itemSpawnPoints = new List<Data_Position>();
		foreach(Vector2 p in prefabDetails.itemSpawns) {
			_itemSpawnPoints.Add(new Data_Position(I, p));
		}
		// Doors are added separately
		_leftSideDoorID = -1;
		_rightSideDoorID = -1;
		_backDoorIDs = new List<int>();
		DOORS = new List<Data_Door>();
	} 

	// TODO remove this
	public void manualAddItemSpawn(float xPos, float yPos) {
		_itemSpawnPoints.Add(new Data_Position(INDEX, xPos, yPos));
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

	// Adds a door to this room
	public void addDoor(Data_Door D)
	{
		switch(D.type) {
		case Data_Door.TYPE_BACK_DOOR:
			_backDoorIDs.Add(D.INDEX);
			break;
		case Data_Door.TYPE_LEFT_SIDE:
			_leftSideDoorID = D.INDEX;
			break;
		case Data_Door.TYPE_RIGHT_SIDE:
			_rightSideDoorID = D.INDEX;
			break;
		}
		DOORS.Add(D);
	}

	// Returns a random item spot, if the room has any (otherwise null)
	public Data_Position getRandomItemSpawnPoint() {
		if(_itemSpawnPoints.Count == 0) {
			return null;
		} else {
			int i = UnityEngine.Random.Range(0, _itemSpawnPoints.Count);
			return _itemSpawnPoints[i].clone();
		}
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
    }

	// Returns how many doors are located in this room
	public int getNumberOfDoors() {
		return DOORS.Count;
	}
}
