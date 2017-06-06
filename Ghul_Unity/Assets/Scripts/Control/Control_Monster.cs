using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_Monster : Control_Character {

	// The debugging switch to make the monster non-aggressive
	public bool MOSTLY_HARMLESS;

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

	// For prediction and planning of attacks
	private float EFFECTIVE_MINIMUM_ATTACK_RANGE;
	private float EFFECTIVE_ATTACK_MARGIN;

	// Noise data
	private bool newNoiseHeard;
	private Data_Door lastNoiseHeardFrom;
	private float lastNoiseVolume;

	// Monster AI states
	public const int STATE_WANDERING = 0;
	public const int STATE_SEARCHING = 1;
	public const int STATE_STALKING = 2;
	public const int STATE_PURSUING = 3;
	public const int STATE_FLEEING = 5;

	private const double utilityPenaltyRitualRoom = double.MaxValue / 2;
	private const double utilityPenaltyPreviousRoom = double.MaxValue / 4;
	private const double utilityPenaltyTonisRoom = double.MaxValue / 8;
	private const double utilityPenaltyToniInTheWay = double.MaxValue / 4;

	private float stateUpdateCooldown;
	private float distanceThresholdToStartPursuing {
		get { return me.AGGRO * HALF_SCREEN_SIZE_HORIZONTAL; }
		set { return; }
	}
	private double certaintyThresholdToStartStalking;
	private float cumultativeImpasseDuration;

	[NonSerialized]
	public Data_Door nextDoorToGoThrough;
	[NonSerialized]
	public Data_Door doorToLurkAt;
	// This can be used to prevent endless door walk cycles:
	private Data_Room previousRoomVisited;

	// prefab, to be placed for each death
	public Transform tombstone;
	public GameObject AttackZone;

	// Graphics parameters
	private GameObject monsterImageObject;
	private SpriteRenderer monsterRenderer;
	private GameObject civilianObject;
	private SpriteRenderer civilianRenderer;

	// Animator for transitioning between animation states
	private Animator animatorMonster;
	private Animator animatorCivilian;

	// Invisibility controls
	private const float maxVisibility = 0.9f;
	private const float minVisibility = 0.1f;
	private int INVISIBLE_AFTER_ITEM;
	private float INVISIBILITY_TRANSITION_DURATION;
	private bool currentlyInvisible {
		get { return (GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE && GS.numItemsPlaced >= INVISIBLE_AFTER_ITEM && !attackAnimationPlaying); }
		set { return; }
	}
	private float visibilityTransitionSpeed = 1f;

	void Awake() {
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

		INVISIBLE_AFTER_ITEM = (int)Global_Settings.read("MONSTER_INVISIBLE_AFTER_ITEM");
		INVISIBILITY_TRANSITION_DURATION = Global_Settings.read("MONSTER_INVISIBILIY_TRANSITION");
	}

	void Start() {
		monsterImageObject = GameObject.Find("MonsterImage");
		monsterRenderer = monsterImageObject.GetComponent<SpriteRenderer>();

		// Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		civilianObject = GameObject.Find("StickmanCivilian");
		civilianRenderer = civilianObject.GetComponent<SpriteRenderer>();
		civilianObject.SetActive(false); // Civ-Monster not visible at first.

		// Setting the animator
		if(monsterImageObject != null) {
			animatorMonster = monsterImageObject.GetComponent<Animator>();
		}
		if(civilianObject != null) {
			animatorCivilian = civilianObject.GetComponent<Animator>();
		}
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState) {
		this.GS = gameState;
		this.me = gameState.getMonster();
		this.Toni = gameState.getToni();
		this.currentEnvironment = me.isIn.env;

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
				if(AttackZone.activeSelf && Debug.isDebugBuild) {
					AttackZone.transform.localScale = 
						new Vector3(distanceThresholdToStartPursuing, AttackZone.transform.localScale.y, AttackZone.transform.localScale.z);
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
			if(Toni.currentVelocityAbsolute < ANIM_MIN_SPEED_FOR_WALKING && Math.Abs(Toni.atPos - me.atPos) < EFFECTIVE_MINIMUM_ATTACK_RANGE) {
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
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			// Transition for walking / attacking
			if(animatorMonster != null) {
				if(me.currentVelocityAbsolute < ANIM_MIN_SPEED_FOR_WALKING) {
					animatorMonster.SetBool("Is Walking", false);
				} else if(me.currentVelocityAbsolute >= ANIM_MIN_SPEED_FOR_WALKING) {
					animatorMonster.SetBool("Is Walking", true);
				}
			}
		} else if(GS.OVERALL_STATE == Control_GameState.STATE_MONSTER_PHASE) {
			// Transition for walking
			if(animatorCivilian != null) {
				if(me.currentVelocityAbsolute < ANIM_MIN_SPEED_FOR_WALKING) {
					animatorCivilian.SetBool("Is Walking", false);
				} else if(me.currentVelocityAbsolute >= ANIM_MIN_SPEED_FOR_WALKING) {
					animatorCivilian.SetBool("Is Walking", true);
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
		// Check the harmless flag
		if(Debug.isDebugBuild && MOSTLY_HARMLESS && GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			me.state = STATE_WANDERING;
			return;
		} else if(!MOSTLY_HARMLESS && GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE && me.state == STATE_WANDERING) {
			me.state = STATE_SEARCHING;
		}

		// Check the state update cooldown
		if(stateUpdateCooldown > 0) {
			stateUpdateCooldown -= Time.fixedDeltaTime;
			return;
		}
		// Update time since last kill and aggro level
		me.timeSinceLastKill += Time.fixedDeltaTime;
		me.AGGRO = 0.1f * GS.numItemsPlaced + me.timeSinceLastKill / 60.0f;
		me.AGGRO += (Toni.carriedItem != null) ? me.AGGRO : 0;

		// Update AI state; cycle through the conditions several times, if necessary
		int previousState = me.state, iterationCounter = 0; 
		do {
			previousState = me.state;
			switch(previousState) {
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
				if(GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < distanceThresholdToStartPursuing) {
					me.state = STATE_PURSUING;
				} else if(GS.separationBetweenTwoRooms[me.pos.RoomId, me.worldModel.mostLikelyTonisRoomIndex] > 1 &&
					me.worldModel.certainty < certaintyThresholdToStartStalking) {
					me.state = STATE_SEARCHING;
				}
				break;
			// If, while pursuing, monster sees Toni is within attack range, initiate an attack
			// On the other hand, if Toni cannot be seen, start stalking again
			case STATE_PURSUING:
				if(GS.monsterSeesToni) {
					if(!attackAnimationPlaying) {
						// If you see Toni, predict where he will when the attack animation completes...
						float tonisPredictedPosition = Toni.atPos + ATTACK_DURATION * Toni.currentVelocitySigned;
						// ...as well as where the attack his his direction would land
						float attackLandingPoint = me.atPos + Math.Sign(Toni.atPos - me.atPos) * ATTACK_RANGE;
						// If Toni's predicted position and the attack landing point are within the attack margin (hit box), initiate attack
						if(!Toni.isInvulnerable && Math.Abs(tonisPredictedPosition - attackLandingPoint) < EFFECTIVE_ATTACK_MARGIN) {
							StartCoroutine(playAttackAnimation(Toni.atPos, Toni));
							stateUpdateCooldown = ATTACK_DURATION + ATTACK_COOLDOWN;
						}
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
			// Sanity check
			if(iterationCounter++ > 10) {
				Debug.LogError("Monster state transition limit exceeded! Last two states: " + previousState + ", " + me.state);
				break;
			}
		} while(previousState != me.state);
	}

	// Update is called once per frame
	void Update() {
		// Don't do anything if the game state is not loaded yet or suspended
		if(GS == null || GS.SUSPENDED) {
			animatorMonster.speed = 0;
			animatorCivilian.speed = 0;
			return;
		} else {
			animatorMonster.speed = 1f;
			animatorCivilian.speed = 1f;
		}

		// Control visibility
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE && GS.numItemsPlaced >= INVISIBLE_AFTER_ITEM) {
			if(currentlyInvisible && monsterRenderer.color.a > minVisibility) {
				float fade = Mathf.SmoothDamp(monsterRenderer.color.a, minVisibility, ref visibilityTransitionSpeed, INVISIBILITY_TRANSITION_DURATION);
				monsterRenderer.color = new Color(1f, 1f, 1f, fade);
			} else if(!currentlyInvisible && monsterRenderer.color.a < maxVisibility){
				float fade = Mathf.SmoothDamp(monsterRenderer.color.a, maxVisibility, ref visibilityTransitionSpeed, INVISIBILITY_TRANSITION_DURATION);
				monsterRenderer.color = new Color(1f, 1f, 1f, fade);
			}
		}

		// While the character is etherial, don't do anything
		if(me.etherialCooldown > 0.0f) { 
			me.etherialCooldown -= Time.deltaTime;
			return;
		}

		// If the endgame has been triggered, but the monster has not yet been killed,
		// teleport into the ritual room and stand there passively
		if(GS.OVERALL_STATE == Control_GameState.STATE_TRANSFORMATION && !GS.MONSTER_KILLED) {
			if(me.isIn.INDEX != RITUAL_ROOM_INDEX) {
				monsterRenderer.color = Color.white;
				teleportToRitualRoom();
			} else {
				setSpriteFlip(me.atPos > Toni.atPos);
				return;
			}
		}

		// During the collection phase of the game, make Toni drop his carried items if he gets too close
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE && GS.monsterSeesToni && Math.Abs(GS.distanceToToni) < MARGIN_ITEM_STEAL
			&& !(MOSTLY_HARMLESS && Debug.isDebugBuild)) {
			Toni.control.dropItem();
		}

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
		// If monster sees Toni, stare at him until he either leaves or comes withing striking range
		if(GS.monsterSeesToni) {
			nextDoorToGoThrough = null;
			doorToLurkAt = null;
			setSpriteFlip(Toni.atPos < me.atPos);
			return;
		} else if(GS.separationBetweenTwoRooms[me.pos.RoomId, me.worldModel.mostLikelyTonisRoomIndex] == 1) {
			// If the monster cannot see Toni, but believes him to be in an adjacent room,
			// take position next to the door leading to that room, ready to strike
			if(doorToLurkAt == null || doorToLurkAt.isIn != me.isIn) {
				foreach(Data_Door d in me.isIn.DOORS.Values) {
					if(d.connectsTo.isIn.INDEX == me.worldModel.mostLikelyTonisRoomIndex) {
						doorToLurkAt = d;
						break;
					}
				}
			}
			// Walk towards the door, if found
			if(doorToLurkAt != null) {
				float attackVector = getNearestAttackVector(doorToLurkAt.atPos);
				if(attackVector != 0) {
					walk(attackVector, true, Time.deltaTime);
					return;
				}
			}
		} else {
			doorToLurkAt = null;
		}

		// Per default, look for the next door to go through
		if(nextDoorToGoThrough == null) {
			// Penalize going into Toni's supposed room until the aggro is high enough to engage right away
			double meetingToniPenalty = (distanceThresholdToStartPursuing >= HALF_SCREEN_SIZE_HORIZONTAL) ? 0 : utilityPenaltyTonisRoom;
			// Penalties: ritual room > Toni's room (opt.) > distance to door > previous room = going past Toni
			nextDoorToGoThrough = findNextDoorToVisit(true, 10.0, utilityPenaltyRitualRoom, 0, meetingToniPenalty, 0);
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
				float attackVector = getNearestAttackVector(Toni.atPos);
				if(attackVector != 0) {
					walk(attackVector, true, Time.deltaTime);
				} else {
					// ...unless there aren't any valid positions to attack from in this room!
					setSpriteFlip(distToToni < 0);
				}
			} else {
				setSpriteFlip(distToToni < 0);
			}
		} else {
			// Otherwise, run towards him
			walk(distToToni, true, Time.deltaTime);
		}
	}

	// Returns the horizontal distance (positive or negative) to the nearest valid attack position against Toni
	private float getNearestAttackVector(float targetPos) {
		// Calculate the both possible attack points
		float leftAttackVector = targetPos - ATTACK_RANGE - me.atPos;
		float rightAttackVector = targetPos + ATTACK_RANGE - me.atPos;
		// If the left attack point is valid and closer to my current position than the right one, return it
		if(Math.Abs(leftAttackVector) < Math.Abs(rightAttackVector) && (me.atPos + leftAttackVector) > me.isIn.leftWalkBoundary) {
			return leftAttackVector;
		}
		// Ditto for the right one
		if(Math.Abs(leftAttackVector) >= Math.Abs(rightAttackVector) && (me.atPos + rightAttackVector) < me.isIn.rightWalkBoundary) {
			return rightAttackVector;
		}
		// Default case: wait for Toni to move
		return 0;
	}

	// Walk towards the door and through it, if possible
	private void walkToAndThroughDoor(Data_Door door, bool run, float deltaTime) {
		Debug.Assert(door != null);
		if(door != null) {
			float distToDoor = door.visiblePos - me.atPos;
			if(Math.Abs(distToDoor) <= MARGIN_DOOR_ENTRANCE) {
				goingThroughADoor = true;
				StartCoroutine(goThroughTheDoor(door));
			} else {
				Data_Door triggeredDoor = walk(distToDoor, run, deltaTime);
				if(triggeredDoor == door) {
					goingThroughADoor = true;
					StartCoroutine(goThroughTheDoor(triggeredDoor));
				}
			}
		}
	}

	// This function carries out the necessary adjustments to the monster's game objects
	public void setupEndgame() {
		monsterImageObject.SetActive(false);
		civilianObject.SetActive(true);
		me.state = STATE_WANDERING;
		// Change the movement speed to human
		WALKING_SPEED = Global_Settings.read("CHARA_WALKING_SPEED");
		// Since the intruders still have infinite stamina, don't let them run at full speed
		RUNNING_SPEED = Global_Settings.read("CHARA_WALKING_SPEED");
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
		goingThroughADoor = true;
		StartCoroutine(goThroughTheDoor(targetDoor));
	}

	// Killing the monster / civilian during endgame
	public void dieAndRespawn() {
		Vector3 pos = transform.position;
		// Place a tombstone where the death occured
		Instantiate(tombstone, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity);

		// Move the civilian to a distant room
		me.updatePosition(GS.getRoomFurthestFrom(me.isIn.INDEX), 0, 0);
		me.worldModel.updateMyRoom(me.isIn, false);
		nextDoorToGoThrough = null;
		// Move the character sprite directly to where the game state says it should be standing
		Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(savedPosition - transform.position);

		GS.MONSTER_KILLED = true;
	}

	// Superclass functions implemented
	public override void setSpriteFlip(bool state) {
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
	// Attacking animation triggers
	protected override void startAttackAnimation() {
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			// Activate the attack animation
			animatorMonster.SetTrigger("Attack");
		}
	}
	protected override void stopAttackAnimation() {
		// Cancel the animation
		animatorMonster.SetTrigger("AttackCancel");
	}
	// Reset the kill time upon kill
	protected override void postKillHook() {
		me.timeSinceLastKill = 0;
		// Extend the time the monster stands still after killing Toni (while the house is being rebuilt)
		me.etherialCooldown = Global_Settings.read("TOTAL_DEATH_DURATION");
	}

	public override void activateCooldown(float duration) {
		me.etherialCooldown = duration;
	}
	// The rest stays empty for now (only relevant for Toni)...
	protected override void updateStamina(bool isRunning) {}
	protected override void regainStamina() {}
	protected override void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos) {}
	protected override void preDoorTransitionHook(Data_Door doorTaken) {}
	protected override void preRoomLeavingHook(Data_Door doorTaken) {}
}
