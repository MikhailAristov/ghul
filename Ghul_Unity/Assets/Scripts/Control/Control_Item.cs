using UnityEngine;
using System;

public class Control_Item : MonoBehaviour {

	[NonSerialized]
	private Data_GameState GS;
	[NonSerialized]
	private Environment_Room currentEnvironment;
	[NonSerialized]
	private Data_Item me;

	private float ITEM_CARRY_ELEVATION = 0.0f;

	// Use this for initialization
	void Start() {	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState, int ownIndex)
	{
		this.GS = gameState;
		this.me = gameState.getItemByIndex(ownIndex);
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
			me.updatePosition(GS.getCHARA().isIn, GS.getCHARA().pos.X, ITEM_CARRY_ELEVATION);
			break;
		case Data_Item.STATE_DROPPED:
			// Let the item fall unless it's already on the floor if it is just dropped
			break;
		}
	}

	// When CHARA dies again without retrieving the item
	public void resetToSpawnPosition() {
		if(me.state == Data_Item.STATE_ON_CADAVER || me.state == Data_Item.STATE_INITIAL) {
			// Reset the state
			me.state = Data_Item.STATE_INITIAL;
			// Find the original spawn point and reset the position
			Data_ItemSpawn target = GS.getItemSpawnPointByIndex(me.itemSpotIndex);
			Debug.Log("setting location to " + GS.getRoomByIndex(target.RoomId) + " " + target.X + "/"+ target.Y);
			me.updatePosition(GS.getRoomByIndex(target.RoomId), target.X, target.Y);
			// Set the sprite's parent to the containing room and move the sprite there
			transform.parent = GS.getRoomByIndex(me.pos.RoomId).env.transform; // Move the game object to the room game object
			Debug.Log("setting local location to " + me.atPos + "/" + me.elevation + "/"+ transform.position.z);
			transform.position = new Vector3(me.atPos, me.elevation, transform.position.z);
			// Show the object
			GetComponent<Renderer>().enabled = true;
		} else {
			Debug.LogError("Cannot reset " + me);
		}
	}

	// When CHARA picks it up
	public void moveToInventory() {
		if(me.state == Data_Item.STATE_INITIAL || me.state == Data_Item.STATE_ON_CADAVER || me.state == Data_Item.STATE_DROPPED) { 
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
			me.updatePosition(GS.getCadaver().isIn, GS.getCadaver().pos.X, ITEM_CARRY_ELEVATION);
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
