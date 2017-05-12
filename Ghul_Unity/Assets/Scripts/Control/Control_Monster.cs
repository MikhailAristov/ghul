using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_Monster : Control_Character {

	// Thresholds for animation transitions.
	public float ANIM_MIN_SPEED_FOR_WALKING = 0.01f;

	[NonSerialized]
	private Data_Monster me;

	protected override Data_Character getMe() {
		return me as Data_Character;
	}

	[NonSerialized]
	private Data_PlayerCharacter Toni;

	private float MARGIN_DOOR_ENTRANCE;
	private float HALF_SCREEN_SIZE_HORIZONTAL;
	private float MARGIN_ITEM_STEAL;
	private float WAIT_FOR_TONI_TO_MOVE;

	private float EFFECTIVE_MINIMUM_ATTACK_RANGE; // For prediction and planning of attacks
	private float EFFECTIVE_ATTACK_MARGIN; // Ditto

	// Noise data
	private bool newNoiseHeard;
	private Data_Door lastNoiseHeardFrom;
	private float lastNoiseVolume;

	// Monster AI states
	public const int STATE_WANDERING = 0;
	public const int STATE_SEARCHING = 1;
	public const int STATE_STALKING = 2;
	public const int STATE_PURSUING = 3;
	// public const int STATE_ATTACKING = 4; // The attacking state is merged into STATE_PURSUING
	public const int STATE_FLEEING = 5;

	private const double utilityPenaltyRitualRoom = double.MaxValue / 2;
	private const double utilityPenaltyPreviousRoom = double.MaxValue / 4;
	private const double utilityPenaltyTonisRoom = double.MaxValue / 8;
	private const double utilityPenaltyToniInTheWay = double.MaxValue / 4;

	private float stateUpdateCooldown;
	private float distanceThresholdToStartPursuing; // = AGGRO * screen width / 2
	private double certaintyThresholdToStartStalking;
	private float cumultativeImpasseDuration;

	[NonSerialized]
	public Data_Door nextDoorToGoThrough;
	private Data_Room previousRoomVisited; // This can be used to prevent endless door walk cycles

	// prefab, to be placed for each death
	public Transform tombstone;
	public GameObject attackArm;

	// Graphics parameters
	private GameObject monsterImageObject;
	private SpriteRenderer monsterRenderer;
	private GameObject civilianObject;
	private SpriteRenderer civilianRenderer;

	// Animator for transitioning between animation states
	private Animator animatorMonster;
	private Animator animatorCivilian;
	private Transform monsterImageTransform; // For sprite offset during attack
	private bool animatorStateAttack;
	private const float ATTACK_SPRITE_OFFSET = 1.75f;

	void Start() {
		monsterImageObject = GameObject.Find("MonsterImage");
		monsterRenderer = monsterImageObject.GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		civilianObject = GameObject.Find("StickmanCivilian");
		civilianRenderer = civilianObject.GetComponent<SpriteRenderer>();
		civilianObject.SetActive(false); // Civ-Monster not visible at first.
		attackArmRenderer = attackArm.GetComponent<LineRenderer>();

		// Setting the animator
		if (monsterImageObject != null) {
			animatorMonster = monsterImageObject.GetComponent<Animator>();
		}
		if (civilianObject != null) {
			animatorCivilian = civilianObject.GetComponent<Animator>();
		}
		animatorStateAttack = false;
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState) {
		this.GS = gameState;
		this.me = gameState.getMonster();
		this.Toni = gameState.getToni();
		this.currentEnvironment = me.isIn.env;

		RUNNING_SPEED = Global_Settings.read("MONSTER_WALKING_SPEED");
		WALKING_SPEED = Global_Settings.read("MONSTER_SLOW_WALKING_SPEED");
		MARGIN_DOOR_ENTRANCE = Global_Settings.read("MARGIN_DOOR_ENTRANCE");
		HALF_SCREEN_SIZE_HORIZONTAL = Global_Settings.read("SCREEN_SIZE_HORIZONTAL") / 2;

		ATTACK_RANGE = Global_Settings.read("MONSTER_ATTACK_RANGE");
		ATTACK_MARGIN = Global_Settings.read("MONSTER_ATTACK_MARGIN");
		ATTACK_DURATION = Global_Settings.read("MONSTER_ATTACK_DURATION");
		ATTACK_COOLDOWN = Global_Settings.read("MONSTER_ATTACK_COOLDOWN");

		EFFECTIVE_ATTACK_MARGIN = ATTACK_MARGIN / 2;
		EFFECTIVE_MINIMUM_ATTACK_RANGE = ATTACK_RANGE - EFFECTIVE_ATTACK_MARGIN;

		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");
		RITUAL_ROOM_INDEX = (int)Global_Settings.read("RITUAL_ROOM_INDEX");
		MARGIN_ITEM_STEAL = Global_Settings.read("MARGIN_ITEM_COLLECT") / 10f;
		WAIT_FOR_TONI_TO_MOVE = Global_Settings.read("MONSTER_WAIT_FOR_TONI_MOVE");

		// Move the character sprite directly to where the game state says it should be standing
		Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);

		// Artificial intelligence
		me.worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
		StartCoroutine(displayWorldState(1f / 60f));
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

	protected new void FixedUpdate() {
		base.FixedUpdate();

		if(GS == null || GS.SUSPENDED) {
			return;
		} // Don't do anything if the game state is not loaded yet or suspended

		// If monster sees Toni, do not update the world model
		if(GS.monsterSeesToni) {
			me.worldModel.toniKnownToBeInRoom(me.isIn);
			// Do update the time both Toni and monster have been standing still, though
			if(Math.Abs(Toni.currentVelocity) < 0.1f && Math.Abs(Toni.atPos - me.atPos) < EFFECTIVE_MINIMUM_ATTACK_RANGE) {
				cumultativeImpasseDuration += Time.fixedDeltaTime;
			} else {
				cumultativeImpasseDuration = 0;
			}
		} else {
			// Otherwise, predict Toni's movements according to blind transition model
			me.worldModel.predictOneTimeStep();
			// And if a noise has been heard, update the model accordingly
			if(newNoiseHeard) {
				me.worldModel.filter(lastNoiseVolume, lastNoiseHeardFrom);
				newNoiseHeard = false;
			} else {
				// If no noise has been heard, filter anyway
				me.worldModel.filterWithNullSignal();
			}
			// Just to clean things up...
			if(cumultativeImpasseDuration > 0) {
				cumultativeImpasseDuration = 0;
			}
		}

		// Handling the animation
		if (GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			// Transition for walking / attacking
			if (animatorMonster != null) {
				if (Math.Abs (getMe ().currentVelocity) < ANIM_MIN_SPEED_FOR_WALKING) {
					animatorMonster.SetBool ("Is Walking", false);
				} else if (Math.Abs (getMe ().currentVelocity) >= ANIM_MIN_SPEED_FOR_WALKING) {
					animatorMonster.SetBool ("Is Walking", true);
				}
			}
		} else if (GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_PHASE) {
			// Transition for walking
			if (animatorCivilian != null) {
				if (Math.Abs (getMe ().currentVelocity) < ANIM_MIN_SPEED_FOR_WALKING) {
					animatorCivilian.SetBool ("Is Walking", false);
				} else if (Math.Abs (getMe ().currentVelocity) >= ANIM_MIN_SPEED_FOR_WALKING) {
					animatorCivilian.SetBool ("Is Walking", true);
				}
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
		me.AGGRO = 0.1f * GS.numItemsPlaced + me.timeSinceLastKill / 60.0f;
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
		// But if Toni's position no longer certain, start searching again
		case STATE_STALKING:
			distanceThresholdToStartPursuing = me.AGGRO * HALF_SCREEN_SIZE_HORIZONTAL; 
			if(GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < distanceThresholdToStartPursuing) {
				me.state = STATE_PURSUING;
			} else if(me.worldModel.certainty < certaintyThresholdToStartStalking) {
				me.state = STATE_SEARCHING;
			}
			break;
		// If, while pursuing, monster sees Toni is within attack range, initiate an attack
		// On the other hand, if Toni cannot be seen, start stalking again
		case STATE_PURSUING:
			if(GS.monsterSeesToni) {
				if(!attackAnimationPlaying) {
					// If you see Toni, predict where he will when the attack animation completes...
					float tonisPredictedPosition = Toni.atPos + ATTACK_DURATION * Toni.currentVelocity;
					// ...as well as where the attack his his direction would land
					float attackLandingPoint = me.atPos + Math.Sign(Toni.atPos - me.atPos) * ATTACK_RANGE;
					// If Toni's predicted position and the attack landing point are within the attack margin (hit box), initiate attack
					if(!Toni.isInvulnerable && Math.Abs(tonisPredictedPosition - attackLandingPoint) < EFFECTIVE_ATTACK_MARGIN) {
						StartCoroutine(playAttackAnimation(Toni.atPos, Toni));
						stateUpdateCooldown = ATTACK_DURATION + ATTACK_COOLDOWN;
					}

					// Activate the attack animation. Note, that the monster sprite needs to be moved momentarily since it's off center.
					animatorMonster.SetTrigger("Attack");
					/*
					if (monsterImageTransform == null) {
						monsterImageTransform = animatorMonster.gameObject.transform;
					}
					if (animatorMonster.gameObject.GetComponent<SpriteRenderer> ().flipX) {
						monsterImageTransform.Translate(new Vector3(ATTACK_SPRITE_OFFSET, 0.0f, 0.0f));
					} else {
						monsterImageTransform.Translate(new Vector3((-1) * ATTACK_SPRITE_OFFSET, 0.0f, 0.0f));
					}
					*/
				}
				// Otherwise, keep pursuing
			} else {
				// If you don't see Toni anymore, go back to stalking
				stateUpdateCooldown = DOOR_TRANSITION_DURATION;
				me.state = STATE_STALKING;
			}
			break;
		// Wandering is a special case: if the ritual has been performed, start fleeing from Toni
		case STATE_WANDERING:
			if(GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_PHASE &&
			   GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < HALF_SCREEN_SIZE_HORIZONTAL) {
				me.state = STATE_FLEEING;
			}
			break;
		// Stop fleeing if Toni is no longer seen
		case STATE_FLEEING:
			if(!GS.monsterSeesToni || GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_DEAD) {
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
	void Update() {
		if(GS == null || GS.SUSPENDED) {
			return;
		} // Don't do anything if the game state is not loaded yet or suspended
		if(me.etherialCooldown > 0.0f) { // While the character is etherial, don't do anything
			me.etherialCooldown -= Time.deltaTime;
			return;
		}

		// If the endgame has been triggered, but the monster has not yet been killed,
		// teleport into the ritual room and stand there passively
		if(GS.OVERALL_STATE == Control_GameState.STATE_TRANSFORMATION && !GS.MONSTER_KILLED) {
			if(me.isIn.INDEX != RITUAL_ROOM_INDEX) {
				teleportToRitualRoom();
			} else {
				setSpriteFlip(me.atPos > Toni.atPos);
				return;
			}
		}

		// During the collection phase of the game, make Toni drop his carried items if he gets too close
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE && GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < MARGIN_ITEM_STEAL) {
			Toni.control.dropItem();
		}

		/*
		if (GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_PHASE || GS.OVERALL_STATE == Control_GameState.STATE_TRANSFORMATION) {
			// Checking whether attack is over and moving the sprite if that is the case.
			if (animatorMonster.GetCurrentAnimatorStateInfo(0).IsName("Monster_attack") && !animatorStateAttack) {
				animatorStateAttack = true;
			}
			if (animatorMonster.GetCurrentAnimatorStateInfo(0).IsName("Monster_idle") && animatorStateAttack) {
				animatorStateAttack = false;
				if (animatorMonster.gameObject.GetComponent<SpriteRenderer> ().flipX) {
					monsterImageTransform.Translate(new Vector3((-1) * ATTACK_SPRITE_OFFSET, 0.0f, 0.0f));
				} else {
					monsterImageTransform.Translate(new Vector3(ATTACK_SPRITE_OFFSET, 0.0f, 0.0f));
				}
			}
		}
		*/

		try {
			// Correct state handling
			switch(me.state) {
			default:
			case STATE_SEARCHING:
				enactSearchPolicy(true);
				break;
			case STATE_PURSUING:
				if(!attackAnimationPlaying) {
					enactPursuitPolicy();
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
		} catch(NullReferenceException e) {
			// If a null reference exception is thrown here, it's most likely because the world model is borked and needs a soft reset
			Debug.LogException(e);
			me.worldModel.softReset();
		}

	}

	/* Assigns a utility score to each currently visible door and returns the door with the highest utility
	 * 
	 *  aggressive :			if specified, a door's utility increases, the closer it takes the monster to Toni
	 * 	localDistanceFactor :	the higher the factor, the more the monster will prefer closer doors over further ones
	 * 	ritualRoomPenalty :		flat utility penalty for going into the ritual room (should normally be very high)
	 * 	previousRoomPenalty :	flat penalty for returning to the room where the monster came from
	 *  meetingToniPenalty :	flat penalty on entering the room where Toni is most likely to be
	 *  toniInTheWayPenalty :	flat penalty for running past Toni to reach that door
	*/
	private Data_Door findNextDoorToVisit(bool aggressive, double localDistanceFactor, double ritualRoomPenalty, double previousRoomPenalty, double meetingToniPenalty, double toniInTheWayPenalty) {
		Data_Door result = null;
		// Analyze available door options
		double highestDoorUtility = double.MinValue;
		double curUtility;
		foreach(Data_Door door in me.isIn.DOORS.Values) {
			int targetRoomIndex = door.connectsTo.isIn.INDEX;

			// Initialize door utility with a random value to avoid deadlocks
			curUtility = UnityEngine.Random.Range(0, 10f);

			// Apply common door utility adjustments:
			// -> door leads to the ritual room
			curUtility -= (targetRoomIndex == RITUAL_ROOM_INDEX && GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) ? ritualRoomPenalty : 0;
			// -> door leads back to where monster came from
			curUtility -= (targetRoomIndex == previousRoomVisited.INDEX) ? previousRoomPenalty : 0;
			// -> door leads to the room where Toni is most likely to be
			curUtility -= (targetRoomIndex == me.worldModel.mostLikelyTonisRoomIndex) ? meetingToniPenalty : 0;
			// -> Toni stands between monster and the door
			bool toniIsInTheWay = GS.monsterSeesToni && ((door.atPos <= Toni.atPos && Toni.atPos <= me.atPos) || (door.atPos >= Toni.atPos && Toni.atPos >= me.atPos));
			curUtility -= toniIsInTheWay ? toniInTheWayPenalty : 0;
			// -> distance to door (relative to room width)
			curUtility -= localDistanceFactor * (me.isIn.width - Math.Abs(me.atPos - door.atPos)) / me.isIn.width;

			// If aggressive search wanted, take the door that brings you closest to Toni
			if(aggressive) {
				foreach(Data_Room room in GS.ROOMS.Values) {
					curUtility += me.worldModel.probabilityThatToniIsInRoom[room.INDEX] *
					(GS.distanceBetweenTwoRooms[me.isIn.INDEX, room.INDEX] - GS.distanceBetweenTwoRooms[targetRoomIndex, room.INDEX]);
				}
			}

			// Check if the new utility is higher than the one found previously
			if(curUtility > highestDoorUtility) {
				result = door;
				highestDoorUtility = curUtility;
			}
		}
		Debug.Assert(result != null);
		return result;
	}

	// Walk towards the neighbouring room that appears to be the closest to Toni's suspected position (in aggressive mode)
	// Or walk around randomply (in wandering mode)
	private void enactSearchPolicy(bool aggressiveSearch) {
		if(nextDoorToGoThrough == null) {
			if(aggressiveSearch) {
				// Penalties: ritual room > previous room > going past Toni > distance to the door > Toni's room
				nextDoorToGoThrough = findNextDoorToVisit(true, 0.1, utilityPenaltyRitualRoom, utilityPenaltyPreviousRoom, 0, utilityPenaltyToniInTheWay / 2);
			} else {
				double fearOfToni = (utilityPenaltyTonisRoom + utilityPenaltyToniInTheWay) / 2;
				// Penalties: fear of Toni > previous room > > distance to the door > ritual room
				nextDoorToGoThrough = findNextDoorToVisit(false, 0.1, 0, utilityPenaltyPreviousRoom, fearOfToni, fearOfToni);
			}
		}
		// With the door set, move towards and through it
		walkToAndThroughDoor(nextDoorToGoThrough, false, Time.deltaTime);
	}

	// Run towards the nearest door away from Toni
	private void enactFlightPolicy() {
		if(nextDoorToGoThrough == null) {
			// Penalties: going past Toni > distance to door > Toni's room > ritual room = previous room
			nextDoorToGoThrough = findNextDoorToVisit(false, 100.0, 0, 0, utilityPenaltyTonisRoom, utilityPenaltyToniInTheWay);
		}
		// With the door set, move towards and through it at runnig pace
		walkToAndThroughDoor(nextDoorToGoThrough, true, Time.deltaTime);
	}

	// Go towards the closest room to where Toni is most likely to be
	private void enactStalkingPolicy() {
		if(nextDoorToGoThrough == null) {
			// If monster sees Toni, go through the closest door that doesn't lead you past Toni
			// This is not unlike the fleeing policy, but ritual room is still off-limits
			if(GS.monsterSeesToni) {
				// Penalties: ritual room > going past Toni > distance to door > Toni's room = previous room
				nextDoorToGoThrough = findNextDoorToVisit(false, 10.0, utilityPenaltyRitualRoom, 0, 0, utilityPenaltyToniInTheWay);
			} else {
				// Penalize going into Toni's supposed room until the aggro is high enough to engage right away
				double meetingToniPenalty = (distanceThresholdToStartPursuing >= HALF_SCREEN_SIZE_HORIZONTAL) ? 0 : utilityPenaltyTonisRoom;
				// Penalties: ritual room > Toni's room (opt.) > distance to door > previous room = going past Toni
				nextDoorToGoThrough = findNextDoorToVisit(true, 10.0, utilityPenaltyRitualRoom, 0, meetingToniPenalty, 0);
			}
		}
		walkToAndThroughDoor(nextDoorToGoThrough, false, Time.deltaTime);
	}

	// Run towards within striking range of Toni
	private void enactPursuitPolicy() {
		// If Toni is still inside the striking range, just turn towards him and wait for his move
		float distToToni = GS.distanceToToni;
		if(Math.Abs(distToToni) < EFFECTIVE_MINIMUM_ATTACK_RANGE) {
			// However, if Toni has been standing still for some time, find the nearest attack position and move towards it
			if(cumultativeImpasseDuration > WAIT_FOR_TONI_TO_MOVE) {
				walk(getNearestAttackVector(), true, Time.deltaTime);
			} else {
				setSpriteFlip(distToToni < 0);
			}
		} else {
			// Otherwise, run towards him
			walk(distToToni, true, Time.deltaTime);
		}
	}

	// Returns the horizontal distance (positive or negative) to the nearest valid attack position against Toni
	private float getNearestAttackVector() {
		if(GS.monsterSeesToni) {
			// Calculate the both possible attack points
			float leftAttackVector = Toni.atPos - ATTACK_RANGE - me.atPos;
			float rightAttackVector = Toni.atPos + ATTACK_RANGE - me.atPos;
			// If the left attack point is valid and closer to my current position than the right one, return it
			if(Math.Abs(leftAttackVector) < Math.Abs(rightAttackVector) &&  (me.atPos + leftAttackVector) > me.isIn.leftWalkBoundary) {
				return leftAttackVector;
			}
			// Ditto for the right one
			if(Math.Abs(leftAttackVector) >= Math.Abs(rightAttackVector) && (me.atPos + rightAttackVector) < me.isIn.rightWalkBoundary) {
				return rightAttackVector;
			}
			// Default case: run towards Toni
			return (Toni.atPos - me.atPos);
		} else {
			return 0;
		}
	}

	// Walk towards the door and through it, if possible
	private void walkToAndThroughDoor(Data_Door door, bool run, float deltaTime) {
		Debug.Assert(door != null);
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
		monsterImageObject.SetActive(false);
		civilianObject.SetActive(true);
		me.state = STATE_WANDERING;
		// Halve the movement speed
		RUNNING_SPEED = Global_Settings.read("MONSTER_WALKING_SPEED") / 2f;
		WALKING_SPEED = Global_Settings.read("MONSTER_SLOW_WALKING_SPEED") / 2f;
	}

	private void teleportToRitualRoom() {
		// Find the door furthest removed from the pentagram
		Data_Position pentagramPos = new Data_Position(RITUAL_ROOM_INDEX, Global_Settings.read("RITUAL_PENTAGRAM_CENTER"));
		Data_Door targetDoor = null;
		float distToPentagram = 0;
		foreach(Data_Door d in GS.getRoomByIndex(RITUAL_ROOM_INDEX).DOORS.Values) {
			float newDistance = GS.getDistance(d, pentagramPos);
			if(newDistance > distToPentagram) {
				targetDoor = d.connectsTo;
				distToPentagram = newDistance;
			}
		}
		// Set AI state to fleeing, just in case
		me.state = STATE_FLEEING;
		// "Go" through the door
		StartCoroutine(goThroughTheDoor(targetDoor));
	}

	// Killing the monster / civilian during endgame
	public void dieAndRespawn() {
		GS.MONSTER_KILLED = true;

		Vector3 pos = transform.position;
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

	protected override void postDoorTransitionHook(Data_Door doorTaken) {
		me.worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
		nextDoorToGoThrough = null;
		previousRoomVisited = doorTaken.isIn;
	}

	// If hit after the ritual was performed, die
	public override void getHit() {
		Debug.Log(me + " is hit");
		if(GS.OVERALL_STATE > Control_GameState.STATE_COLLECTION_PHASE) {
			dieAndRespawn();
		}
	}
	// Reset the kill time upon kill
	protected override void postKillHook() {
		me.timeSinceLastKill = 0;
		// Extend the time the monster stands still after killing Toni (while the house is being rebuilt)
		me.etherialCooldown = Global_Settings.read("TOTAL_DEATH_DURATION");
	}
	protected override void activateCooldown(float duration) {
		me.etherialCooldown = duration;
	}
	// The rest stays empty for now (only relevant for Toni)...
	protected override void updateStamina(bool isRunning) {}
	protected override void regainStamina() {}
	protected override void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos) {}
	protected override void preDoorTransitionHook(Data_Door doorTaken) {}
	protected override void preRoomLeavingHook(Data_Door doorTaken) {}
}
