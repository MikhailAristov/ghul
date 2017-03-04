using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
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
	private float SCREEN_SIZE_HORIZONTAL;
	private float KILL_RADIUS;
	private float TIME_TO_REACT;
	private float ATTACK_RANGE;
	private float ATTACK_MARGIN;
	private float ATTACK_DURATION;
	private float ATTACK_COOLDOWN;
	private int RITUAL_ROOM_INDEX;

	// Artificial intelligence controls
	//public bool IS_DANGEROUS; // set to false to make (the monster "blind" or) the civilians walk around aimlessly.
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
	private Data_Room previousRoomVisited; // This can be used to prevent endless door walk cycles

	private bool attackAnimationPlaying;

	// prefab, to be placed for each death
	public Transform tombstone;
	public GameObject attackArm;
	private LineRenderer attackArmRenderer;

	// Graphics parameters
	private GameObject monsterImageObject;
	private SpriteRenderer monsterRenderer;
	private GameObject civilianObject;
	private SpriteRenderer civilianRenderer;

	void Start() {
		monsterImageObject = GameObject.Find("MonsterImage");
		monsterRenderer = monsterImageObject.GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		civilianObject = GameObject.Find("StickmanCivilian");
		civilianRenderer = civilianObject.GetComponent<SpriteRenderer>();
		civilianObject.SetActive(false); // Civ-Monster not visible at first.
		attackArmRenderer = attackArm.GetComponent<LineRenderer>();
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
		SCREEN_SIZE_HORIZONTAL = Global_Settings.read("SCREEN_SIZE_HORIZONTAL");

		ATTACK_RANGE = Global_Settings.read("MONSTER_ATTACK_RANGE");
		ATTACK_MARGIN = Global_Settings.read("MONSTER_ATTACK_MARGIN");
		ATTACK_DURATION = Global_Settings.read("MONSTER_ATTACK_DURATION");
		ATTACK_COOLDOWN = Global_Settings.read("MONSTER_ATTACK_COOLDOWN");

		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");
		RITUAL_ROOM_INDEX = (int)Global_Settings.read("RITUAL_ROOM_INDEX");

        // Move the character sprite directly to where the game state says it should be standing
        Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);

		// Artificial intelligence
		worldModel = new AI_WorldModel(gameState);
		StartCoroutine(displayWorldState(1/60));
		worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
		// Initialize the state machine
		myState = STATE_SEARCHING;
		stateUpdateCooldown = -1.0f;
		certaintyThresholdToStartPursuing = 0.5;
		previousRoomVisited = me.isIn;
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

	// Inform the monster that Toni has just walked through this door to its other side
	public void seeToniGoThroughDoor(Data_Door originDoor) {
		worldModel.toniKnownToBeInRoom(originDoor.connectsTo.isIn);
	}

	// Updates the internal state if necessary
	private void updateState() {
		// Check the state update cooldown
		if(stateUpdateCooldown > 0) {
			stateUpdateCooldown -= Time.fixedDeltaTime;
			return;
		}

		// Update AI state 
		switch(myState) {
		// If, while searching, a definitive position is established, start stalking
		case STATE_SEARCHING:
			if(worldModel.certainty >= certaintyThresholdToStartPursuing) {
				// Keep the monster in the stalking mode for a bit to ensure it spots the player
				stateUpdateCooldown = DOOR_TRANSITION_DURATION;
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
			if(!Toni.isInvulnerable() && GS.monsterSeesToni && Math.Abs(Math.Abs(GS.distanceToToni) - ATTACK_RANGE) <= ATTACK_MARGIN) {
				myState = STATE_ATTACKING;
				stateUpdateCooldown = ATTACK_DURATION + ATTACK_COOLDOWN;
			} else if(!GS.monsterSeesToni) {
				stateUpdateCooldown = DOOR_TRANSITION_DURATION;
				myState = STATE_STALKING;
			}
			break;
		// If, after attacking, the monster still sees Toni, but he is out of range, start pursuing again
		// On the other hand, if Toni cannot be seen, start stalking again
		case STATE_ATTACKING:
			if(GS.monsterSeesToni && Math.Abs(Math.Abs(GS.distanceToToni) - ATTACK_RANGE) > ATTACK_MARGIN) {
				myState = STATE_PURSUING;
			} else if(!GS.monsterSeesToni) {
				myState = STATE_STALKING;
			}
			break;
		// Wandering is a special case: if the ritual has been performed, start fleeing from Toni
		case STATE_WANDERING:
			if(GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < (SCREEN_SIZE_HORIZONTAL / 2)) { // && GS.RITUAL_PERFORMED
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

	// Update is called once per frame
	void Update () {
		if (GS == null || GS.SUSPENDED) { return; } // Don't do anything if the game state is not loaded yet or suspended
		if (me.etherialCooldown > 0.0f) { // While the character is etherial, don't do anything
			me.etherialCooldown -= Time.deltaTime;
			return;
		}

		if (GS.RITUAL_PERFORMED) {
			// TODO: Move monster to ritual room, standing before only usable door
			myState = STATE_WANDERING;
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
			enactSearchPolicy(true);
			break;
		case STATE_PURSUING:
			enactPursuitPolicy();
			break;
		case STATE_ATTACKING:
			if(!attackAnimationPlaying) {
				StartCoroutine(playAttackAnimation());
			}
			break;
		case STATE_WANDERING:
			enactSearchPolicy(false);
			break;
		case STATE_FLEEING:
			enactFlightPolicy();
			break;
		/*
		case STATE_STALKING:
			// ???
			break;
		*/
		}
	}

	// Walk towards the neighbouring room that appears to be the closest to Toni's suspected position (in aggressive mode)
	// Or walk around randomply (in wandering mode)
	private void enactSearchPolicy(bool aggressiveSearch) {
		// Only conduct full search if no door has been selected as target yet
		if(nextDoorToSearch == null) {
			// Analyze available door options
			double highestDoorUtility = double.MinValue; double curUtility;
			foreach(Data_Door door in me.isIn.DOORS.Values) {
				curUtility = 0;

				// Try to ignore any doors leading to the ritual room
				int targetRoomIndex = door.connectsTo.isIn.INDEX;
				if(targetRoomIndex == RITUAL_ROOM_INDEX && !IS_CIVILIAN) {
					curUtility = double.MinValue / 2;
				} else if (targetRoomIndex == previousRoomVisited.INDEX) {
					curUtility = double.MinValue / 4;
				}

				// Calculate the utility of going through that door
				if(aggressiveSearch) {
					foreach(Data_Room room in GS.ROOMS.Values) {
						if(room != me.isIn && room.INDEX != RITUAL_ROOM_INDEX) { // Ignore this room, as well as the ritual room
							curUtility += worldModel.probabilityThatToniIsInRoom[room.INDEX] *
							(GS.distanceBetweenTwoRooms[me.isIn.INDEX, room.INDEX] - GS.distanceBetweenTwoRooms[targetRoomIndex, room.INDEX]);
						}
					}
				} else { // Non-aggressive search just picks doors at random
					curUtility += UnityEngine.Random.Range(0, 10f);
				}

				// Check if the new utility is higher than the one found previously
				if(curUtility > highestDoorUtility) {
					nextDoorToSearch = door;
					highestDoorUtility = curUtility;
				}
			}
		}
		// With the door set, move towards and through it
		walkToAndThroughDoor(nextDoorToSearch, false, Time.deltaTime);
	}

	// Run towards within striking range of Toni
	private void enactPursuitPolicy() {
		// Predict the possible attack positions
		float tonisPredictedPosition = Toni.atPos + ATTACK_DURATION * Toni.currentVelocity;
		float attackPointLeft = tonisPredictedPosition - ATTACK_RANGE * 0.99f;
		float attackPointRight = tonisPredictedPosition + ATTACK_RANGE * 0.99f;
		// Choose the best attack position
		if(Math.Abs(attackPointLeft - me.atPos) < Math.Abs(attackPointRight - me.atPos) && attackPointLeft > me.isIn.leftWalkBoundary) {
			walk(attackPointLeft - me.atPos, true, Time.deltaTime);
		} else if(attackPointRight < me.isIn.rightWalkBoundary) {
			walk(attackPointRight - me.atPos, true, Time.deltaTime);
		} else { 
			// In case both attack points are unreachable, just run towards Toni to scare him into fleeing
			walk(GS.distanceToToni, true, Time.deltaTime);
		}
	}

	// Play out the attack animation
	private IEnumerator playAttackAnimation() {
		attackAnimationPlaying = true;
		// Flip the sprite if necessary
		setSpriteFlip(Toni.atPos < me.atPos);
		float attackPoint = me.atPos + Math.Sign(Toni.atPos - me.atPos) * ATTACK_RANGE;
		Debug.LogWarning("monster attacks from " + me.pos + " to " + attackPoint);
		// PHASE 1: Attack
		float attackProgress = 0f; float attackProgressStep = 1f/60f;
		while(attackProgress < 1f) {
			if(!GS.SUSPENDED) {
				// TODO play one frame forward
				attackArmRenderer.SetPosition(1, new Vector3(attackProgress * (attackPoint - me.atPos), 0, 0));
				attackProgress += attackProgressStep;
			}
			yield return new WaitForSeconds(ATTACK_DURATION * attackProgressStep);
		}
		// PHASE 2: Resolve
		if(!Toni.isInvulnerable() && GS.monsterSeesToni && Math.Abs(Toni.atPos - attackPoint) <= ATTACK_MARGIN) {
			Toni.control.getHit();
		}
		// PHASE 3: Cooldown
		attackArmRenderer.SetPosition(1, new Vector2(0, 0));
		yield return new WaitForSeconds(ATTACK_COOLDOWN);
		attackAnimationPlaying = false;
	}

	private void enactFlightPolicy() {
		if(nextDoorToSearch == null) {
			// Analyze available door options
			double highestDoorUtility = double.MinValue; double curUtility;
			foreach(Data_Door door in me.isIn.DOORS.Values) {
				// Utility is basically equal to the distance to the door, plus a random factor to avoid deadlocks
				curUtility = Math.Abs(door.visiblePos - me.atPos) + UnityEngine.Random.Range(0f,1f);
				// However, having to run past Monster!Toni is penalized
				bool toniIsBetweenMeAndDoor = (door.atPos <= Toni.atPos && Toni.atPos <= me.atPos) || (door.atPos >= Toni.atPos && Toni.atPos >= me.atPos);
				curUtility -= toniIsBetweenMeAndDoor ? (double.MaxValue / 2) : 0;
				// Check if the new utility is higher than the one found previously
				if(curUtility > highestDoorUtility) {
					nextDoorToSearch = door;
					highestDoorUtility = curUtility;
				}
			}
		}
		// With the door set, move towards and through it at runnig pace
		walkToAndThroughDoor(nextDoorToSearch, true, Time.deltaTime);
	}

	// Walk towards the door
	private void walkToAndThroughDoor(Data_Door door, bool run, float deltaTime) {
		float distToDoor = door.visiblePos - me.atPos;
		if(Math.Abs(distToDoor) <= MARGIN_DOOR_ENTRANCE) {
			StartCoroutine(goThroughTheDoor(door));
		} else {
			Data_Door triggeredDoor = walk(distToDoor, run, deltaTime);
			if(triggeredDoor == door) {
				StartCoroutine(goThroughTheDoor(triggeredDoor));
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
		previousRoomVisited = currentRoom;
	}
	// The rest stays empty for now (only relevant for Toni)...
	protected override void updateStamina(bool isRunning) {}
	protected override void regainStamina() {}
	protected override void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos) {}
	protected override void activateCooldown(float duration) { me.etherialCooldown = duration; }
	protected override void doBeforeLeavingRoom(Data_Door doorTaken) {}
	protected override void cameraFadeOut(float duration) {}
	protected override void cameraFadeIn(float duration) {}
	protected override void makeNoise(int type, Data_Position atPos) {}
}
