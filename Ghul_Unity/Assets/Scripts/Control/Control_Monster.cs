using UnityEngine;
using System;
using System.Collections;
using System.Linq;

public class Control_Monster : Control_Character {
	
    [NonSerialized]
	private Data_Monster me;
	protected override Data_Character getMe() { return me as Data_Character; }
    [NonSerialized]
	private Data_PlayerCharacter Toni;
	[NonSerialized]
	private AI_WorldModel worldModel;

	private float MARGIN_DOOR_ENTRANCE;
	private float KILL_RADIUS;
	private float TIME_TO_REACT;
	private float ATTACK_RANGE;
	private float ATTACK_MARGIN;
	private float ATTACK_DURATION;
	private int RITUAL_ROOM_INDEX;

	// Artificial intelligence controls
	public bool IS_DANGEROUS; // set to false to make (the monster "blind" or) the civilians walk around aimlessly.
	private bool IS_CIVILIAN = false;

	private bool newNoiseHeard;
	private Data_Door lastNoiseHeardFrom;
	private float lastNoiseVolume;

	// Monster AI states
	public const int STATE_WANDERING = 0;
	public const int STATE_SEARCHING = 1;
	public const int STATE_STALKING = 2;
	public const int STATE_PURSUING = 3;
	public const int STATE_ATTACKING = 4;
	public const int STATE_FLEEING = 5;

	public int myState;
	private float stateUpdateCooldown;
	private double certaintyThresholdToStartPursuing;

	private Data_Door nextDoorToSearch;

	// prefab, to be placed for each death
	public Transform tombstone; 

	// Graphics parameters
	private GameObject monsterImageObject;
	private SpriteRenderer monsterRenderer;
	private GameObject civilianObject;
	private SpriteRenderer civilianRenderer;

	void Start() {
		IS_DANGEROUS = true;
		monsterImageObject = GameObject.Find("MonsterImage");
		monsterRenderer = monsterImageObject.GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		civilianObject = GameObject.Find("StickmanCivilian");
		civilianRenderer = civilianObject.GetComponent<SpriteRenderer>();
		civilianObject.SetActive(false); // Civ-Monster not visible at first.
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState)
	{
		this.GS = gameState;
        this.me = gameState.getMonster();
		this.Toni = gameState.getToni();
		this.currentEnvironment = me.isIn.env;

		RUNNING_SPEED = Global_Settings.read("MONSTER_WALKING_SPEED");
		WALKING_SPEED = Global_Settings.read("MONSTER_SLOW_WALKING_SPEED");
		KILL_RADIUS = Global_Settings.read("MONSTER_KILL_RADIUS");
		MARGIN_DOOR_ENTRANCE = Global_Settings.read("MARGIN_DOOR_ENTRANCE");

		ATTACK_RANGE = Global_Settings.read("MONSTER_ATTACK_RANGE");
		ATTACK_MARGIN = Global_Settings.read("MONSTER_ATTACK_MARGIN");
		ATTACK_DURATION = Global_Settings.read("MONSTER_ATTACK_DURATION");

		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");
		RITUAL_ROOM_INDEX = (int)Global_Settings.read("RITUAL_ROOM_INDEX");

        // Move the character sprite directly to where the game state says it should be standing
        Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);

