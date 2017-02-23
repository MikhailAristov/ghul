using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class Control_PlayerCharacter : Control_Character {

	[NonSerialized]
	private Data_PlayerCharacter me;
	protected override Data_Character getMe() { return me as Data_Character; }
	[NonSerialized]
	private Data_Cadaver cadaver;

	// General settings
	private float SINGLE_STEP_LENGTH;
	private float walkingDistanceSinceLastNoise;

    private float RUNNING_STAMINA_LOSS;
    private float WALKING_STAMINA_GAIN;
    private float STANDING_STAMINA_GAIN;

	private int RITUAL_ROOM_INDEX;
	private float RITUAL_PENTAGRAM_CENTER;
	private float RITUAL_PENTAGRAM_RADIUS;

	private float RESPAWN_TRANSITION_DURATION;
	private float INVENTORY_DISPLAY_DURATION;

	// Gameplay parameters
	private float TIME_TO_REACT;
	private bool isTransformed;

	// Graphics parameters
	private GameObject stickmanObject;
	private SpriteRenderer stickmanRenderer;
	private GameObject monsterToniObject;
	private SpriteRenderer monsterToniRenderer;
	private Control_Camera mainCameraControl;
	private Control_Sound soundSystem;
	public Canvas inventoryUI;

	// Zapping-effect parameters
	private GameObject zappingSoundObject;
	private AudioSource zappingSound;
	private GameObject zappingParticleObject;
	private ParticleSystem zappingParticles;

    // Use this for initialization; note that only local variables are initialized here, game state is loaded later
    void Start () {
		stickmanObject = GameObject.Find("Stickman");
		stickmanRenderer = stickmanObject.GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		monsterToniObject = GameObject.Find("MonsterToniImage");
		monsterToniRenderer = monsterToniObject.GetComponent<SpriteRenderer>();
		monsterToniObject.SetActive(false); // Monster-Toni not visible at first.
		zappingSoundObject = GameObject.Find("ZappingSound");
		zappingSound = zappingSoundObject.GetComponent<AudioSource>();
		zappingParticleObject = GameObject.Find("ZapEffect");
		zappingParticles = zappingParticleObject.GetComponent<ParticleSystem>();

		isTransformed = false;
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

		// Transformation into monster
		if (!isTransformed && GS.RITUAL_PERFORMED) {
			stickmanObject.SetActive(false);
			monsterToniObject.SetActive(true);
			if (!stickmanRenderer.flipX) {
				monsterToniRenderer.flipX = true;
			}
			isTransformed = true;
		}

		// Item actions or attack after ritual
		if (Input.GetButtonDown("Action")) {
			if (!GS.RITUAL_PERFORMED) {
				takeItem();
			} else {
				attack();
			}
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

		// Debug: Make New Game Button clickable again.
		if (Debug.isDebugBuild && Input.GetButtonDown("MakeResettable")) {
			GS.RITUAL_PERFORMED = false;
		}

		// If conditions for placing the item at the pentagram are right, do just that
		if(me.carriedItem != null && me.isIn.INDEX == RITUAL_ROOM_INDEX &&
		    Math.Abs(RITUAL_PENTAGRAM_CENTER - me.atPos) <= RITUAL_PENTAGRAM_RADIUS) {
			GS.indexOfSearchedItem++; // now the next item is to be searched
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
		if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.01f)
        {
			Data_Door walkIntoDoor = walk(Input.GetAxis("Horizontal"), Input.GetButton("Run"));
			if(walkIntoDoor != null) {
				// Walk through the door if triggered
				StartCoroutine(goThroughTheDoor(walkIntoDoor));
			}
		} else {
			regainStamina();
        }
	}
	// Superclass functions implemented
	protected override void setSpriteFlip(bool state) {
		stickmanRenderer.flipX = state;
		monsterToniRenderer.flipX = !state;
	}
	protected override bool canRun() {
		return !me.exhausted; 
	}
	protected override void updateStamina(bool isRunning) {
		if(isRunning) {
			me.modifyStamina(RUNNING_STAMINA_LOSS * Time.deltaTime);
		} else {
			me.modifyStamina(WALKING_STAMINA_GAIN * Time.deltaTime);
		}
	}
	protected override void regainStamina() {
		me.modifyStamina(STANDING_STAMINA_GAIN * Time.deltaTime);
	}
	protected override void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos) {
		walkingDistanceSinceLastNoise += Mathf.Abs(walkedDistance);
		if(walkingDistanceSinceLastNoise > SINGLE_STEP_LENGTH) {
			soundSystem.makeNoise(type, atPos);
		}
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

		// Hide Toni's sprite and replace it with the cadaver
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

		// Move the Toni sprite back to the starting room
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
			GS.numItemsCollected++;
			Debug.Log("Item #" + currentItem.INDEX + " collected.");
			// Make noise at the current location
			soundSystem.makeNoise(Control_Sound.NOISE_TYPE_ITEM, me.pos);
			// Auto save when collecting an item.
			Data_GameState.saveToDisk(GS);
			// Show inventory
			StartCoroutine("displayInventory");
		} else {
			// Check whether other item is within picking-distance
			bool itemNearby = false;
			Vector3 destPos = new Vector3(); // to place the zap-particle where the item is located
			foreach (Data_Item item in GS.ITEMS.Values) {
				if (me.isIn == item.isIn && Math.Abs(me.atPos - item.atPos) < Global_Settings.read("MARGIN_ITEM_COLLECT") && item.INDEX != currentItem.INDEX) {
					itemNearby = true;
					destPos = item.gameObj.transform.position;
				}
			}
			if (itemNearby) {
				// emit zapping sound and visual effect
				zappingParticleObject.transform.Translate(destPos - zappingParticleObject.transform.position);
				zappingParticles.Play();
				zappingSound.Play();
				soundSystem.makeNoise(Control_Sound.NOISE_TYPE_ZAP, me.pos);
			}

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

	// Attack and kill the other monster / civilians after the ritual has been performed
	private void attack() {
		if (me.isIn.INDEX == GS.getMonster().isIn.INDEX) {
			if (Mathf.Abs(transform.position.x - GS.getMonster().gameObj.transform.position.x) <= Global_Settings.read("MONSTER_KILL_RADIUS")) {
				// Kill the monster or civilian
				GS.CIVILIAN_KILLED = true;
			}
		}
	}

	// Superclass functions implemented 
	protected override void activateCooldown(float duration) {
		me.etherialCooldown = duration;
	}
	protected override void cameraFadeOut(float duration) {
		mainCameraControl.fadeOut(duration);
	}
	protected override void cameraFadeIn(float duration) {
		mainCameraControl.fadeIn(duration);
	}
	protected override void resetAttackStatus() {
		me.remainingReactionTime = TIME_TO_REACT;
		mainCameraControl.resetRedOverlay();
	}
	protected override void makeNoise(int type, Data_Position atPos) {
		soundSystem.makeNoise(type, atPos);
		walkingDistanceSinceLastNoise = 0;
	}
	protected override void updateDoorUsageStatistic(Data_Door door, Data_Room currentRoom, Data_Door destinationDoor, Data_Room destinationRoom) {
		int spawn1Index = -1;
		int spawn2Index = -1;
		Data_Door iteratorDoor;
		// Find the corresponding door spawn IDs.
		for(int i = 0; i < currentRoom.countAllDoorSpawns; i++) {
			iteratorDoor = currentRoom.getDoorAtSpawn(i);
			if(iteratorDoor != null) {
				if(iteratorDoor.INDEX == door.INDEX) {
					spawn1Index = GS.HOUSE_GRAPH.ABSTRACT_ROOMS[currentRoom.INDEX].DOOR_SPAWNS.Values[i].INDEX;
					// Should work, if room ID and vertex ID really are the same...
					break;
				}
			}
		}
		for(int i = 0; i < destinationRoom.countAllDoorSpawns; i++) {
			iteratorDoor = destinationRoom.getDoorAtSpawn(i);
			if(iteratorDoor != null) {
				if(iteratorDoor.INDEX == destinationDoor.INDEX) {
					spawn2Index = GS.HOUSE_GRAPH.ABSTRACT_ROOMS[destinationRoom.INDEX].DOOR_SPAWNS.Values[i].INDEX;
					// Should work, if room ID and vertex ID really are the same...
					break;
				}
			}
		}
		if(spawn1Index != -1 && spawn2Index != -1) {
			GS.HOUSE_GRAPH.DOOR_SPAWNS[spawn1Index].increaseNumUses();
			GS.HOUSE_GRAPH.DOOR_SPAWNS[spawn2Index].increaseNumUses();
		}
		else {
			Debug.Log("Cannot find door spawn ID for at least one of these doors: " + door.INDEX + "," + destinationDoor.INDEX + ". Just got spawn IDs " + spawn1Index + ", " + spawn2Index);
		}
	}
}
