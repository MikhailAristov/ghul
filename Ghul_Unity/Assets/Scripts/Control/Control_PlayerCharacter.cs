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

	[NonSerialized]
	private Data_Cadaver cadaver;

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

	// Graphics parameters
	private GameObject stickmanObject;
	private SpriteRenderer stickmanRenderer;
	private GameObject monsterToniObject;
	private SpriteRenderer monsterToniRenderer;
	private Control_Camera mainCameraControl;
	private Control_Sound soundSystem;
	public Canvas inventoryUI;
	public GameObject pentagram;
	public GameObject attackArm;

	// Zapping-effect parameters
	private GameObject zappingSoundObject;
	private AudioSource zappingSound;
	private GameObject zappingParticleObject;
	private ParticleSystem zappingParticles;

	// Use this for initialization; note that only local variables are initialized here, game state is loaded later
	void Start() {
		stickmanObject = GameObject.Find("Stickman");
		stickmanRenderer = stickmanObject.GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		monsterToniObject = GameObject.Find("MonsterToniImage");
		monsterToniRenderer = monsterToniObject.GetComponent<SpriteRenderer>();
		monsterToniObject.SetActive(false); // Monster-Toni not visible at first.
		zappingSoundObject = GameObject.Find("ZappingSound");
		zappingSound = zappingSoundObject.GetComponent<AudioSource>();
		zappingParticleObject = GameObject.Find("ZapEffect");
		zappingParticles = zappingParticleObject.GetComponent<ParticleSystem>();
		attackArmRenderer = attackArm.GetComponent<LineRenderer>();

		mainCameraControl = Camera.main.GetComponent<Control_Camera>();
		inventoryUI.transform.FindChild("CurrentItem").GetComponent<Image>().CrossFadeAlpha(0.0f, 0.0f, false);
		soundSystem = GameObject.Find("GameState").GetComponent<Control_Sound>();
		walkingDistanceSinceLastNoise = 0;
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState) {
		this.GS = gameState;
		this.me = gameState.getToni();
		this.currentEnvironment = me.isIn.env;
		this.cadaver = GS.getCadaver();

		// Set general movement parameters
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

		RITUAL_ROOM_INDEX = (int)Global_Settings.read("RITUAL_ROOM_INDEX");
		RITUAL_PENTAGRAM_CENTER = Global_Settings.read("RITUAL_PENTAGRAM_CENTER");
		RITUAL_ITEM_PLACEMENT_RADIUS = Global_Settings.read("RITUAL_PENTAGRAM_RADIUS") / 10f;
		RITUAL_ITEM_PLACEMENT_DURATION = Global_Settings.read("RITUAL_ITEM_PLACEMENT");
		SUICIDLE_DURATION = Global_Settings.read("SUICIDLE_DURATION");

		ATTACK_RANGE = Global_Settings.read("MONSTER_ATTACK_RANGE");
		ATTACK_MARGIN = Global_Settings.read("TONI_ATTACK_MARGIN");
		ATTACK_DURATION = Global_Settings.read("TONI_ATTACK_DURATION");
		ATTACK_COOLDOWN = Global_Settings.read("TONI_ATTACK_COOLDOWN");
		
		// Move the character sprite directly to where the game state says it should be standing
		Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);
	}

	// Update is called once per frame
	void Update() {
		// Don't do anything if the game state is not loaded yet or suspended or in the final endgame state
		if(GS == null || GS.SUSPENDED || GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_DEAD) { 
			return; 
		} 
		if(me.etherialCooldown > 0.0f) { // While the character is etherial, don't do anything
			me.etherialCooldown -= Time.deltaTime;
			return;
		}
		// This is for the suicidle later...
		me.timeWithoutAction += Time.deltaTime;

		// Item actions or attack after ritual
		if(Input.GetButtonDown("Action")) {
			me.timeWithoutAction = 0;
			if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
				takeItem();
			} else if(!attackAnimationPlaying) {
				StartCoroutine(playAttackAnimation(me.atPos + (monsterToniRenderer.flipX ? 1f : -1f), GS.getMonster()));
			}
		}
		if(Input.GetButtonDown("Inventory")) { // Show inventory
			StopCoroutine("displayInventory");
			StartCoroutine("displayInventory");
		}
		if(Debug.isDebugBuild && Input.GetButtonDown("Jump")) { // Drop (debug only)
			dropItem();
		}

		// Dying on command (debug only)
		if(Debug.isDebugBuild && Input.GetButtonDown("Die")) {
			StartCoroutine(dieAndRespawn());
		}

		// If conditions for placing the item at the pentagram are right, do just that
		if(me.carriedItem != null && me.isIn.INDEX == RITUAL_ROOM_INDEX &&
			Math.Abs(RITUAL_PENTAGRAM_CENTER - me.atPos) <= RITUAL_ITEM_PLACEMENT_RADIUS) {
			GS.indexOfSearchedItem++; // now the next item is to be searched
			StartCoroutine(putItemOntoPentagram());
		}

		// Vertical "movement"
		if(Input.GetButtonDown("Vertical")) {
			me.timeWithoutAction = 0;
			// Check if the character can walk through the door, and if so, move them to the "other side"
			Data_Door door = currentEnvironment.getDoorAtPos(transform.position.x);
			if(door != null) {
				StartCoroutine(goThroughTheDoor(door));
				return;
			}
		}

		// Horizontal movement
		if(Mathf.Abs(Input.GetAxis("Horizontal")) > 0.01f) {
			me.timeWithoutAction = 0;
			Data_Door walkIntoDoor = walk(Input.GetAxis("Horizontal"), Input.GetButton("Run"), Time.deltaTime);
			if(walkIntoDoor != null) {
				// Walk through the door if triggered
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
			if(Math.Abs(me.currentVelocity) < 0.1f) {
				me.cntStandingSinceLastDeath++;
			} else if(Math.Abs(me.currentVelocity) < WALKING_RUNNING_THRESHOLD) {
				me.cntWalkingSinceLastDeath++;
			} else {
				me.cntRunningSinceLastDeath++;
			}
			// Update the distance walked in the current room
			me.roomHistory[me.roomHistory.Count - 1].increaseWalkedDistance(me.currentVelocity * Time.fixedDeltaTime);
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

	public override void getHit() {
		StartCoroutine(dieAndRespawn());
	}

	private IEnumerator dieAndRespawn() {
		Debug.Log(me + " died...");

		// Start cooldown
		me.etherialCooldown = RESPAWN_TRANSITION_DURATION;

		if(GS.OVERALL_STATE < Control_GameState.STATE_TRANSFORMATION) {
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
			GS.TONI_KILLED = true;
			me.deaths++;

			// Fade back in
			mainCameraControl.fadeIn(RESPAWN_TRANSITION_DURATION / 3);
		} else {
			// During the endgame, simply replace the monster Toni sprite with the cadaver
			GS.TONI_KILLED = true;
			monsterToniRenderer.enabled = false;
			cadaver.gameObj.transform.position = transform.position + (new Vector3(0, -1.55f));
			cadaver.updatePosition(me.isIn, me.atPos);
			me.etherialCooldown = 0;
		}

		// Trigger an autosave upon changing locations
		Control_Persistence.saveToDisk(GS);
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
			Control_Persistence.saveToDisk(GS);
			// Show inventory
			StartCoroutine("displayInventory");
		} else {
			// Check whether other item is within picking-distance
			bool itemNearby = false;
			Vector3 destPos = new Vector3(); // to place the zap-particle where the item is located
			bool error = false;
			foreach(Data_Item item in GS.ITEMS.Values) {
				if(me.isIn == item.isIn && Math.Abs(me.atPos - item.atPos) < Global_Settings.read("MARGIN_ITEM_COLLECT") && item.INDEX != currentItem.INDEX) {
					itemNearby = true;
					if(item.gameObj != null) {
						destPos = item.gameObj.transform.position;
					} else {
						// Somehow there's no gameObj attached to the item. Weird.
						error = true;
					}
				}
			}
			if(itemNearby && !error) {
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
			// Make noise at the current location
			soundSystem.makeNoise(Control_Sound.NOISE_TYPE_ITEM, me.pos);
			// Auto save when dropping an item.
			Control_Persistence.saveToDisk(GS);
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
	private IEnumerator putItemOntoPentagram() {
		// Find the next free slot for the item
		string verticeObjName = "Vertice_" + (GS.numItemsPlaced % 5 + 1);
		Vector3 verticePos = pentagram.transform.FindChild(verticeObjName).transform.position;
		// Place the item in it
		me.carriedItem.control.placeForRitual(verticePos, RITUAL_ITEM_PLACEMENT_DURATION);
		Debug.Log("Item #" + me.carriedItem.INDEX + " placed for the ritual");
		me.carriedItem = null;
		// Wait for the item to fully materialize
		activateCooldown(RITUAL_ITEM_PLACEMENT_DURATION);
		yield return new WaitForSeconds(RITUAL_ITEM_PLACEMENT_DURATION);
		// Auto save when placing is complete
		Control_Persistence.saveToDisk(GS);
	}

	public void setupEndgame() {
		stickmanObject.SetActive(false);
		// Only display monster Toni sprite if monster is still alive
		monsterToniObject.SetActive(GS.OVERALL_STATE < Control_GameState.STATE_MONSTER_DEAD);
		monsterToniRenderer.flipX = !stickmanRenderer.flipX;
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

	// Superclass functions implemented
	protected override void activateCooldown(float duration) {
		me.etherialCooldown = duration;
	}

	protected override void preDoorTransitionHook(Data_Door doorTaken) {
		mainCameraControl.fadeOut(DOOR_TRANSITION_DURATION / 2);
		
	}
	protected override void preRoomLeavingHook(Data_Door doorTaken) {
		if(GS.monsterSeesToni) {
			GS.getMonster().control.seeToniGoThroughDoor(doorTaken);
		}
	}
	protected override void postDoorTransitionHook(Data_Door doorTaken) {
		// Update door usage statistics
		updateDoorUsageStatistic(doorTaken, doorTaken.isIn, doorTaken.connectsTo, doorTaken.connectsTo.isIn);
		// Update room visitation history
		me.roomHistory.Add(new AI_RoomHistory(doorTaken.connectsTo.isIn));
		// Fade camera back in
		mainCameraControl.fadeIn(DOOR_TRANSITION_DURATION / 2);
		// Make noise
		soundSystem.makeNoise(Control_Sound.NOISE_TYPE_DOOR, doorTaken.pos);
		walkingDistanceSinceLastNoise = 0;
		// Save the game
		Control_Persistence.saveToDisk(GS);
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
	// The rest stays empty for now (only relevant for the monster)...
	protected override void postKillHook() {}
}
