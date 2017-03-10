using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Data_PlayerCharacter : Data_Character {

    // Pointer to the character behavior aspect of the container object
    [NonSerialized]
	public Control_PlayerCharacter control;
	public override Control_Character getControl() {
		return control as Control_Character; 
	}

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
	// Remaining time to escape the monster's
    public float remainingReactionTime { get; set; }

	// The item the character currently carries
	[NonSerialized]
	public Data_Item carriedItem;

	[SerializeField]
	private int _deaths;
	public int deaths {
		get { return _deaths; }
		set { _deaths = value; }
	}

    // Constructor
    public Data_PlayerCharacter(string gameObjectName) : base(gameObjectName)
    {
        control = gameObj.GetComponent<Control_PlayerCharacter>();
        // Initialize gameplay parameters
		etherialCooldown = 0.0f;
        stamina = 1.0f;
        exhausted = false;
		carriedItem = null;
		deaths = 0;
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
