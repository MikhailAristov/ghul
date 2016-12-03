using UnityEngine;
using System;

[Serializable]
public class Data_PlayerCharacter : Data_Character {

    // Pointer to the character behavior aspect of the container object
    [NonSerialized]
    public Control_PlayerCharacter control;

    // Gameplay parameters:
    [SerializeField]
    private float _stamina; // goes from 0.0 to 1.0
    public float stamina
    {
        get { return _stamina; }
        private set { _stamina = value; }
    }
    [SerializeField]
    private bool _exhausted;
    public bool exhausted
    {
        get { return _exhausted; }
        private set { _exhausted = value; }
    }
    // New gameplay parameters
    public bool controllable { get; set; }
    public Data_Position startingPos { get; set; }
    public float deathDuration { get; set; }
    public bool isDying { get; set; }
    public float remainingReactionTime { get; set; } //remaining time to escape killing radius

    // Constructor
    public Data_PlayerCharacter(string gameObjectName) : base(gameObjectName)
    {
        control = gameObj.GetComponent<Control_PlayerCharacter>();
        // Initialize gameplay parameters
        stamina = 1.0f;
        exhausted = false;
        controllable = true;
        isDying = false;
    }

    // Moves the player character back to the starting position
    public void resetPosition(Data_GameState GS)
    {
        updatePosition(GS.getRoomByIndex(startingPos.RoomId), startingPos.X);
    }

    // Updates the stamina meter with the specified amount (positive or negative), within boundaries
    // Sets the exhausted flag as necessary and returns the final value of the meter
    public float modifyStamina(float Delta)
    {
        float tempStamina = stamina + Delta;
        if (tempStamina >= 1.0f)
        {
            stamina = 1.0f;
            exhausted = false;
        }
        else if (tempStamina <= 0.0f)
        {
            stamina = 0.0f;
            exhausted = true;
        }
        else
        {
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
