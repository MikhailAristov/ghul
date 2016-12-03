using UnityEngine;
using System;

[Serializable]
public class Data_Monster : Data_Character {

    // Pointer to the character behavior aspect of the container object
    [NonSerialized]
    public Control_Monster control;

    // Gameplay parameters:
    public bool playerInSight;
	public bool playerDetected; // defines whether the monster knows where to go
    public float playerPosLastSeen;
    public bool isRandomTargetSet; // define whether the monster made up its mind where to go
    public float randomTargetPos;
    public bool isThinking;
    public float remainingThinkingTime;
    
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
    }

    // Resets game object references, e.g. after a saved state load
    public void fixObjectReferences(Data_GameState GS)
    {
        gameObj = GameObject.Find(_gameObjName);
        control = gameObj.GetComponent<Control_Monster>();
        isIn = GS.getRoomByIndex(_pos.RoomId);
    }
}
