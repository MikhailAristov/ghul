using UnityEngine;
using System;

[Serializable]
public class Data_Monster : Data_Character {

	// Pointer to the character behavior aspect of the container object
	[NonSerialized]
	public Control_Monster control;

	public override Control_Character getControl() {
		return control as Control_Character; 
	}

	[NonSerialized]
	public Control_MonsterPerception perception;
	[SerializeField]
	public AI_WorldModel worldModel;

	// AI parameters
	[SerializeField]
	public int state;
	[SerializeField]
	public float AGGRO;	// = (number of items collected so far) / 10 + (minutes elapsed since last kill) (double that if Toni carries an item)
	[SerializeField]
	public float timeSinceLastKill;

	public Data_Monster(string gameObjectName) : base(gameObjectName) {
		control = gameObj.GetComponent<Control_Monster>();
		// AI parameters
		state = Control_Monster.STATE_SEARCHING;
	}

	public void resetWorldModel(Data_GameState GS) {
		if(worldModel == null) {
			worldModel = new AI_WorldModel(GS);
		} else {
			worldModel.reset(GS);
		}
	}

	// Resets game object references, e.g. after a saved state load
	public void fixObjectReferences(Data_GameState GS) {
		gameObj = GameObject.Find(_gameObjName);
		control = gameObj.GetComponent<Control_Monster>();
		isIn = GS.getRoomByIndex(_pos.RoomId);
	}
}
