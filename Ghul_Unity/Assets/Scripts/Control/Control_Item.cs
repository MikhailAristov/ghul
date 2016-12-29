using UnityEngine;
using System;

public class Control_Item : MonoBehaviour {

	[NonSerialized]
	private Data_GameState GS;
	[NonSerialized]
	private Environment_Room currentEnvironment;
	[NonSerialized]
	private Data_Item me;

	private float ITEM_CARRY_ELEVATION;
	private float ITEM_FLOOR_LEVEL;

	// Use this for initialization
	void Start() {	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState, int ownIndex)
	{
		this.GS = gameState;
		this.me = gameState.getItemByIndex(ownIndex);

		ITEM_CARRY_ELEVATION = GS.getSetting("ITEM_CARRY_ELEVATION");
		ITEM_FLOOR_LEVEL = GS.getSetting("ITEM_FLOOR_LEVEL");
	}
	
	// Update is called once per frame
	void Update() {
		if (GS == null || GS.SUSPENDED) { return; } // Don't do anything if the game state is not loaded yet or suspended

		switch(me.state) {
		case Data_Item.STATE_INITIAL:
		case Data_Item.STATE_ON_CADAVER:
		case Data_Item.STATE_PLACED:
			return; // Don't do anything as long as the item is in its initial position or in the ritual room or on the corpse
		case Data_Item.STATE_CARRIED:
			// Set the position of the item to the position of the card
			Data_PlayerCharacter chara = GS.getCHARA();
			me.updatePosition(chara.isIn, chara.pos.X, ITEM_CARRY_ELEVATION);
			updateGameObjectPosition();
			break;
		case Data_Item.STATE_DROPPED:
			if(me.elevation > ITEM_FLOOR_LEVEL) { // Let the item fall to the ground
				float newElevation = me.elevation - Time.deltaTime * getDownwardVelocity(ITEM_CARRY_ELEVATION - me.elevation);
				me.updatePosition(me.isIn, me.pos.X, Math.Max(ITEM_FLOOR_LEVEL, newElevation));
				updateGameObjectPosition();
			}
			// Let the item fall unless it's already on the floor if it is just dropped
			break;
		}
	}

	// Calculates the downward velocity of the falling object from the distance it had already fallen
	private float getDownwardVelocity(float fallenDistance) {
		float g = 1.5f * 9.81f; // Because fantasy physics
		float v = (float)Math.Sqrt(2 * fallenDistance * g);
		return (v > 0 ? v : g); // Differential equations are a bitch...
	}

	// Update the game object/sprite's position within the game space from the current game state
	private void updateGameObjectPosition() {
		Vector3 targetPos = new Vector3(me.atPos, me.elevation, transform.position.z);
		if(transform.parent != me.isIn.env.transform) {
			transform.parent = me.isIn.env.transform; // Move the game object to the room game object
			transform.localPosition = targetPos;
		} else if(Vector3.Distance(transform.position, targetPos) > 0.01f) {
			transform.localPosition = targetPos;
		}
	}

	// When CHARA dies again without retrieving the item
	public void resetToSpawnPosition() {
		if(me.state == Data_Item.STATE_ON_CADAVER || me.state == Data_Item.STATE_INITIAL) {
			// Reset the state
			me.state = Data_Item.STATE_INITIAL;
			// Find the original spawn point and reset the position
			Data_ItemSpawn target = GS.getItemSpawnPointByIndex(me.itemSpotIndex);
			Data_Room spawnRoom = GS.getRoomByIndex(target.RoomId);
			me.updatePosition(spawnRoom, target.X, target.Y);
			updateGameObjectPosition();
			// Show the object
			GetComponent<Renderer>().enabled = true;
		} else {
			Debug.LogError("Cannot reset " + me);
		}
	}

	// When CHARA picks it up
	public void moveToInventory() {
		if(me.isTakeable()) { 
			me.state = Data_Item.STATE_CARRIED;
			GetComponent<Renderer>().enabled = false;
		} else {
			Debug.LogError("Cannot pick up " + me);
		}
	}

	// When CHARA drops it
	public void dropFromInventory() {
		if(me.state == Data_Item.STATE_CARRIED) { 
			me.state = Data_Item.STATE_DROPPED;
			GetComponent<Renderer>().enabled = true;
		} else {
			Debug.LogError("Cannot drop " + me);
		}
	}

	// When CHARA dies
	public void moveToCadaver() {
		if(me.state == Data_Item.STATE_CARRIED) { 
			me.state = Data_Item.STATE_ON_CADAVER;
			me.updatePosition(me.isIn, me.pos.X, ITEM_FLOOR_LEVEL); // The item remains where it was, just on the floor level
			updateGameObjectPosition();
			GetComponent<Renderer>().enabled = false;
		} else {
			Debug.LogError("Cannot transfer " + me + " to cadaver");
		}
	}

	// When CHARA gets to the ritual room
	public void placeForRitual() {
		if(me.state == Data_Item.STATE_CARRIED) { 
			me.state = Data_Item.STATE_PLACED;
			GetComponent<Renderer>().enabled = true;
		} else {
			Debug.LogError("Cannot put down " + me);
		}
	}
}
