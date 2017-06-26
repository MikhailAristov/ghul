using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class Control_PlayerCharacter : Control_Character {

	[NonSerialized]
	private Data_PlayerCharacter me;

	protected override Data_Character getMe() {
		return me as Data_Character;
	}

	// General settings
	private float WALKING_RUNNING_THRESHOLD;
	private float SINGLE_STEP_LENGTH;
	private float walkingDistanceSinceLastNoise;

	private float RUNNING_STAMINA_LOSS;
	private float WALKING_STAMINA_GAIN;
	private float STANDING_STAMINA_GAIN;

	private float RITUAL_PENTAGRAM_CENTER;
	private float RITUAL_ITEM_PLACEMENT_RADIUS;
	private float RITUAL_ITEM_PLACEMENT_DURATION;
	private float SUICIDLE_DURATION;

	private float RESPAWN_TRANSITION_DURATION;
	private float INVENTORY_DISPLAY_DURATION;
	private float MARGIN_ITEM_COLLECT;
	private float ITEM_PICKUP_DURATION;
	private float ITEM_ZAP_DURATION;

	// Graphics parameters
	public GameObject stickmanObject;
	public SpriteRenderer stickmanRenderer;
	public GameObject monsterToniObject;
	public SpriteRenderer monsterToniRenderer;
	public Control_Camera mainCameraControl;
	public Control_CorpsePool CorpsePoolControl;
	public Control_Noise noiseSystem;
	public Control_Music JukeboxControl;
	public Image carriedItemUI;
	public GameObject pentagram;

	// Animator for transitioning between animation states
	public Animator animatorHuman;
	public Animator animatorMonsterToni;
	private bool isRunning;

	// Most basic initialization
	void Awake() {
		WALKING_SPEED = Global_Settings.read("CHARA_WALKING_SPEED");
		RUNNING_SPEED = Global_Settings.read("CHARA_RUNNING_SPEED");
		WALKING_RUNNING_THRESHOLD = (WALKING_SPEED + RUNNING_SPEED) / 2;
		SINGLE_STEP_LENGTH = Global_Settings.read("CHARA_SINGLE_STEP_LENGTH");

		RUNNING_STAMINA_LOSS = Global_Settings.read("RUNNING_STAMINA_LOSS");
		WALKING_STAMINA_GAIN = Global_Settings.read("WALKING_STAMINA_GAIN");
		STANDING_STAMINA_GAIN = Global_Settings.read("STANDING_STAMINA_GAIN");

		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");

		RESPAWN_TRANSITION_DURATION = Global_Settings.read("TOTAL_DEATH_DURATION");
		INVENTORY_DISPLAY_DURATION = Global_Settings.read("INVENTORY_DISPLAY_DURATION");
		MARGIN_ITEM_COLLECT = Global_Settings.read("MARGIN_ITEM_COLLECT");

		RITUAL_ROOM_INDEX = Global_Settings.readInt("RITUAL_ROOM_INDEX");
		RITUAL_PENTAGRAM_CENTER = Global_Settings.read("RITUAL_PENTAGRAM_CENTER");
		RITUAL_ITEM_PLACEMENT_RADIUS = Global_Settings.read("RITUAL_PENTAGRAM_RADIUS") / 2;
		RITUAL_ITEM_PLACEMENT_DURATION = Global_Settings.read("RITUAL_ITEM_PLACEMENT");
		SUICIDLE_DURATION = Global_Settings.read("SUICIDLE_DURATION");

		ATTACK_RANGE = Global_Settings.read("MONSTER_ATTACK_RANGE");
		ATTACK_MARGIN = Global_Settings.read("TONI_ATTACK_MARGIN");
		ATTACK_DURATION = Global_Settings.read("TONI_ATTACK_DURATION");
		ATTACK_COOLDOWN = Global_Settings.read("TONI_ATTACK_COOLDOWN");

		ITEM_PICKUP_DURATION = Global_Settings.read("ITEM_PICKUP_DURATION");
		ITEM_ZAP_DURATION = Global_Settings.read("ITEM_ZAP_DURATION");
	}

	// Use this for initialization; note that only local variables are initialized here, game state is loaded later
	void Start() {
		carriedItemUI.CrossFadeAlpha(0.0f, 0.0f, false);
		walkingDistanceSinceLastNoise = 0;
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState) {
		this.GS = gameState;
		this.me = gameState.getToni();
		this.currentEnvironment = me.isIn.env;

		// Ensure the appropriate sprite display
		switch(GS.OVERALL_STATE) {
		default:
		case Control_GameState.STATE_COLLECTION_PHASE:
			stickmanObject.SetActive(true);
			monsterToniObject.SetActive(false);
			break;
		case Control_GameState.STATE_TRANSFORMATION:
		case Control_GameState.STATE_MONSTER_PHASE:
			stickmanObject.SetActive(true);
			monsterToniObject.SetActive(false);
			break;
		case Control_GameState.STATE_MONSTER_DEAD:
			stickmanObject.SetActive(false);
			monsterToniObject.SetActive(false);
			break;
		}

		// Move the character sprite directly to where the game state says it should be standing
		Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);
	}

	// Update is called once per frame
	void Update() {
		// Don't do anything if the game state is not loaded yet or suspended or in the final endgame state
		if(GS == null || GS.SUSPENDED || GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_DEAD) {
			animatorHuman.speed = 0;
			animatorMonsterToni.speed = 0;
			return; 
		} else {
			animatorHuman.speed = 1f;
			animatorMonsterToni.speed = 1f;
		}
		// While the character is etherial, don't do anything
		if(me.cooldown > 0) {
			me.cooldown -= Time.deltaTime;
			return;
		}
		// This is for the suicidle later...
		me.timeWithoutAction += Time.deltaTime;

		// Item actions before the ritual or attack action after ritual
		if(Input.GetButtonDown("Action")) {
			if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
				// If the player is already carrying the item, display it instead
				if(me.carriedItem != null) {
					StartCoroutine(displayInventory());
				} else {
					// Otherwise, check whether there is an item to be picked up at the current location
					Data_Item thisItem = GS.getItemAtPos(me.pos, MARGIN_ITEM_COLLECT);
					// If so, play the animation to take it
					if(thisItem != null) {
						StartCoroutine(takeItem(thisItem));
					}
				}
			} else if(!attackAnimationPlaying) {
				me.timeWithoutAction = 0;
				activateCooldown(ATTACK_DURATION + ATTACK_COOLDOWN);
				StartCoroutine(playAttackAnimation(me.atPos + (monsterToniRenderer.flipX ? 1f : -1f), GS.getMonster()));
			}
		}

		// Show inventory
		if(Input.GetButtonDown("Inventory")) { 
			StartCoroutine(displayInventory());
		}

		// Drop (debug only)
		if(Debug.isDebugBuild && Input.GetButtonDown("Drop")) {
			dropItem();
		}

		// Dying on command (debug only)
		if(Debug.isDebugBuild && Input.GetButtonDown("Die")) {
			StartCoroutine(dieAndRespawn());
		}

		// If conditions for placing the item at the pentagram are right, do just that
		if(me.carriedItem != null && me.isIn.INDEX == RITUAL_ROOM_INDEX &&
		   Math.Abs(RITUAL_PENTAGRAM_CENTER - me.atPos) <= RITUAL_ITEM_PLACEMENT_RADIUS) {
			StartCoroutine(putItemOntoPentagram());
		}

		// Vertical "movement"
		if(Input.GetButtonDown("Vertical")) {
			me.timeWithoutAction = 0;
			// Check if the character can walk through the door, and if so, move them to the "other side"
			Data_Door door = currentEnvironment.getDoorAtPos(transform.position.x);
			if(door != null) {
				goingThroughADoor = true;
				StartCoroutine(goThroughTheDoor(door));
				return;
			}
		}

		// Running switch
		isRunning = Input.GetButton("Run");

		// Horizontal movement
		if(Mathf.Abs(Input.GetAxis("Horizontal")) > 0.01f) {
			me.timeWithoutAction = 0;
			Data_Door walkIntoDoor = walk(Input.GetAxis("Horizontal"), isRunning, Time.deltaTime);
			if(walkIntoDoor != null) {
				// Walk through the door if triggered
				goingThroughADoor = true;
				StartCoroutine(goThroughTheDoor(walkIntoDoor));
				return;
			}
		} else {
			regainStamina();
		}

		// Suicidle...
		if(GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_PHASE) {
			if(me.timeWithoutAction >= SUICIDLE_DURATION) {
				me.timeWithoutAction = 0;
				StartCoroutine(dieAndRespawn());
			}
			mainCameraControl.setRedOverlay(me.timeWithoutAction / SUICIDLE_DURATION);
		}
	}

	protected new void FixedUpdate() {
		base.FixedUpdate();

		// Don't do anything if the game state is not loaded yet or suspended
		if(GS == null || GS.SUSPENDED) {
			return;
		}

		// Update the movement statistics during the collection stage
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			// Update the ticks for running and standing statistics
			if(me.currentVelocityAbsolute < ANIM_MIN_SPEED_FOR_WALKING) {
				me.cntStandingSinceLastDeath++;
			} else if(me.currentVelocityAbsolute < WALKING_RUNNING_THRESHOLD) {
				me.cntWalkingSinceLastDeath++;
			} else {
				me.cntRunningSinceLastDeath++;
			}
			// Update the distance walked in the current room
			me.increaseWalkedDistance(me.currentVelocityAbsolute * Time.fixedDeltaTime);
		}

		// Transition for walking / running animation
		switch(GS.OVERALL_STATE) {
		case Control_GameState.STATE_COLLECTION_PHASE:
			if(animatorHuman != null && animatorHuman.isInitialized) {
				animatorHuman.SetFloat("Speed", animatorMovementSpeed);
				animatorHuman.SetBool("Is Running", isRunning && animatorMovementSpeed > ANIM_MIN_SPEED_FOR_WALKING && canRun());
				animatorHuman.SetBool("Is Exhausted", me.exhausted);
			}
			break;
		case Control_GameState.STATE_MONSTER_PHASE:
		case Control_GameState.STATE_TRANSFORMATION:
			if(animatorMonsterToni != null && animatorMonsterToni.isInitialized) {
				animatorMonsterToni.SetFloat("Speed", animatorMovementSpeed);
			}
			break;
		default:
			break;
		}
	}

	// Superclass functions implemented
	public override void setSpriteFlip(bool state) {
		stickmanRenderer.flipX = state;
		monsterToniRenderer.flipX = !state;
	}

	protected override bool canRun() {
		// Human Toni gets exhausted, monster Toni doesn't
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			return !me.exhausted; 
		} else {
			return true;
		}
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
			noiseSystem.makeNoise(type, atPos);
		}
	}

	public override void getHit() {
		halt();
		StartCoroutine(dieAndRespawn());
	}

	private IEnumerator dieAndRespawn() {
		Debug.Log(me + " died...");
		activateCooldown(RESPAWN_TRANSITION_DURATION);

		if(GS.OVERALL_STATE < Control_GameState.STATE_TRANSFORMATION) {
			float timeStep = RESPAWN_TRANSITION_DURATION / 2, waitUntil = Time.timeSinceLevelLoad;

			// Trigger the death animation and wait until it's half-done
			waitUntil += timeStep;
			if(animatorHuman != null && animatorHuman.isInitialized) {
				animatorHuman.SetTrigger("Is Killed");
			}
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);

			// Now fade out the main camera while the death animation is still playing
			mainCameraControl.fadeOut(timeStep);
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);

			// While the camera is dark, hide Toni's actual body and replace it with a corpse from the pool
			stickmanRenderer.enabled = false;
			CorpsePoolControl.placeHumanCorpse(me.isIn.env.gameObject, me.pos.asLocalVector(), stickmanRenderer.flipX);
			GS.updateCadaverPosition(me.pos, stickmanRenderer.flipX);
			// Transfer the current item if any to the cadaver
			leaveItemOnCadaver();

			// Now move the Toni sprite back to the starting room
			me.resetPosition(GS);
			currentEnvironment = me.isIn.env;
			transform.position = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
			setSpriteFlip(true); // Flip the sprite towards the wall scribbles
			stickmanRenderer.enabled = true;

			// Reset movements and animations
			Input.ResetInputAxes();
			halt();
			if(animatorHuman != null && animatorHuman.isInitialized) {
				animatorHuman.SetTrigger("Is Resurrected");
			}

			// Trigger the house mix up and a new item
			GS.TONI_KILLED = true;
			GS.ANOTHER_ITEM_PLEASE = true;
			me.deaths++;

			// Fade back in
			mainCameraControl.fadeIn(timeStep);
		} else {
			// During the endgame, simply replace the monster Toni sprite with the cadaver
			GS.TONI_KILLED = true;
			monsterToniRenderer.enabled = false;
			CorpsePoolControl.placeMonsterCorpse(me.isIn.env.gameObject, me.pos.asLocalVector(), stickmanRenderer.flipX);
			GS.updateCadaverPosition(me.pos, stickmanRenderer.flipX);
			activateCooldown(0);
		}

		// Trigger an autosave upon changing locations
		Control_Persistence.saveToDisk(GS);
	}

	// The player takes a nearby item if there is any
	private IEnumerator takeItem(Data_Item thisItem) {
		float waitUntil = Time.timeSinceLevelLoad;
		halt();
		// Check if the item that would be picked up is the one currently sought for the ritual
		if(thisItem != GS.getCurrentItem()) {
			// If not, play the zap
			Debug.Log(thisItem + " is not the item you are looking for (" + GS.getCurrentItem() + ")");
			setSpriteFlip(thisItem.atPos < me.atPos);
			if(animatorHuman != null && animatorHuman.isInitialized) {
				animatorHuman.SetTrigger("Is Zapped");
			}
			activateCooldown(ITEM_ZAP_DURATION);
			// Make a zapping noise at the location
			noiseSystem.makeNoise(Control_Noise.NOISE_TYPE_ZAP, me.pos);
		} else {
			// If this is the correct item, take it by activate the appropriate animation
			triggerItemAnimation(thisItem.pos.asLocalVector());
			JukeboxControl.playItemPickupJingle(GS.numItemsPlaced, delay:0.33f);
			activateCooldown(ITEM_PICKUP_DURATION);
			// Wait until the animation is half-complete
			waitUntil += ITEM_PICKUP_DURATION / 2;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);
			// Move item to inventory
			if(thisItem.control == null) {
				thisItem.fixObjectReferences(GS, null);
			}
			thisItem.control.moveToInventory();
			me.carriedItem = thisItem;
			Debug.Log(thisItem + " has been collected");
			// Make noise at the current location
			noiseSystem.makeNoise(Control_Noise.NOISE_TYPE_ITEM, me.pos);
			// Show inventory
			StartCoroutine(displayInventory());
			// Now wait until the animation is done before saving
			yield return new WaitUntil(() => (me.cooldown <= 0));
			// Auto save when collecting an item.
			Control_Persistence.saveToDisk(GS);
		}
	}

	private void triggerItemAnimation(Vector2 itemPosition) {
		// First, flip the sprite in the appropriate direction
		setSpriteFlip(itemPosition.x < me.atPos);
		// Then, set the proper trigger, depending on where the item is
		if(animatorHuman != null && animatorHuman.isInitialized) {
			if(itemPosition.y > -0.8f) {
				animatorHuman.SetTrigger("Take High");
			} else if(itemPosition.y < -1.4f) {
				animatorHuman.SetTrigger("Take Low");
			} else {
				animatorHuman.SetTrigger("Take Mid");
			}
		}
	}

	// The player drops the carried item
	public void dropItem() {
		if(me.carriedItem != null) {
			// Drop item down
			me.carriedItem.control.dropFromInventory();
			Debug.Log("Item #" + me.carriedItem.INDEX + " dropped");
			me.carriedItem = null;
			// Make noise at the current location
			noiseSystem.makeNoise(Control_Noise.NOISE_TYPE_ITEM, me.pos);
			// Auto save when dropping an item.
			Control_Persistence.saveToDisk(GS);
		}
	}

	// The player dies and leaves the item on chara's cadaver
	private void leaveItemOnCadaver() {
		// First, reset any item that was already on cadaver back to its spawn point
		foreach(Data_Item item in GS.ITEMS.Values) {
			if(item.state == Data_Item.STATE_ON_CADAVER) {
				item.control.resetToSpawnPosition();
			}
		}
		// Only leave item if you actually carry one
		if(me.carriedItem != null) {
			// Drop item down
			me.carriedItem.control.moveToCadaver();
			Debug.Log("Item #" + me.carriedItem.INDEX + " left on cadaver");
			me.carriedItem = null;
			// No autosave because death already autosaves
		}
	}

	// The player reaches the pentagram with an item
	private IEnumerator putItemOntoPentagram() {
		float waitUntil = Time.timeSinceLevelLoad + ITEM_PICKUP_DURATION;
		// Find the next free slot for the item
		string verticeObjName = "Vertice_" + (GS.numItemsPlaced % 5 + 1);
		Vector3 verticePos = pentagram.transform.FindChild(verticeObjName).transform.position;
		// Stop, play animation, and wait for the item to fully materialize
		halt();
		triggerItemAnimation(verticePos);
		JukeboxControl.playItemPlacementJingle(GS.numItemsPlaced, delay:0.33f);
		activateCooldown(ITEM_PICKUP_DURATION + RITUAL_ITEM_PLACEMENT_DURATION);
		yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);
		// Place the item in it
		me.carriedItem.control.placeForRitual(verticePos, RITUAL_ITEM_PLACEMENT_DURATION);
		Debug.Log("Item #" + me.carriedItem.INDEX + " placed for the ritual");
		me.carriedItem = null;
		// Auto save when placing is complete
		waitUntil += RITUAL_ITEM_PLACEMENT_DURATION;
		yield return new WaitUntil(() => Time.timeSinceLevelLoad > waitUntil);
		Control_Persistence.saveToDisk(GS);
	}

	// For the opening cutscene
	public void enterTheHouse() {
		// Find the door
		Data_Door entryDoor = GS.getRoomByIndex(RITUAL_ROOM_INDEX).rightmostDoor.connectsTo;
		// And go through it
		StartCoroutine(goThroughTheDoor(entryDoor));
		// Turn Toni towards the center of the ritual room
		setSpriteFlip(true);
	}

	public void setupEndgame() {
		stickmanObject.SetActive(false);
		// Only display monster Toni sprite if monster is still alive
		monsterToniObject.SetActive(GS.OVERALL_STATE < Control_GameState.STATE_MONSTER_DEAD);
		monsterToniRenderer.flipX = !stickmanRenderer.flipX;
		// Update speeds
		WALKING_SPEED = Global_Settings.read("MONSTER_SLOW_WALKING_SPEED");
		RUNNING_SPEED = Global_Settings.read("MONSTER_WALKING_SPEED");
		WALKING_RUNNING_THRESHOLD = (WALKING_SPEED + RUNNING_SPEED) / 2;
	}

	// Shows the currentlly carried item on the UI
	private IEnumerator displayInventory() {
		if(me.carriedItem != null) {
			// Update the current image sprite
			carriedItemUI.sprite = me.carriedItem.control.transform.GetComponent<SpriteRenderer>().sprite;
			// Quickly fade the image in and wait
			carriedItemUI.CrossFadeAlpha(1.0f, INVENTORY_DISPLAY_DURATION / 4, false);
			yield return new WaitForSeconds(INVENTORY_DISPLAY_DURATION / 2);
			// Fade it out again slowly
			carriedItemUI.CrossFadeAlpha(0.0f, INVENTORY_DISPLAY_DURATION / 2, false);
		}
	}

	// An emergency brake for cutscenes
	public void halt() {
		me.currentVelocitySigned = 0;
		me.currentVelocityAbsolute = 0;
		animatorMovementSpeed = 0;
		if(animatorHuman != null && animatorHuman.isInitialized) {
			animatorHuman.SetFloat("Speed", 0);
			animatorHuman.SetBool("Is Running", false);
		}
		if(animatorMonsterToni != null && animatorMonsterToni.isInitialized) {
			animatorMonsterToni.SetFloat("Speed", 0);
		}
	}

	// Superclass functions implemented
	protected override void failedDoorTransitionHook(Data_Door doorTaken) {
		if(doorTaken.state == Data_Door.STATE_HELD) {
			GS.getMonster().perception.seeToniRattleAtTheDoorknob(doorTaken);
		} else {
			noiseSystem.makeNoise(Control_Noise.NOISE_TYPE_DOOR, me.pos);
		}
		// Play headshake animation
		if(animatorHuman != null && animatorHuman.isInitialized) {
			animatorHuman.SetTrigger("Shake Head");
		}
	}

	protected override void preDoorTransitionHook(Data_Door doorTaken) {
		if(mainCameraControl.isFocusedOn(me.pos)) {
			mainCameraControl.fadeOut(DOOR_TRANSITION_DURATION / 2);
		}
	}

	protected override void preRoomLeavingHook(Data_Door doorTaken) {
		if(GS.monsterSeesToni) {
			GS.getMonster().perception.seeToniGoThroughDoor(doorTaken);
		}
	}

	protected override void postDoorTransitionHook(Data_Door doorTaken) {
		// Update door usage statistics
		updateDoorUsageStatistic(doorTaken, doorTaken.isIn, doorTaken.connectsTo, doorTaken.connectsTo.isIn);
		// Fade camera back in
		if(mainCameraControl.isFocusedOn(me.pos)) {
			mainCameraControl.fadeIn(DOOR_TRANSITION_DURATION / 2);
		}
		// Make noise
		noiseSystem.makeNoise(Control_Noise.NOISE_TYPE_DOOR, doorTaken.connectsTo.pos);
		walkingDistanceSinceLastNoise = 0;
		// Reset current inputs if they would cause Toni to walk back through the same door
		if((doorTaken.connectsTo.type == Data_Door.TYPE_RIGHT_SIDE && Input.GetAxis("Horizontal") > 0.01f) ||
		   (doorTaken.connectsTo.type == Data_Door.TYPE_LEFT_SIDE && Input.GetAxis("Horizontal") < -0.01f) ||
		   (doorTaken.connectsTo.type == Data_Door.TYPE_BACK_DOOR && Input.GetButtonDown("Vertical"))) {
			Input.ResetInputAxes();
		}
		// During the monster phase, guide monster Toni to the intruders
		if(GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_PHASE) {
			guideMonsterToniToIntruders();
		}
		// Save the game
		Control_Persistence.saveToDisk(GS);
	}

	// This function guides the player to the "civilian" intruders in the house during the monster phase of the game
	public void guideMonsterToniToIntruders(bool drawAttention = false) {
		if(GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_PHASE && !GS.monsterSeesToni) {
			// Find the door in the current room that currently has the shortest distance to the intruder
			Data_Door bestDoor = me.isIn.DOORS.Values[0];
			float shortestDistance = float.MaxValue, curDistance;
			foreach(Data_Door door in me.isIn.DOORS.Values) {
				curDistance = GS.getDistance(door, GS.getMonster().pos);
				if(curDistance < shortestDistance) {
					bestDoor = door;
					shortestDistance = curDistance;
				}
				// Also, close this door if it has been opened and held earlier
				if(door.control.isHeldOpen) {
					door.control.forceClose(silently: true);
				}
			}
			// (Re-)Open this door
			if(drawAttention) {
				bestDoor.control.forceClose(silently: true);
				bestDoor.control.open(silently: false, holdOpen: true, forceCreak: true);
			} else {
				bestDoor.control.open(silently: true, holdOpen: true);
			}
		}
	}

	private void updateDoorUsageStatistic(Data_Door door, Data_Room currentRoom, Data_Door destinationDoor, Data_Room destinationRoom) {
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
		} else {
			Debug.Log("Cannot find door spawn ID for at least one of these doors: " + door.INDEX + "," + destinationDoor.INDEX + ". Just got spawn IDs " + spawn1Index + ", " + spawn2Index);
		}
	}
	// Attacking animation triggers
	protected override void startAttackAnimation() {
		// Activate the attack animation
		if(animatorMonsterToni != null && animatorMonsterToni.isInitialized) {
			animatorMonsterToni.SetTrigger("Attack");
		}
	}

	protected override void stopAttackAnimation() {
		// Cancel the animation
		if(animatorMonsterToni != null && animatorMonsterToni.isInitialized) {
			animatorMonsterToni.SetTrigger("AttackCancel");
		}
	}

	// After each kill, open the door leading to the next intruder
	protected override void postKillHook() {
		guideMonsterToniToIntruders(drawAttention: true);
	}
}
