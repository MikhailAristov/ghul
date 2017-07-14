using UnityEngine;
using System;
using System.Collections;

public class Control_Item : MonoBehaviour {

	// Because fantasy physics
	public const float GRAVITATIONAL_CONSTANT = 0.5f * 9.81f;

	[NonSerialized]
	private Data_GameState GS;
	[NonSerialized]
	private Data_Item me;

	private SpriteRenderer spriteRenderer;
	public Sprite BloodyScribble;

	private float ITEM_CARRY_ELEVATION;
	private float ITEM_FLOOR_LEVEL;
	private bool atRest;

	void Awake() {
		ITEM_CARRY_ELEVATION = Global_Settings.read("ITEM_CARRY_ELEVATION");
		ITEM_FLOOR_LEVEL = Global_Settings.read("ITEM_FLOOR_LEVEL");

		spriteRenderer = GetComponent<SpriteRenderer>();
		atRest = true;
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState, int ownIndex) {
		this.GS = gameState;
		this.me = gameState.getItemByIndex(ownIndex);
		Debug.Assert(me != null);

		// Check the item visibility
		spriteRenderer.enabled = me.isVisible();
	}
	
	// Update is called once per frame
	void Update() {
		if(GS == null || GS.SUSPENDED) {
			return;
		} // Don't do anything if the game state is not loaded yet or suspended

		// Debug for the missing reference exception
		if(me == null) {
			Debug.LogError(name + " is missing the data object reference!");
		} else if(me.control == null) {
			Debug.LogError(name + "'s data object is missing the control back-reference!");
		} else if(me.control != this) {
			Debug.LogError(name + "'s data object control back-reference is inconsistent!");
		}

		// Set the position of the item to chara's position as long as it is carried
		switch(me.state) {
		case Data_Item.STATE_CARRIED:
			Data_PlayerCharacter chara = GS.getToni();
			me.updatePosition(chara.isIn, chara.pos.X, ITEM_CARRY_ELEVATION);
			updateGameObjectPosition();
			break;
		case Data_Item.STATE_DROPPED:
			if(me.elevation > ITEM_FLOOR_LEVEL) {
				atRest = false;
				float newElevation = me.elevation - Time.deltaTime * getDownwardVelocity(ITEM_CARRY_ELEVATION - me.elevation);
				me.updatePosition(me.isIn, me.pos.X, Math.Max(ITEM_FLOOR_LEVEL, newElevation));
				updateGameObjectPosition();
			} else if(!atRest) {
				me.pos.snapToGrid();
				updateGameObjectPosition();
				atRest = true;
			}
			break;
		default:
			break;
		}
	}

	// Calculates the downward velocity of the falling object from the distance it had already fallen
	private float getDownwardVelocity(float fallenDistance) {
		float v = (float)Math.Sqrt(2 * fallenDistance * GRAVITATIONAL_CONSTANT);
		return (v > 0 ? v : GRAVITATIONAL_CONSTANT); // Differential equations are a bitch...
	}

	// When placed, fades in the item at its designated position on the pentagram
	private IEnumerator materializeAtRitualPosition(Vector3 targetPosition, float duration) {
		SpriteRenderer rend = GetComponent<SpriteRenderer>();
		// Move the sprite to target position
		me.updatePosition(me.isIn, targetPosition.x, targetPosition.y);
		me.pos.snapToGrid();
		updateGameObjectPosition();
		transform.position = targetPosition;
		// Set alpha channel to zero
		rend.color -= new Color(0, 0, 0, rend.color.a);
		// Enable the sprite
		rend.enabled = true;
		// Slowly restore the alpha channel over time
		for(float timeLeft = duration; timeLeft > 0; timeLeft -= Time.deltaTime) {
			rend.color += new Color(0, 0, 0, Time.deltaTime / duration);
			yield return null;
		}
		// Update the game state
		GS.nextChapter();
	}

	// Update the game object/sprite's position within the game space from the current game state
	public void updateGameObjectPosition() {
		// The first two items are placed on the pentagram "in front" of the player character
		float zPos = transform.position.z;
		if(me.state == Data_Item.STATE_DROPPED | (me.elevation - spriteRenderer.bounds.size.y / 2) <= ITEM_FLOOR_LEVEL) {
			zPos = -2f;
			GetComponent<SpriteRenderer>().sortingLayerName = "Foreground";
		} else {
			zPos = 0;
			GetComponent<SpriteRenderer>().sortingLayerName = "Items";
		}
		Vector3 targetPos = new Vector3(me.atPos, me.elevation, zPos);
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
			spriteRenderer.enabled = true;
		} else {
			Debug.LogError("Cannot reset " + me);
		}
	}

	// When CHARA picks it up
	public void moveToInventory() {
		Debug.AssertFormat(me != null, "{0} lost the object reference!", name);
		Debug.AssertFormat(spriteRenderer != null, "{0} lost the sprite renderer reference!", name);
		if(me == null) {
			fixBrokenDataObjectReference();
		}
		if(me.isTakeable()) {
			me.state = Data_Item.STATE_CARRIED;
			spriteRenderer.enabled = false;
		} else {
			Debug.LogError("Cannot pick up " + me);
		}
	}

	// When CHARA drops it
	public void dropFromInventory() {
		if(me.state == Data_Item.STATE_CARRIED) { 
			me.state = Data_Item.STATE_DROPPED;
			spriteRenderer.enabled = true;
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
			spriteRenderer.enabled = false;
		} else {
			Debug.LogError("Cannot transfer " + me + " to cadaver");
		}
	}

	// When CHARA gets to the ritual room
	public void placeForRitual(Vector3 targetPosition, float duration) {
		if(me.state == Data_Item.STATE_CARRIED) { 
			me.state = Data_Item.STATE_PLACED;
			StartCoroutine(materializeAtRitualPosition(targetPosition, duration));
		} else {
			Debug.LogError("Cannot put down " + me);
		}
	}

	// Fixes the this.me reference in case it got broken
	private void fixBrokenDataObjectReference() {
		// Parse own index out of the GameObject name
		if(name.StartsWith("Item")) {
			int ownIndex = int.Parse(name.Substring(4, 2));
			// Fix the object references
			me = GS.getItemByIndex(ownIndex);
			me.control = this;
			Debug.LogWarning(name + " had to fix its data object reference!");
		} else {
			throw new ArgumentException("Malformed item name: " + name);
		}
	}
}
