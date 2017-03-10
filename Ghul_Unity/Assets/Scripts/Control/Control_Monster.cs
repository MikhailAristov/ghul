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

	private float MARGIN_DOOR_ENTRANCE;
	private float SCREEN_SIZE_HORIZONTAL;
	private float MARGIN_ITEM_STEAL;
	private int RITUAL_ROOM_INDEX;

	// Artificial intelligence controls
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

	private float stateUpdateCooldown;
	private float distanceThresholdToStartPursuing; // = AGGRO * screen width / 2
	private double certaintyThresholdToStartStalking;

	private Data_Door nextDoorToGoThrough;
	private Data_Room previousRoomVisited; // This can be used to prevent endless door walk cycles

	// prefab, to be placed for each death
	public Transform tombstone;
	public GameObject attackArm;

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
		MARGIN_DOOR_ENTRANCE = Global_Settings.read("MARGIN_DOOR_ENTRANCE");
		SCREEN_SIZE_HORIZONTAL = Global_Settings.read("SCREEN_SIZE_HORIZONTAL");

		ATTACK_RANGE = Global_Settings.read("MONSTER_ATTACK_RANGE");
		ATTACK_MARGIN = Global_Settings.read("MONSTER_ATTACK_MARGIN");
		ATTACK_DURATION = Global_Settings.read("MONSTER_ATTACK_DURATION");
		ATTACK_COOLDOWN = Global_Settings.read("MONSTER_ATTACK_COOLDOWN");

		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");
		RITUAL_ROOM_INDEX = (int)Global_Settings.read("RITUAL_ROOM_INDEX");
		MARGIN_ITEM_STEAL = Global_Settings.read("MARGIN_ITEM_COLLECT") / 10f;

		// Setup endgame, if it has already been triggered
		if(GS.RITUAL_PERFORMED) {
			setupEndgame();
		}

        // Move the character sprite directly to where the game state says it should be standing
        Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);

		// Artificial intelligence
		me.worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
		StartCoroutine(displayWorldState(1f/60f));
		// Initialize the state machine
		stateUpdateCooldown = -1.0f;
		certaintyThresholdToStartStalking = 0.5;
		previousRoomVisited = me.isIn;
    }

	private IEnumerator displayWorldState(float interval) {
		while(true) {
			if(!GS.SUSPENDED) {
				foreach(Data_Room room in GS.ROOMS.Values) {
					room.env.updateDangerIndicator(me.worldModel.probabilityThatToniIsInRoom[room.INDEX]);
				}
			}
			yield return new WaitForSecondsRealtime(interval);
		}
	}

	void FixedUpdate() {
		if (GS == null || GS.SUSPENDED) { return; } // Don't do anything if the game state is not loaded yet or suspended

		// If monster sees Toni, everything else is irrelevant
		if(GS.monsterSeesToni) {
			me.worldModel.toniKnownToBeInRoom(me.isIn);
		} else {
			// Otherwise, predict Toni's movements according to blind transition model
			me.worldModel.predictOneTimeStep();
			// And if a noise has been heard, update the model accordingly
			if(newNoiseHeard) {
				me.worldModel.filter(lastNoiseVolume, lastNoiseHeardFrom);
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
		me.worldModel.toniKnownToBeInRoom(originDoor.connectsTo.isIn);
	}

	// Updates the internal state if necessary
	private void updateState() {
		// Check the state update cooldown
		if(stateUpdateCooldown > 0) {
			stateUpdateCooldown -= Time.fixedDeltaTime;
			return;
		}
		// Update time since last kill and aggro level
		me.timeSinceLastKill += Time.fixedDeltaTime;
		me.AGGRO = 0.1f * GS.numItemsCollected + me.timeSinceLastKill / 60.0f;
		me.AGGRO += (Toni.carriedItem != null) ? me.AGGRO : 0;

		// Update AI state 
		switch(me.state) {
		// If, while searching, a definitive position is established, start stalking
		case STATE_SEARCHING:
			if(me.worldModel.certainty >= certaintyThresholdToStartStalking) {
				// Keep the monster in the stalking mode for a bit to ensure it spots the player
				stateUpdateCooldown = DOOR_TRANSITION_DURATION;
				me.state = STATE_STALKING;
				nextDoorToGoThrough = null;
			}
			break;
		// If, while stalking, monster sees Toni, start pursuing
		// But if Tonis position no longer certain, start searching again
		case STATE_STALKING:
			distanceThresholdToStartPursuing = me.AGGRO * SCREEN_SIZE_HORIZONTAL / 2; 
			if(GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < distanceThresholdToStartPursuing) {
				me.state = STATE_PURSUING;
			} else if(me.worldModel.certainty < certaintyThresholdToStartStalking) {
				me.state = STATE_SEARCHING;
			}
			break;
		// If, while pursuing, monster sees Toni is within attack range, initiate an attack
		// On the other hand, if Toni cannot be seen, start stalking again
		case STATE_PURSUING:
			if(!Toni.isInvulnerable && GS.monsterSeesToni && Math.Abs(Math.Abs(GS.distanceToToni) - ATTACK_RANGE) <= ATTACK_MARGIN) {
				me.state = STATE_ATTACKING;
				stateUpdateCooldown = ATTACK_DURATION + ATTACK_COOLDOWN;
			} else if(!GS.monsterSeesToni) {
				stateUpdateCooldown = DOOR_TRANSITION_DURATION;
				me.state = STATE_STALKING;
			}
			break;
		// If, after attacking, the monster still sees Toni, but he is out of range, start pursuing again
		// On the other hand, if Toni cannot be seen, start stalking again
		case STATE_ATTACKING:
			if(GS.monsterSeesToni && Math.Abs(Math.Abs(GS.distanceToToni) - ATTACK_RANGE) > ATTACK_MARGIN) {
				me.state = STATE_PURSUING;
			} else if(!GS.monsterSeesToni) {
				me.state = STATE_STALKING;
			}
			break;
		// Wandering is a special case: if the ritual has been performed, start fleeing from Toni
		case STATE_WANDERING:
			if(IS_CIVILIAN && GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < (SCREEN_SIZE_HORIZONTAL / 2)) {
				me.state = STATE_FLEEING;
			}
			break;
		// Stop fleeing if Toni is no longer seen
		case STATE_FLEEING:
			if(!GS.monsterSeesToni) {
				me.state = STATE_WANDERING;
			}
			break;
		// Searching is the default state
		default:
			me.state = STATE_SEARCHING;
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
			me.state = STATE_WANDERING;
			monsterImageObject.SetActive(false);
		} else {
			civilianObject.SetActive(false); // Monster-Toni not visible at first.
		}

		if (GS.CIVILIAN_KILLED) {
			GS.CIVILIAN_KILLED = false;
			dieAndRespawn();
		}

		if (me.state != STATE_WANDERING && !IS_CIVILIAN && GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < MARGIN_ITEM_STEAL) {
			Toni.control.dropItem();
		}

		// Correct state handling
		switch(me.state) {
		default:
		case STATE_SEARCHING:
			enactSearchPolicy(true);
			break;
		case STATE_PURSUING:
			enactPursuitPolicy();
			break;
		case STATE_ATTACKING:
			if(!attackAnimationPlaying) {
				StartCoroutine(playAttackAnimation(Toni.atPos, Toni));
			}
			break;
		case STATE_WANDERING:
			enactSearchPolicy(false);
			break;
		case STATE_FLEEING:
			enactFlightPolicy();
			break;
		case STATE_STALKING:
			enactStalkingPolicy();
			break;
		}
	}

	// Walk towards the neighbouring room that appears to be the closest to Toni's suspected position (in aggressive mode)
	// Or walk around randomply (in wandering mode)
	private void enactSearchPolicy(bool aggressiveSearch) {
		// Only conduct full search if no door has been selected as target yet
		if(nextDoorToGoThrough == null) {
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
							curUtility += me.worldModel.probabilityThatToniIsInRoom[room.INDEX] *
							(GS.distanceBetweenTwoRooms[me.isIn.INDEX, room.INDEX] - GS.distanceBetweenTwoRooms[targetRoomIndex, room.INDEX]);
						}
					}
				} else { // Non-aggressive search just picks doors at random
					curUtility += UnityEngine.Random.Range(0, 10f);
				}

				// Check if the new utility is higher than the one found previously
				if(curUtility > highestDoorUtility) {
					nextDoorToGoThrough = door;
					highestDoorUtility = curUtility;
				}
			}
		}
		// With the door set, move towards and through it
		walkToAndThroughDoor(nextDoorToGoThrough, false, Time.deltaTime);
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

	private void enactFlightPolicy() {
		if(nextDoorToGoThrough == null) {
			// Analyze available door options
			double highestDoorUtility = double.MinValue; double curUtility;
			foreach(Data_Door door in me.isIn.DOORS.Values) {
				// Utility is basically equal to the distance to the door, plus a random factor to avoid deadlocks
				curUtility = Math.Abs(door.visiblePos - me.atPos) + UnityEngine.Random.Range(0f, 0.1f);
				// However, having to run past Monster!Toni is penalized
				bool toniIsBetweenMeAndDoor = (door.atPos <= Toni.atPos && Toni.atPos <= me.atPos) || (door.atPos >= Toni.atPos && Toni.atPos >= me.atPos);
				curUtility -= toniIsBetweenMeAndDoor ? (double.MaxValue / 2) : 0;
				// Check if the new utility is higher than the one found previously
				if(curUtility > highestDoorUtility) {
					nextDoorToGoThrough = door;
					highestDoorUtility = curUtility;
				}
			}
		}
		// With the door set, move towards and through it at runnig pace
		walkToAndThroughDoor(nextDoorToGoThrough, true, Time.deltaTime);
	}

	// Go towards the closest room to where Toni is most likely to be
	private void enactStalkingPolicy() {
		if(nextDoorToGoThrough == null) {
			// If monster sees Toni, go through the closest door that doesn't lead you past Toni
			// This is not unlike the fleeing policy
			if(GS.monsterSeesToni) {
				double highestDoorUtility = double.MinValue;
				double curUtility;
				foreach(Data_Door door in me.isIn.DOORS.Values) {
					// Utility is basically equal to the distance to the door, plus a random factor to avoid deadlocks
					curUtility = Math.Abs(door.visiblePos - me.atPos) + UnityEngine.Random.Range(0f, 0.1f);
					// However, having to run past Toni is penalized
					bool toniIsBetweenMeAndDoor = (door.atPos <= Toni.atPos && Toni.atPos <= me.atPos) || (door.atPos >= Toni.atPos && Toni.atPos >= me.atPos);
					curUtility -= toniIsBetweenMeAndDoor ? (2.0 * (double)Math.Abs(door.visiblePos - Toni.atPos)) : 0;
					// Check if the new utility is higher than the one found previously
					if(curUtility > highestDoorUtility) {
						nextDoorToGoThrough = door;
						highestDoorUtility = curUtility;
					}
				}
			} else {
				// If you don't see Toni, find a room that is closest to you and to Toni's supposed location
				// then go through the door that'll take you to it the fastest
				float roomDistance; float bestRoomDistance = float.MaxValue; int bestRoomIndex = me.isIn.INDEX;
				for(int i = 0; i < GS.ROOMS.Count; i++) {
					roomDistance = GS.distanceBetweenTwoRooms[me.isIn.INDEX, i] + GS.distanceBetweenTwoRooms[i, me.worldModel.mostLikelyTonisRoomIndex];
					if(roomDistance < bestRoomDistance && i != me.isIn.INDEX) {
						bestRoomIndex = i;
						bestRoomDistance = roomDistance;
					}
				}
				// Now find the door that will lead you there fastest
				double highestDoorUtility = double.MinValue;
				double curUtility;
				foreach(Data_Door door in me.isIn.DOORS.Values) {
					// Utility is basically equal to the distance from monster to door,
					// plus distance from door to the target room, plus a random factor to avoid deadlocks
					curUtility = Math.Abs(door.visiblePos - me.atPos)
								+ GS.getDistance(door, new Data_Position(bestRoomIndex, 0f))
								+ UnityEngine.Random.Range(0f, 0.1f);
					// However, having to go through the ritual room is penalized
					curUtility -= (door.connectsTo.isIn.INDEX == RITUAL_ROOM_INDEX) ? (0.5 * double.MaxValue) : 0;
					// Avoid going into Toni's room, too, as long as pursuit threshold is higher than the screen size
					curUtility -= (door.connectsTo.isIn.INDEX == bestRoomIndex && distanceThresholdToStartPursuing < SCREEN_SIZE_HORIZONTAL) ? (0.2 * double.MaxValue) : 0;
					// Check if the new utility is higher than the one found previously
					if(curUtility > highestDoorUtility) {
						nextDoorToGoThrough = door;
						highestDoorUtility = curUtility;
					}
				}
			}
		}
		walkToAndThroughDoor(nextDoorToGoThrough, false, Time.deltaTime);
	}

	// Walk towards the door and through it, if possible
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

	// This function carries out the necessary adjustments to the monster's game objects
	public void setupEndgame() {
		IS_CIVILIAN = true;
		monsterImageObject.SetActive(false);
		civilianObject.SetActive(true);
		me.state = STATE_WANDERING;
		// Halve the movement speed
		RUNNING_SPEED = Global_Settings.read("MONSTER_WALKING_SPEED") / 2f;
		WALKING_SPEED = Global_Settings.read("MONSTER_SLOW_WALKING_SPEED") / 2f;
	}

	// Killing the monster / civilian during endgame
	public void dieAndRespawn() {
		Vector3 pos = transform.position;

		if (!IS_CIVILIAN) {
			Debug.Log("The monster died.");
			setupEndgame();
		} else {
			Debug.Log("A civilian died.");
		}
		// Place a tombstone where the death occured
		Instantiate(tombstone, new Vector3(pos.x, pos.y - 1.7f, pos.z), Quaternion.identity);

		// Move the civilian to a distant room
		me.updatePosition(GS.getRoomFurthestFrom(me.isIn.INDEX), 0, 0);
		me.worldModel.updateMyRoom(me.isIn, false);
		nextDoorToGoThrough = null;
		// Move the character sprite directly to where the game state says it should be standing
		Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);
	}

	// Superclass functions implemented
	protected override void setSpriteFlip(bool state) {
		monsterRenderer.flipX = !state;
		civilianRenderer.flipX = state;
	}
	protected override bool canRun() {
		return true; // The monster can always run
	}
	// Instead of updating statistics, the monster updates its position in the world model and checks whether it sees Toni
	protected override void updateDoorUsageStatistic(Data_Door door, Data_Room currentRoom, Data_Door destinationDoor, Data_Room destinationRoom) {
		me.worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
		nextDoorToGoThrough = null;
		previousRoomVisited = currentRoom;
	}
	// If hit as a civiilian, die
	public override void getHit() {
		if(IS_CIVILIAN) {
			dieAndRespawn();
		}
	}
	// Reset the kill time upon kill
	protected override void postKillHook() {
		me.timeSinceLastKill = 0;
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
