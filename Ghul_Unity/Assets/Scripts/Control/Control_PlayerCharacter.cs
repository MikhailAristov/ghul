using UnityEngine;
using UnityEngine.UI;
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
	private float SINGLE_STEP_LENGTH;
	private float walkingDistanceSinceLastNoise;

    private float RUNNING_STAMINA_LOSS;
    private float WALKING_STAMINA_GAIN;
    private float STANDING_STAMINA_GAIN;

	private int RITUAL_ROOM_INDEX;
	private float RITUAL_PENTAGRAM_CENTER;
	private float RITUAL_PENTAGRAM_RADIUS;

    private float DOOR_TRANSITION_DURATION;
	private float RESPAWN_TRANSITION_DURATION;
	private float INVENTORY_DISPLAY_DURATION;

	// Gameplay parameters
	private float TIME_TO_REACT;

	// Graphics parameters
	private SpriteRenderer stickmanRenderer;
	private Control_Camera mainCameraControl;
	private Control_Sound soundSystem;
	public Canvas inventoryUI;

    // Use this for initialization; note that only local variables are initialized here, game state is loaded later
    void Start () {
		stickmanRenderer = transform.FindChild("Stickman").GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		mainCameraControl = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Control_Camera>();
		inventoryUI.transform.FindChild("CurrentItem").GetComponent<Image>().CrossFadeAlpha(0.0f, 0.0f, false);
		soundSystem = GameObject.Find("GameState").GetComponent<Control_Sound>();
		walkingDistanceSinceLastNoise = 0;
    }

    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState)
    {
        this.GS = gameState;
        this.me = gameState.getToni();
        this.currentEnvironment = me.isIn.env;
		this.cadaver = GS.getCadaver();

        // Set general movement parameters
        WALKING_SPEED = Global_Settings.read("CHARA_WALKING_SPEED");
        RUNNING_SPEED = Global_Settings.read("CHARA_RUNNING_SPEED");
		SINGLE_STEP_LENGTH = Global_Settings.read("CHARA_SINGLE_STEP_LENGTH");

        RUNNING_STAMINA_LOSS = Global_Settings.read("RUNNING_STAMINA_LOSS");
        WALKING_STAMINA_GAIN = Global_Settings.read("WALKING_STAMINA_GAIN");
        STANDING_STAMINA_GAIN = Global_Settings.read("STANDING_STAMINA_GAIN");

        VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");

		RESPAWN_TRANSITION_DURATION = Global_Settings.read("TOTAL_DEATH_DURATION");
		TIME_TO_REACT = Global_Settings.read("TIME_TO_REACT");

		INVENTORY_DISPLAY_DURATION = Global_Settings.read("INVENTORY_DISPLAY_DURATION");

		RITUAL_ROOM_INDEX = (int)Global_Settings.read("RITUAL_ROOM_INDEX");
		RITUAL_PENTAGRAM_CENTER = Global_Settings.read("RITUAL_PENTAGRAM_CENTER");
		RITUAL_PENTAGRAM_RADIUS = Global_Settings.read("RITUAL_PENTAGRAM_RADIUS");

		me.remainingReactionTime = TIME_TO_REACT;
		
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

		// Item actions
		if (Input.GetButtonDown("Action")) { // Take item
			takeItem();
		}
		if (Input.GetButtonDown("Inventory")) { // Show inventory
			StopCoroutine("displayInventory");
			StartCoroutine("displayInventory");
		}
		if (Debug.isDebugBuild && Input.GetButtonDown("Jump")) { // Drop (debug only)
			dropItem();
		}

		// Dying on command (debug only)
		if (Debug.isDebugBuild && Input.GetButtonDown("Die")) {
			StartCoroutine(dieAndRespawn());
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
			float velocity = WALKING_SPEED; int noiseType = Control_Sound.NOISE_TYPE_WALK;
            if (Input.GetButton("Run") && !me.exhausted) {
                velocity = RUNNING_SPEED;
				noiseType = Control_Sound.NOISE_TYPE_RUN;
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

			// Make noise (if necessary)
			walkingDistanceSinceLastNoise += Mathf.Abs(displacement);
			if(walkingDistanceSinceLastNoise > SINGLE_STEP_LENGTH) {
				soundSystem.makeNoise(noiseType, me.pos);
			}
        } else
        { // Regain stamina while standing still
            me.modifyStamina(STANDING_STAMINA_GAIN * Time.deltaTime);
        }
    }

	// This function transitions the character through a door
	private IEnumerator goThroughTheDoor(Data_Door door) {
		Data_Room currentRoom = me.isIn;
		me.etherialCooldown = DOOR_TRANSITION_DURATION;

		// Fade out and wait
		mainCameraControl.fadeOut(DOOR_TRANSITION_DURATION / 2);
		yield return new WaitForSeconds(DOOR_TRANSITION_DURATION);

		Data_Door destinationDoor = door.connectsTo;
		Data_Room destinationRoom = destinationDoor.isIn;

		// Move character within game state
		float newValidPosition = destinationRoom.env.validatePosition(destinationDoor.atPos);
		me.updatePosition(destinationRoom, newValidPosition);
		currentEnvironment = me.isIn.env;
		me.remainingReactionTime = TIME_TO_REACT;
		mainCameraControl.resetRedOverlay();

		// Move character sprite
		Vector3 targetPosition = new Vector3(newValidPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);

		// Increase number of door uses. Needs door spawn IDs, not door IDs.
		int spawn1Index = -1;
		int spawn2Index = -1;
		Data_Door iteratorDoor;
		// Find the corresponding door spawn IDs.
		for (int i = 0; i < currentRoom.countAllDoorSpawns; i++) {
			iteratorDoor = currentRoom.getDoorAtSpawn(i);
			if (iteratorDoor != null) {
				if (iteratorDoor.INDEX == door.INDEX) {
					spawn1Index = GS.HOUSE_GRAPH.ABSTRACT_ROOMS[currentRoom.INDEX].DOOR_SPAWNS.Values[i].INDEX; // Should work, if room ID and vertex ID really are the same...
					break;
				}
			}
		}
		for (int i = 0; i < destinationRoom.countAllDoorSpawns; i++) {
			iteratorDoor = destinationRoom.getDoorAtSpawn(i);
			if (iteratorDoor != null) {
				if (iteratorDoor.INDEX == destinationDoor.INDEX) {
					spawn2Index = GS.HOUSE_GRAPH.ABSTRACT_ROOMS[destinationRoom.INDEX].DOOR_SPAWNS.Values[i].INDEX; // Should work, if room ID and vertex ID really are the same...
					break;
				}
			}
		}
		if (spawn1Index != -1 && spawn2Index != -1) {
			GS.HOUSE_GRAPH.DOOR_SPAWNS[spawn1Index].increaseNumUses();
			GS.HOUSE_GRAPH.DOOR_SPAWNS[spawn2Index].increaseNumUses();
		} else {
			Debug.Log("Cannot find door spawn ID for at least one of these doors: " + door.INDEX + "," + destinationDoor.INDEX + ". Just got spawn IDs " + spawn1Index + ", " + spawn2Index);
		}

		// Fade back in
		mainCameraControl.fadeIn(DOOR_TRANSITION_DURATION / 2);

		// Make noise at the original door's location
		soundSystem.makeNoise(Control_Sound.NOISE_TYPE_DOOR, door.pos);
		walkingDistanceSinceLastNoise = 0;

		// Trigger an autosave upon changing locations
		Data_GameState.saveToDisk(GS);
	}

	// Player withing the attack radius -> reduce time to react
	public void takeDamage() {
		me.remainingReactionTime -= Time.deltaTime;
		if(me.remainingReactionTime <= 0.0f) {
			StartCoroutine(dieAndRespawn());
		} else {
			mainCameraControl.setRedOverlay(1.0f - me.remainingReactionTime / TIME_TO_REACT);
		}
	}

	private IEnumerator dieAndRespawn() {
		// Start cooldown
		me.etherialCooldown = RESPAWN_TRANSITION_DURATION;

		mainCameraControl.setRedOverlay(0.0f);
		Debug.Log(me + " died...");
		me.deaths++;

		// Hide chara's sprite and replace it with the cadaver
		stickmanRenderer.enabled = false;
		cadaver.gameObj.transform.position = transform.position + (new Vector3(0, -1.55f));
		cadaver.updatePosition(me.isIn, me.atPos);
		// Transfer the current item if any to the cadaver
		leaveItemOnCadaver();

		// Wait before fading out
		yield return new WaitForSeconds(RESPAWN_TRANSITION_DURATION / 3);
		mainCameraControl.fadeOut(RESPAWN_TRANSITION_DURATION / 3);
		// Wait until fade out is complete before moving the sprite
		yield return new WaitForSeconds(RESPAWN_TRANSITION_DURATION / 3);

		// Move the chara sprite back to the starting room
		me.resetPosition(GS);
		currentEnvironment = me.isIn.env;
		transform.position = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		stickmanRenderer.enabled = true;

		// Trigger the house mix up
		GS.KILLED = true;

		// Fade back in
		mainCameraControl.fadeIn(RESPAWN_TRANSITION_DURATION / 3);

		// Reset the hitpoints
		me.remainingReactionTime = TIME_TO_REACT;

		// Trigger an autosave upon changing locations
		Data_GameState.saveToDisk(GS);
	}

	// The player takes a nearby item if there is any
	private void takeItem() {
		Data_Item currentItem = GS.getCurrentItem();
		if(me.carriedItem == null && currentItem.isTakeable() && me.isIn == currentItem.isIn &&
		   Math.Abs(me.atPos - currentItem.atPos) < Global_Settings.read("MARGIN_ITEM_COLLECT")) {
			// the player got the item with index itemIndex.
			currentItem.control.moveToInventory();
			me.carriedItem = currentItem;
			Debug.Log("Item #" + currentItem.INDEX + " collected.");
			// Make noise at the current location
			soundSystem.makeNoise(Control_Sound.NOISE_TYPE_ITEM, me.pos);
			// Auto save when collecting an item.
			Data_GameState.saveToDisk(GS);
			// Show inventory
			StartCoroutine("displayInventory");
		} else {
			Debug.LogWarning("Can't pick up " + currentItem);
		}
	}

	// The player drops the carried item
	public void dropItem() {
		if(me.carriedItem != null) {
			// Drop item down
			me.carriedItem.control.dropFromInventory();
			Debug.Log("Item #" + me.carriedItem.INDEX + " dropped");
			me.carriedItem = null;
			// Reset the reaction time after dropping the item
			me.remainingReactionTime = TIME_TO_REACT;
			// Make noise at the current location
			soundSystem.makeNoise(Control_Sound.NOISE_TYPE_ITEM, me.pos);
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

	// Shows the currentlly carried item on the UI
	private IEnumerator displayInventory() {
		if(me.carriedItem != null) {
			Image curItemImg = inventoryUI.transform.FindChild("CurrentItem").GetComponent<Image>();
			// Update the current image sprite
			curItemImg.sprite = me.carriedItem.control.transform.GetComponent<SpriteRenderer>().sprite;
			// Quickly fade the image in and wait
			curItemImg.CrossFadeAlpha(1.0f, INVENTORY_DISPLAY_DURATION / 4, false);
			yield return new WaitForSeconds(INVENTORY_DISPLAY_DURATION / 2);
			// Fade it out again slowly
			curItemImg.CrossFadeAlpha(0.0f, INVENTORY_DISPLAY_DURATION / 2, false);
		}
	}
}