		// Artificial intelligence
		worldModel = new AI_WorldModel(gameState);
		StartCoroutine(displayWorldState(1/60));
		// Initialize the state machine
		myState = STATE_SEARCHING;
		stateUpdateCooldown = -1.0f;
		certaintyThresholdToStartPursuing = 0.5;
    }

	private IEnumerator displayWorldState(float interval) {
		while(true) {
			if(!GS.SUSPENDED) {
				foreach(Data_Room room in GS.ROOMS.Values) {
					room.env.updateDangerIndicator(worldModel.probabilityThatToniIsInRoom[room.INDEX]);
				}
			}
			yield return new WaitForSecondsRealtime(interval);
		}
	}

	void FixedUpdate() {
		if (GS == null || GS.SUSPENDED) { return; } // Don't do anything if the game state is not loaded yet or suspended

		// Sanity check: if there are NaNs in world state, reset the whole thing
		for(int i = 0; i < worldModel.probabilityThatToniIsInRoom.Length; i++) {
			if(double.IsNaN(worldModel.probabilityThatToniIsInRoom[i])) {
				Debug.LogWarning("NaN detected, resetting world model");
				worldModel.reset(GS);
				break;
			}
		}

		// If monster sees Toni, everything else is irrelevant
		if(GS.monsterSeesToni) {
			worldModel.toniKnownToBeInRoom(me.isIn);
		} else {
			// Otherwise, predict Toni's movements according to blind transition model
			worldModel.predictOneTimeStep();
			// And if a noise has been heard, update the model accordingly
			if(newNoiseHeard) {
				worldModel.filter(lastNoiseVolume, lastNoiseHeardFrom);
				newNoiseHeard = false;
			}
		}

		// Update monster state as necessary
		updateState();
	}

	// The sound system triggers this function to inform the monster of incoming sounds
	public void hearNoise(Data_Door doorway, float loudness) {
		lastNoiseVolume = loudness;
		lastNoiseHeardFrom = doorway;
		newNoiseHeard = true;
	}

	// Updates the internal state if necessary
	private void updateState() {
		// Check the state update cooldown
		if(stateUpdateCooldown > 0) {
			stateUpdateCooldown -= Time.fixedDeltaTime;
			return;
		}

		// Update AI state 
		if(!IS_DANGEROUS && myState != STATE_FLEEING) {
			myState = STATE_WANDERING;
		} else {
			switch(myState) {
			// If, while searching, a definitive position is established, start stalking
			case STATE_SEARCHING:
				if(worldModel.certainty >= certaintyThresholdToStartPursuing) {
					myState = STATE_STALKING;
					nextDoorToSearch = null;
				}
				break;
			// If, while stalking, monster sees Toni, start pursuing
			// But if Tonis position no longer certain, start searching again
			case STATE_STALKING:
				if(GS.monsterSeesToni) {
					myState = STATE_PURSUING;
				} else if(worldModel.certainty < certaintyThresholdToStartPursuing) {
					myState = STATE_SEARCHING;
				}
				break;
			// If, while pursuing, monster sees Toni is within attack range, initiate an attack
			// On the other hand, if Toni cannot be seen, start stalking again
			case STATE_PURSUING:
				if(GS.monsterSeesToni && Math.Abs(GS.distanceToToni - ATTACK_RANGE) <= ATTACK_MARGIN) {
					myState = STATE_ATTACKING;
					stateUpdateCooldown = ATTACK_DURATION;
				} else if(!GS.monsterSeesToni) {
					myState = STATE_STALKING;
				}
				break;
			// If, after attacking, the monster still sees Toni, but he is out of range, start pursuing again
			// On the other hand, if Toni cannot be seen, start stalking again
			case STATE_ATTACKING:
				if(GS.monsterSeesToni && Math.Abs(GS.distanceToToni - ATTACK_RANGE) > ATTACK_MARGIN) {
					myState = STATE_PURSUING;
				} else if(!GS.monsterSeesToni) {
					myState = STATE_STALKING;
				}
				break;
			// Wandering is a special case: if the ritual has been performed, start fleeing from Toni
			case STATE_WANDERING:
				if(GS.monsterSeesToni && GS.RITUAL_PERFORMED) {
					myState = STATE_FLEEING;
				}
				break;
			// Stop fleeing if Toni is no longer seen
			case STATE_FLEEING:
				if(!GS.monsterSeesToni) {
					myState = STATE_WANDERING;
				}
				break;
			// Searching is the default state
			default:
				myState = STATE_SEARCHING;
				break;
			}
		}

	}

	// Update is called once per frame
	void Update () {
		if (GS == null || GS.SUSPENDED) { return; } // Don't do anything if the game state is not loaded yet or suspended
		if (me.etherialCooldown > 0.0f) { // While the character is etherial, don't do anything
			me.etherialCooldown -= Time.deltaTime;
			return;
		}

		if (GS.RITUAL_PERFORMED) {
			// TODO: Move monster to ritual room, standing before only usable door
			IS_DANGEROUS = false;
			monsterImageObject.SetActive(false);
		} else {
			civilianObject.SetActive(false); // Monster-Toni not visible at first.
		}

		if (GS.CIVILIAN_KILLED) {
			GS.CIVILIAN_KILLED = false;
			dieAndRespawn();
		}

		// Correct state handling
		switch(myState) {
		default:
		case STATE_SEARCHING:
			enactSearchPolicy();
			break;
		/*
		case STATE_STALKING:
			// ???
			break;
		case STATE_PURSUING:
			// Run towards Toni
			break;
		case STATE_ATTACKING:
			// Initiate attack action unless already initiated
			break;
		case STATE_WANDERING:
			// Go towards a random neighbouring room
			// Civs are allowed to enter the ritual room, but monster even if peaceful is not
			break;
		case STATE_FLEEING:
			// Run away from Toni
			break;
		*/
		}
	}

	// Walk towards the neighbouring room that appears to be the closest to Toni's suspected position
	private void enactSearchPolicy() {
		// Only conduct full search if no door has been selected as target yet
		if(nextDoorToSearch == null) {
			// Analyze available door options
			double highestDoorUtility = double.MinValue; double curUtility;
			foreach(Data_Door door in me.isIn.DOORS.Values) {
				// Ignore any doors leading to the ritual room
				if(door.connectsTo.isIn.INDEX == RITUAL_ROOM_INDEX) { continue; }
				// Calculate the utility of going through that door
				curUtility = 0;
				foreach(Data_Room room in GS.ROOMS.Values) {
					if(room != me.isIn && room.INDEX != RITUAL_ROOM_INDEX) { // Ignore this room, as well as the ritual room
						curUtility += worldModel.probabilityThatToniIsInRoom[room.INDEX] *
							(GS.distanceBetweenTwoRooms[me.isIn.INDEX, room.INDEX] - GS.distanceBetweenTwoRooms[door.connectsTo.isIn.INDEX, room.INDEX]);
					}
				}
				// Check if the new utility is higher than the one found previously
				if(curUtility > highestDoorUtility) {
					nextDoorToSearch = door;
					highestDoorUtility = curUtility;
				}
			}
		}
		// With the door set, move towards and through it
		float distToDoor = nextDoorToSearch.visiblePos - me.atPos;
		if(Math.Abs(distToDoor) <= MARGIN_DOOR_ENTRANCE) {
			//Debug.Log("monster at " + me.pos + " and door at " + nextDoorToSearch.pos);
			StartCoroutine(goThroughTheDoor(nextDoorToSearch));
		} else {
			Data_Door triggeredDoor = walk(distToDoor, false, Time.deltaTime);
			if(triggeredDoor == nextDoorToSearch) {
				StartCoroutine(goThroughTheDoor(nextDoorToSearch));
			}
		}
	}

	// Killing the monster / civilian during endgame
	public void dieAndRespawn() {
		Vector3 pos = transform.position;

		if (!IS_CIVILIAN) {
			Debug.Log("The monster died.");
			monsterImageObject.SetActive(false);
			civilianObject.SetActive(true);
			IS_CIVILIAN = true;
		} else {
			Debug.Log("A civilian died.");
		}
		// Place a tombstone where the death occured
		Instantiate(tombstone, new Vector3(pos.x, pos.y - 1.7f, pos.z), Quaternion.identity);

		// Move the civilian to a distant room
		me.updatePosition(GS.getRoomFurthestFrom(me.isIn.INDEX), 0, 0);
		// Move the character sprite directly to where the game state says it should be standing
		Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);

		// Trigger an autosave after killing
		Data_GameState.saveToDisk(GS);
	}

	// Superclass functions implemented
	protected override void setSpriteFlip(bool state) {
		monsterRenderer.flipX = !state;
		civilianRenderer.flipX = state;
	}
	protected override bool canRun() {
		return true; // The monster can always run
	}
	protected override void resetAttackStatus() {
		me.playerDetected = false;
	}
	// Instead of updating statistics, the monster updates its position in the world model and checks whether it sees Toni
	protected override void updateDoorUsageStatistic(Data_Door door, Data_Room currentRoom, Data_Door destinationDoor, Data_Room destinationRoom) {
		worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
		nextDoorToSearch = null;
	}
	// The rest stays empty for now (only relevant for Toni)...
	protected override void updateStamina(bool isRunning) {}
	protected override void regainStamina() {}
	protected override void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos) {}
	protected override void activateCooldown(float duration) { me.etherialCooldown = duration; }
	protected override void cameraFadeOut(float duration) {}
	protected override void cameraFadeIn(float duration) {}
	protected override void makeNoise(int type, Data_Position atPos) {}
}
