using UnityEngine;
using System;

[Serializable]
public class Data_Monster : Data_Character {

    // Pointer to the character behavior aspect of the container object
    [NonSerialized]
	public Control_Monster control;
	[SerializeField]
	public AI_WorldModel worldModel;

	// AI parameters
	[SerializeField]
	public int state;
	[SerializeField]
	public float AGGRO; // = (number of items collected so far) / 10 + (minutes elapsed since last kill) (double that if Toni carries an item)
	[SerializeField]
	public float timeSinceLastKill;

    // Gameplay parameters:
    public bool playerInSight;
	public bool playerDetected; // defines whether the monster knows where to go
    public float playerPosLastSeen;
    public bool isRandomTargetSet; // define whether the monster made up its mind where to go
    public float randomTargetPos;
    public bool isThinking;
    public float remainingThinkingTime;

	// While this value is above zero, it marks the character as uncontrollable and invulnerable, e.g. upon entering a door or dying
	[SerializeField]
	public float etherialCooldown; // in seconds
    
    public Data_Monster(string gameObjectName) : base(gameObjectName)
    {
        control = gameObj.GetComponent<Control_Monster>();
        // Initialize gameplay parameters
        playerInSight = false;
        playerDetected = false;
        playerPosLastSeen = 0.0f;
        isRandomTargetSet = false;
        randomTargetPos = 0.0f;
        isThinking = false;
        remainingThinkingTime = 0.0f;
		// AI parameters
		state = Control_Monster.STATE_SEARCHING;
    }

	public void resetWorldModel(Data_GameState GS) {
		worldModel = new AI_WorldModel(GS);
	}

    // Resets game object references, e.g. after a saved state load
    public void fixObjectReferences(Data_GameState GS)
    {
        gameObj = GameObject.Find(_gameObjName);
        control = gameObj.GetComponent<Control_Monster>();
        isIn = GS.getRoomByIndex(_pos.RoomId);
    }
}
