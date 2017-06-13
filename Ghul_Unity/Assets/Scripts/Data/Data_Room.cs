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
		private set { _INDEX = value; }
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

	public float width {
		get { return _width; }
		private set { _width = value; }
	}

	[NonSerialized]
	private float walkMargin;

	public float leftWalkBoundary {
		get { return (walkMargin - _width / 2); }
	}

	public float rightWalkBoundary {
		get { return (_width / 2 - walkMargin); }
	}

	// List of doors within the current room
	[SerializeField]
	private int _leftSideDoorID;
	[SerializeField]
	private int _rightSideDoorID;
	[SerializeField]
	private List<int> _backDoorIDs;
	// The door list is autosorted by horizontal position
	[NonSerialized]
	public SortedList<float, Data_Door> DOORS;

	public Data_Door leftmostDoor {
		get { return DOORS.Values[0]; }
	}

	public Data_Door rightmostDoor {
		get { return DOORS.Values[DOORS.Count - 1]; }
	}
    
	// List of item spawn positions in the room
	[SerializeField]
	private List<Data_Position> _itemSpawnPoints;

	public bool hasItemSpawns {
		get { return (_itemSpawnPoints.Count > 0); }
	}

	// List of door spawn positions in the room
	[SerializeField]
	private List<float> _doorSpawnPoints;

	public bool hasLeftSideDoorSpawn {
		get { return (_doorSpawnPoints[0] < -_width); }
	}

	public bool hasRightSideDoorSpawn {
		get { return (_doorSpawnPoints[_doorSpawnPoints.Count - 1] > _width); }
	}

	public int countAllDoorSpawns {
		get { return _doorSpawnPoints.Count; }
	}

	public int countBackDoorSpawns {
		get { return (countAllDoorSpawns - (hasLeftSideDoorSpawn ? 1 : 0) - (hasRightSideDoorSpawn ? 1 : 0)); }
	}

	// Room properties for AI purposes
	public double effectiveWidth;
	public double meanExplorationDistance;
	public double meanItemFetchDistance;
	public double meanDoorToDoorDistance;

	public Data_Room(int I, GameObject go, Factory_PrefabRooms.RoomPrefab prefabDetails) {
		INDEX = I;
		// Set the game object references
		_gameObjName = go.name;
		gameObj = go;
		env = go.GetComponent<Environment_Room>();
		// Get room details from the prefab
		_width = Data_Position.snapToGrid(prefabDetails.size.x);
		_itemSpawnPoints = new List<Data_Position>();
		foreach(Vector2 p in prefabDetails.itemSpawns) {
			_itemSpawnPoints.Add(new Data_Position(I, p, align: true));
		}
		walkMargin = Global_Settings.read("HORIZONTAL_ROOM_MARGIN") + Data_Position.PIXEL_GRID_SIZE;
		effectiveWidth = _width - 2 * walkMargin;
		meanExplorationDistance = 1.5 * (_width - Global_Settings.read("SCREEN_SIZE_HORIZONTAL"));
		meanItemFetchDistance = (_itemSpawnPoints.Count > 0) ? (effectiveWidth / 3) : 0;
		// Load door spawnpoints
		_doorSpawnPoints = new List<float>();
		if(prefabDetails.doorSpawnLeft) {
			_doorSpawnPoints.Add(float.MinValue);
		}
		foreach(float doorSpawn in prefabDetails.doorSpawns) {
			_doorSpawnPoints.Add(Data_Position.snapToGrid(doorSpawn));
		}
		if(prefabDetails.doorSpawnRight) {
			_doorSpawnPoints.Add(float.MaxValue);
		}
		_doorSpawnPoints.Sort();
		// Doors are added separately
		removeAllDoors();
	}

	public int CompareTo(Data_Room other) {
		return INDEX.CompareTo(other.INDEX);
	}

	public override string ToString() {
		return INDEX.ToString();
	}

	// Initializes the room with no doors
	public void removeAllDoors() {
		_leftSideDoorID = -1;
		_rightSideDoorID = -1;
		_backDoorIDs = new List<int>();
		DOORS = new SortedList<float, Data_Door>();
	}

	// Returns a door spawn position of the specified index
	public float getDoorSpawnPosition(int spawnIndex) {
		if(spawnIndex < _doorSpawnPoints.Count) {
			return _doorSpawnPoints[spawnIndex];
		} else {
			throw new IndexOutOfRangeException("No door spawn exists in room #" + this + " with index #" + spawnIndex);
		}
	}

	// Returns the door at the specified spawn position, if any
	public Data_Door getDoorAtSpawn(int spawnIndex) {
		float horizontalRoomMargin = Global_Settings.read("HORIZONTAL_ROOM_MARGIN");
		float marginOfError = Global_Settings.read("HORIZONTAL_DOOR_WIDTH") / 2;
		float xPos = getDoorSpawnPosition(spawnIndex);
		// Loop through the doors
		foreach(Data_Door door in DOORS.Values) {
			if((door.type == Data_Door.TYPE_LEFT_SIDE	&& xPos <= (horizontalRoomMargin - this._width / 2)) ||
			   (door.type == Data_Door.TYPE_BACK_DOOR	&& Math.Abs(xPos - door.atPos) < marginOfError) ||
			   (door.type == Data_Door.TYPE_RIGHT_SIDE	&& xPos >= (this._width / 2 - horizontalRoomMargin))) {
				return door;
			}
		}
		return null;
	}

	// Adds a door to this room
	public void addDoor(Data_Door D) {
		switch(D.type) {
		case Data_Door.TYPE_LEFT_SIDE:
			_leftSideDoorID = D.INDEX;
			break;
		case Data_Door.TYPE_BACK_DOOR:
			_backDoorIDs.Add(D.INDEX);
			break;
		case Data_Door.TYPE_RIGHT_SIDE:
			_rightSideDoorID = D.INDEX;
			break;
		}
		DOORS.Add(D.atPos, D);
		// Update statistic after every door added
		meanDoorToDoorDistance = (DOORS.Count > 1) ? effectiveWidth / (DOORS.Count - 1) + Global_Settings.read("DOOR_TRANSITION_DURATION") * Global_Settings.read("CHARA_WALKING_SPEED") : 0;
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
	public void fixObjectReferences(Data_GameState GS, Factory_PrefabController prefabFactory) {
		// Relocate or respawn the game object
		gameObj = GameObject.Find(_gameObjName);
		if(gameObj == null) {
			gameObj = prefabFactory.spawnRoomFromName(_gameObjName, Global_Settings.read("VERTICAL_ROOM_SPACING"));
			_gameObjName = gameObj.name;
		}
		env = gameObj.GetComponent<Environment_Room>();
		walkMargin = Global_Settings.read("HORIZONTAL_ROOM_MARGIN");
		// Re-associate doors
		DOORS = new SortedList<float, Data_Door>();
		if(_leftSideDoorID >= 0) {
			Data_Door d = GS.getDoorByIndex(_leftSideDoorID);
			DOORS.Add(d.atPos, d);
			d.isIn = this;
		}
		foreach(int id in _backDoorIDs) {
			Data_Door d = GS.getDoorByIndex(id);
			DOORS.Add(d.atPos, d);
			d.isIn = this;
		}
		if(_rightSideDoorID >= 0) {
			Data_Door d = GS.getDoorByIndex(_rightSideDoorID);
			DOORS.Add(d.atPos, d);
			d.isIn = this;
		}
	}
}
