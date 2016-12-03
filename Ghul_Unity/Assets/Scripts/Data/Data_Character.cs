using UnityEngine;
using System;

// This is a pseudo-abstract class for other character classes to inherit from
// It should not be instantiated directly
[Serializable]
public class Data_Character {

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
}
