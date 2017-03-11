using UnityEngine;
using System;
using System.Collections;

public class Control_Item : MonoBehaviour {

	[NonSerialized]
	private Data_GameState GS;
	[NonSerialized]
	private Data_Item me;

	private float ITEM_CARRY_ELEVATION;
	private float ITEM_FLOOR_LEVEL;
	private Vector2 ITEM_POSITION_FOR_RITUAL;

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState, int ownIndex)
	{
		this.GS = gameState;
		this.me = gameState.getItemByIndex(ownIndex);

		ITEM_CARRY_ELEVATION = Global_Settings.read("ITEM_CARRY_ELEVATION");
		ITEM_FLOOR_LEVEL = Global_Settings.read("ITEM_FLOOR_LEVEL");

		// Get the ritual details
		float pentagramCenter = Global_Settings.read("RITUAL_PENTAGRAM_CENTER");
		float pentagramRadius = Global_Settings.read("RITUAL_PENTAGRAM_RADIUS");
		float maxItems = Global_Settings.read("RITUAL_ITEMS_REQUIRED");
		// Calculate the intented position of the item at the ritual 
		float ritualPos = (pentagramCenter - pentagramRadius) + (2 * ownIndex + 1) * pentagramRadius / maxItems;
		ITEM_POSITION_FOR_RITUAL = new Vector2(ritualPos, ITEM_FLOOR_LEVEL);

		// Check the item visibility
		GetComponent<Renderer>().enabled = me.isVisible();
	}
	
	// Update is called once per frame
	void Update() {
		if (GS == null || GS.SUSPENDED) { return; } // Don't do anything if the game state is not loaded yet or suspended

		// Set the position of the item to chara's position as long as it is carried
		if(me.state == Data_Item.STATE_CARRIED) {
			Data_PlayerCharacter chara = GS.getToni();
			me.updatePosition(chara.isIn, chara.pos.X, ITEM_CARRY_ELEVATION);
			updateGameObjectPosition();
		}
	}

	// Drops a free-falling object on the floor
	private IEnumerator fallOntoTheFloor() {
		while(me.elevation > ITEM_FLOOR_LEVEL && gameObject != null) {
			float newElevation = me.elevation - Time.deltaTime * getDownwardVelocity(ITEM_CARRY_ELEVATION - me.elevation);
			me.updatePosition(me.isIn, me.pos.X, Math.Max(ITEM_FLOOR_LEVEL, newElevation));
			updateGameObjectPosition();
			yield return null;
		}
	}

	// Calculates the downward velocity of the falling object from the distance it had already fallen
	private float getDownwardVelocity(float fallenDistance) {
		float g = 1.5f * 9.81f; // Because fantasy physics
		float v = (float)Math.Sqrt(2 * fallenDistance * g);
		return (v > 0 ? v : g); // Differential equations are a bitch...
	}

	// When placed, lerps the item to its designated position on the pentagram
	private IEnumerator floatToRitualPosition() {
		while(Vector2.Distance(transform.position, ITEM_POSITION_FOR_RITUAL) > 0.1f) {
			Vector2 delta = Time.deltaTime * ((Vector2)transform.position -  Vector2.Lerp(transform.position, ITEM_POSITION_FOR_RITUAL, 1.0f));
			me.updatePosition(me.isIn, me.atPos - delta.x, me.elevation - delta.y);
			updateGameObjectPosition();
			yield return null;
		}
		GS.numItemsPlaced++;
		GS.NEXT_ITEM_PLEASE = true;
	}

	// Update the game object/sprite's position within the game space from the current game state
	public void updateGameObjectPosition() {
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
			// Reset the position back to spawn point in game state and game space
			me.resetPosition(GS);
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
			StopCoroutine("fallOntoTheFloor");
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
			StartCoroutine("fallOntoTheFloor");
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
			StartCoroutine(floatToRitualPosition());
		} else {
			Debug.LogError("Cannot put down " + me);
		}
	}
}
