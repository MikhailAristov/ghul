using UnityEngine;
using System;
using System.Collections;

public class Control_PlayerCharacter : MonoBehaviour {

    [NonSerialized]
    private Data_GameState GS;
    [NonSerialized]
    private Environment_Room currentEnvironment;
    [NonSerialized]
    private Data_PlayerCharacter me;
	[NonSerialized]
	private Data_Cadaver cadaver;

	// General settings
    private float VERTICAL_ROOM_SPACING;

    private float WALKING_SPEED;
    private float RUNNING_SPEED;

    private float RUNNING_STAMINA_LOSS;
    private float WALKING_STAMINA_GAIN;
    private float STANDING_STAMINA_GAIN;

	private int RITUAL_ROOM_INDEX;
	private float RITUAL_PENTAGRAM_CENTER;
	private float RITUAL_PENTAGRAM_RADIUS;

    private float DOOR_TRANSITION_DURATION;
	private float RESPAWN_TRANSITION_DURATION;

	// Gameplay parameters
	private float timeLeftToEscapeTheMonster;

	// Graphics parameters
	private SpriteRenderer stickmanRenderer;
	private Control_Camera MAIN_CAMERA_CONTROL;

    // Use this for initialization; note that only local variables are initialized here, game state is loaded later
    void Start () {
		stickmanRenderer = transform.FindChild("Stickman").GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		MAIN_CAMERA_CONTROL = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();
    }

    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState)
    {
        this.GS = gameState;
        this.me = gameState.getCHARA();
        this.currentEnvironment = me.isIn.env;
		this.cadaver = GS.getCadaver();

        // Set general movement parameters
        WALKING_SPEED = GS.getSetting("CHARA_WALKING_SPEED");
        RUNNING_SPEED = GS.getSetting("CHARA_RUNNING_SPEED");

        RUNNING_STAMINA_LOSS = GS.getSetting("RUNNING_STAMINA_LOSS");
        WALKING_STAMINA_GAIN = GS.getSetting("WALKING_STAMINA_GAIN");
        STANDING_STAMINA_GAIN = GS.getSetting("STANDING_STAMINA_GAIN");

        VERTICAL_ROOM_SPACING = GS.getSetting("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = GS.getSetting("DOOR_TRANSITION_DURATION");

		RESPAWN_TRANSITION_DURATION = GS.getSetting("TOTAL_DEATH_DURATION");
		timeLeftToEscapeTheMonster = GS.getSetting("TIME_TO_REACT");

		RITUAL_ROOM_INDEX = (int)GS.getSetting("RITUAL_ROOM_INDEX");
		RITUAL_PENTAGRAM_CENTER = GS.getSetting("RITUAL_PENTAGRAM_CENTER");
		RITUAL_PENTAGRAM_RADIUS = GS.getSetting("RITUAL_PENTAGRAM_RADIUS");

		me.remainingReactionTime = timeLeftToEscapeTheMonster;
		
        // Move the character sprite directly to where the game state says it should be standing
        Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
        transform.Translate(savedPosition - transform.position);
    }

    // Update is called once per frame
    void Update () {
		if (GS == null || GS.SUSPENDED) { return; } // Don't do anything if the game state is not loaded yet or suspended
		if (me.etherialCooldown > 0.0f) { // While the character is etherial, don't do anything
			me.etherialCooldown -= Time.deltaTime;
			return;
		}

		//FOR DEBUGGIN ONLY - Dying on command
		if (Debug.isDebugBuild && Input.GetButtonDown("Die")) {
			StartCoroutine(dieAndRespawn());
		}

		// Item actions
		if (Input.GetButtonDown("Action")) { // Take
			takeItem();
		}
		if (Debug.isDebugBuild && Input.GetButtonDown("Jump")) { // Drop (debug only)
			dropItem();
		}

		// If conditions for placing the item at the pentagram are right, do just that
		if(me.carriedItem != null && me.isIn.INDEX == RITUAL_ROOM_INDEX &&
		    Math.Abs(RITUAL_PENTAGRAM_CENTER - me.atPos) <= RITUAL_PENTAGRAM_RADIUS) {
			putItemOntoPentagram();
		}

        // Vertical "movement"
        if (Input.GetAxis("Vertical") > 0.1f)
        {
            // Check if the character can walk through the door, and if so, move them to the "other side"
            Data_Door door = currentEnvironment.getDoorAtPos(transform.position.x);
            if (door != null) {
				StartCoroutine(goThroughTheDoor(door));
                return;
            }
        }
			
        // Horizontal movement
        if (Input.GetAxis("Horizontal") > 0.01f || Input.GetAxis("Horizontal") < -0.01f)
        {
            // Flip the sprite as necessary
			stickmanRenderer.flipX = (Input.GetAxis("Horizontal") < 0.0f) ? true : false;

            // Determine movement speed
            float velocity = WALKING_SPEED;
            if (Input.GetButton("Run") && !me.exhausted) {
                velocity = RUNNING_SPEED;
                me.modifyStamina(RUNNING_STAMINA_LOSS * Time.deltaTime);
            } else {
                me.modifyStamina(WALKING_STAMINA_GAIN * Time.deltaTime);
            }

            // Calculate the new position
            float displacement = Input.GetAxis("Horizontal") * Time.deltaTime * velocity;
            float newPosition = transform.position.x + displacement;
            float validPosition = currentEnvironment.validatePosition(transform.position.x + displacement);
            
            // If validated position is different from the calculated position, we have reached the side of the room
			if(validPosition > newPosition) {		// moving left (negative direction) gets us through the left door
                Data_Door leftDoor = currentEnvironment.getDoorOnTheLeft();
				if (leftDoor != null) { StartCoroutine(goThroughTheDoor(leftDoor)); }
                return;
            }
			else if (validPosition < newPosition) {	// moving right (positive direction) gets us through the right door
                Data_Door rightDoor = currentEnvironment.getDoorOnTheRight();
				if (rightDoor != null) { StartCoroutine(goThroughTheDoor(rightDoor)); }
                return;
            }

            // Move the sprite to the new valid position
            me.updatePosition(validPosition);
            float validDisplacement = validPosition - transform.position.x;
            if (Mathf.Abs(validDisplacement) > 0.0f)
            {
                transform.Translate(validDisplacement, 0, 0);
            }
        } else
        { // Regain stamina while standing still
            me.modifyStamina(STANDING_STAMINA_GAIN * Time.deltaTime);
        }
    }

	// This function transitions the character through a door
	private IEnumerator goThroughTheDoor(Data_Door door) {
		me.etherialCooldown = DOOR_TRANSITION_DURATION;

		// Fade out and wait
		MAIN_CAMERA_CONTROL.fadeOut(DOOR_TRANSITION_DURATION / 2);
		yield return new WaitForSeconds(DOOR_TRANSITION_DURATION);

		Data_Door destinationDoor = door.connectsTo;
		Data_Room destinationRoom = destinationDoor.isIn;

		// Move character within game state
		float newValidPosition = destinationRoom.env.validatePosition(destinationDoor.atPos);
		me.updatePosition(destinationRoom, newValidPosition);
		currentEnvironment = me.isIn.env;
		me.remainingReactionTime = timeLeftToEscapeTheMonster;

		// Move character sprite
		Vector3 targetPosition = new Vector3(newValidPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);

		// Fade back in
		MAIN_CAMERA_CONTROL.fadeIn(DOOR_TRANSITION_DURATION / 2);

		Debug.Log(me + " walks from door #" + door + " to door #" + destinationDoor + " at position " + targetPosition);

		// Trigger an autosave upon changing locations
		Data_GameState.saveToDisk(GS);
	}

	// Player withing the attack radius -> reduce time to react
	public void beingAttacked() {
		me.remainingReactionTime -= Time.deltaTime;
		if (me.remainingReactionTime <= 0.0f) {
			if(me.carriedItem != null) { // First drop the item and reset the timer
				dropItem();
				me.remainingReactionTime = timeLeftToEscapeTheMonster;
			} else {
				StartCoroutine(dieAndRespawn());
			}
		}
	}

	private IEnumerator dieAndRespawn() {
		// Start cooldown
		me.etherialCooldown = RESPAWN_TRANSITION_DURATION;

		Debug.Log(me + " died...");

		// Hide chara's sprite and replace it with the cadaver
		stickmanRenderer.enabled = false;
		cadaver.gameObj.transform.position = transform.position + (new Vector3(0, -1.55f));
		cadaver.updatePosition(me.isIn, me.atPos);
		// Transfer the current item if any to the cadaver
		leaveItemOnCadaver();

		// Wait before fading out
		yield return new WaitForSeconds(RESPAWN_TRANSITION_DURATION / 3);
		MAIN_CAMERA_CONTROL.fadeOut(RESPAWN_TRANSITION_DURATION / 3);
		// Wait until fade out is complete before moving the sprite
		yield return new WaitForSeconds(RESPAWN_TRANSITION_DURATION / 3);

		// Move the chara sprite back to the starting room
		me.resetPosition(GS);
		currentEnvironment = me.isIn.env;
		transform.position = new Vector3(me.startingPos.X, me.startingPos.RoomId * VERTICAL_ROOM_SPACING);
		stickmanRenderer.enabled = true;

		// Fade back in
		MAIN_CAMERA_CONTROL.fadeIn(RESPAWN_TRANSITION_DURATION / 3);

		// Reset the hitpoints
		me.remainingReactionTime = timeLeftToEscapeTheMonster;

		// Trigger an autosave upon changing locations
		Data_GameState.saveToDisk(GS);
	}

	// The player takes a nearby item if there is any
	private void takeItem() {
		Data_Item currentItem = GS.getCurrentItem();
		if(me.carriedItem == null && currentItem.isTakeable() && me.isIn == currentItem.isIn &&
		   Math.Abs(me.atPos - currentItem.atPos) < GS.getSetting("MARGIN_ITEM_COLLECT")) {
			// the player got the item with index itemIndex.
			currentItem.control.moveToInventory();
			me.carriedItem = currentItem;
			Debug.Log("Item #" + currentItem.INDEX + " collected.");

			// Auto save when collecting an item.
			Data_GameState.saveToDisk(GS);
		} else {
			Debug.LogWarning("Can't pick up " + currentItem);
		}
	}

	// The player drops the carried item
	private void dropItem() {
		if(me.carriedItem != null) {
			// Drop item down
			me.carriedItem.control.dropFromInventory();
			Debug.Log("Item #" + me.carriedItem.INDEX + " dropped");
			me.carriedItem = null;
			// Auto save when dropping an item.
			Data_GameState.saveToDisk(GS);
		}
	}

	// The player dies and leaves the item on chara's cadaver
	private void leaveItemOnCadaver() {
		if(me.carriedItem != null) {
			// Drop item down
			me.carriedItem.control.moveToCadaver();
			Debug.Log("Item #" + me.carriedItem.INDEX + " left on cadaver");
			me.carriedItem = null;
			// Not autosave because death already autosaves
		} else {
			Data_Item curItem = GS.getCurrentItem();
			if(curItem.state == Data_Item.STATE_ON_CADAVER) {
				curItem.control.resetToSpawnPosition();
			}
		}
	}

	// The player reaches the pentagram with an item
	private void putItemOntoPentagram() {
		me.carriedItem.control.placeForRitual();
		Debug.Log("Item #" + me.carriedItem.INDEX + " placed for the ritual");
		me.carriedItem = null;
		// Auto save when placing an item.
		Data_GameState.saveToDisk(GS);
	}
}
