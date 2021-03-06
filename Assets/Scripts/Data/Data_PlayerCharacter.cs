﻿using UnityEngine;
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
	private float _stamina;
	public float stamina {
		get { return _stamina; }
		private set { _stamina = value; }
	}

	[SerializeField]
	private bool _exhausted;

	public bool exhausted {
		get { return _exhausted; }
		private set { _exhausted = value; }
	}

	// The item the character currently carries
	[NonSerialized]
	public Data_Item carriedItem;

	[SerializeField]
	private int _deaths;

	public int deaths {
		get { return _deaths; }
		set { _deaths = value; }
	}

	// These parameters are for learning the player's individual movement pattern
	[SerializeField]
	public long cntStandingSinceLastDeath;
	[SerializeField]
	public long cntWalkingSinceLastDeath;
	[SerializeField]
	public long cntRunningSinceLastDeath;
	[SerializeField]
	public List<AI_RoomHistory> roomHistory;

	// Constructor
	public Data_PlayerCharacter(string gameObjectName) : base(gameObjectName) {
		control = gameObj.GetComponent<Control_PlayerCharacter>();
		// Initialize gameplay parameters
		stamina = 1f;
		resetMovementCounters();
	}

	// Updates the stamina meter with the specified amount (positive or negative), within boundaries
	// Sets the exhausted flag as necessary and returns the final value of the meter
	public float modifyStamina(float Delta) {
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

	// Convenience functions
	public void resetMovementCounters() {
		cntStandingSinceLastDeath = 0;
		cntWalkingSinceLastDeath = 0;
		cntRunningSinceLastDeath = 0;
	}

	public void resetRoomHistory() {
		if(roomHistory == null) {
			roomHistory = new List<AI_RoomHistory>();
		} else {
			roomHistory.Clear();
		}
	}

	public void increaseWalkedDistance(float dist) {
		// If the history is still empty after a reset, initialize it with the current room
		// If the last room in the history is not the same room as the current one, add the current one to history
		if(roomHistory.Count < 1 || roomHistory[roomHistory.Count - 1].roomId != isIn.INDEX) {
			roomHistory.Add(new AI_RoomHistory(isIn));
		}
		roomHistory[roomHistory.Count - 1].increaseWalkedDistance(dist);
	}

	// Resets game object references, e.g. after a saved state load
	public void fixObjectReferences(Data_GameState GS) {
		gameObj = GameObject.Find(_gameObjName);
		control = gameObj.GetComponent<Control_PlayerCharacter>();
		isIn = GS.getRoomByIndex(_pos.RoomId);
		// Check for carried item
		Data_Item curItem = GS.getCurrentItem();
		if(curItem.state == Data_Item.STATE_CARRIED) {
			carriedItem = curItem;
		}
	}
}
