using UnityEngine;
using System;

// This is a pseudo-abstract class for other character classes to inherit from
[Serializable]
public abstract class Data_Character {

    // Character name
    [SerializeField]
    protected string _name;
    public string name
    {
        get { return _name; }
        private set { _name = value; }
    }
    // Unique string identifier of the container game object
    [SerializeField]
    protected string _gameObjName;
    // Pointer to the container game object
    [NonSerialized]
	public GameObject gameObj;
	public abstract Control_Character getControl();

    // Position of the character within game space
    [SerializeField]
    protected Data_Position _pos;
    public Data_Position pos
    {
        get { return _pos; }
        private set { return; }
    }
    [NonSerialized]
    public Data_Room isIn;
    public float atPos
    {
        get { return pos.X; }
        private set { return; }
	}
	[NonSerialized]
	public float currentVelocity; // in m/s
	[NonSerialized]
	public float timeWithoutAction;

	// Position of the character within game space
	[SerializeField]
	protected Data_Position _spawnPos;

	// While this value is above zero, it marks the character as uncontrollable and invulnerable, e.g. upon entering a door or dying
	[SerializeField]
	public float etherialCooldown; // in seconds

	// Just some shortcut functions
	public bool isInvulnerable {
		get { return (etherialCooldown > 0); }
		private set { return; }
	}

    // Constructor
    public Data_Character(string gameObjectName)
    {
        _name = gameObjectName;
        _gameObjName = gameObjectName;
        gameObj = GameObject.Find(gameObjectName);
        if (gameObj == null)
        {
            throw new ArgumentException("Cannot find character object: " + gameObjectName);
        }
    }

    public override string ToString() { return name; }

    // Complete position specification
	public void updatePosition(Data_Room R, float xPos, float yPos)
    {
        if (_pos == null) { // Initial specification
			_pos = new Data_Position(R.INDEX, xPos, yPos);
			_spawnPos = _pos.clone();
        } else {
            _pos.RoomId = R.INDEX;
			_pos.X = xPos;
			_pos.Y = yPos;
        }
        isIn = R;
    }
    // Quicker updates
	public void updatePosition(Data_Room R, float xPos) {
		float yPos = (_pos == null) ? 0f : _pos.Y;
		updatePosition(R, xPos, yPos);
	}
    public void updatePosition(float Pos) {
        _pos.X = Pos;
    }

	// Reset the position back to spawn point
	public void resetPosition(Data_GameState GS) {
		updatePosition(GS.getRoomByIndex(_spawnPos.RoomId), _spawnPos.X, _spawnPos.Y);
	}
}
