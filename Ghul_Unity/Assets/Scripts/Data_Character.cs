using UnityEngine;
using System;

[Serializable]
public class Data_Character {

    // Character name
    [SerializeField]
    private string _name;
    public string name
    {
        get { return _name; }
        private set { _name = value; }
    }

    // Unique string identifier of the container game object
    [SerializeField]
    private string _gameObjName;
    // Pointer to the container game object
    [NonSerialized]
    public GameObject gameObj;
    // Pointer to the environment behavior aspect of the container object
    [NonSerialized]
    public Control_PlayerCharacter control;

    // Position of the room within game space
    [SerializeField]
    private Data_Position _pos;
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

    // Gameplay parameters:
    private float _stamina; // goes from 0.0 to 1.0
    public float stamina
    {
        get { return _stamina; }
        private set { _stamina = value; }
    }
    private bool _exhausted;
    public bool exhausted
    {
        get { return _exhausted; }
        private set { _exhausted = value; }
    }

    // Constructor
    public Data_Character(string gameObjectName)
    {
        name = gameObjectName;
        _gameObjName = gameObjectName;
        gameObj = GameObject.Find(gameObjectName);
        if (gameObj == null) {
            throw new ArgumentException("Cannot find character object: " + gameObjectName);
        }
        control = gameObj.GetComponent<Control_PlayerCharacter>();
        // Initialize gameplay parameters
        stamina = 1.0f;
        exhausted = false;
    }

    public override string ToString() { return name; }

    // Complete position specification
    public void updatePosition(Data_Room R, float xPos)
    {
        if (_pos == null) { // Initial specification
            _pos = new Data_Position(R.INDEX, xPos);
        } else {
            _pos.RoomId = R.INDEX;
            _pos.X = xPos;
        }
        isIn = R;
    }
    // Quicker update of horizontal position
    public void updatePosition(float Pos) {
        _pos.X = Pos;
    }

    // Updates the stamina meter with the specified amount (positive or negative), within boundaries
    // Sets the exhausted flag as necessary and returns the final value of the meter
    public float modifyStamina(float Delta)
    {
        float tempStamina = stamina + Delta;
        if(tempStamina >= 1.0f) {
            stamina = 1.0f;
            exhausted = false;
        } else if(tempStamina <= 0.0f) {
            stamina = 0.0f;
            exhausted = true;
        } else {
            stamina = tempStamina;
        }
        return stamina;
    }

    // Resets game object references, e.g. after a saved state load
    public void fixObjectReferences(Data_GameState GS)
    {
        gameObj = GameObject.Find(_gameObjName);
        control = gameObj.GetComponent<Control_PlayerCharacter>();
        isIn = GS.getRoomByIndex(_pos.RoomId);
    }
}
