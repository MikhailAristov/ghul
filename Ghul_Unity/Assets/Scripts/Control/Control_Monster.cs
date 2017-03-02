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

	private float KILL_RADIUS;
	private float TIME_TO_REACT;
	private float ATTACK_RANGE;
	private float ATTACK_MARGIN;
	private float ATTACK_DURATION;

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

		ATTACK_RANGE = Global_Settings.read("MONSTER_ATTACK_RANGE");
		ATTACK_MARGIN = Global_Settings.read("MONSTER_ATTACK_MARGIN");
		ATTACK_DURATION = Global_Settings.read("MONSTER_ATTACK_DURATION");

		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");

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

		if (GS.monsterSeesToni && IS_DANGEROUS && !Toni.isInvulnerable()) {
			// The monster is in the same room as the player.
			me.playerInSight = true;
			me.playerDetected = true;
			me.playerPosLastSeen = Toni.gameObj.transform.position.x;

			float distanceToPlayer = GS.distanceToToni;
			if (distanceToPlayer <= KILL_RADIUS) {
				// Getting VERY close the monster, e.g. running past it forces CHARA to drop their item
				if(distanceToPlayer <= KILL_RADIUS / 10) {
					Toni.control.dropItem();
				}
				Toni.control.takeDamage();
			} else {
				// The monster moves towards the player.
				moveToPoint(Toni.gameObj.transform.position.x);
			}

		} else {
			
			// The monster is not in the same room as the player.
			if (me.playerDetected) {
				
				// The monster knows where to go next
				moveToPoint (me.playerPosLastSeen);
				if (Mathf.Abs (transform.position.x - me.playerPosLastSeen) <= 0.1f) {
					checkForDoors();
					me.playerDetected = false;
				}

			} else {

				// The monster walks around randomly.
				if (!me.isRandomTargetSet) {
					randomMovementDecision();
				}
				if (!me.isThinking) {
					moveToPoint(me.randomTargetPos);
					if (Mathf.Abs (transform.position.x - me.randomTargetPos) <= 0.6f) {
						if (checkForDoors ()) {
							me.remainingThinkingTime = UnityEngine.Random.Range (1, 2);
							me.isThinking = true;
						} else {
							//print ("The monster can't find a door.");
							me.isRandomTargetSet = false;
						}
					}
				} else {
					me.remainingThinkingTime -= Time.deltaTime;
					if (me.remainingThinkingTime <= 0.0f) {
						me.isThinking = false;
						me.isRandomTargetSet = false;
					}
				}

			}
		}

	}

	// If the door connects to the portal room, the monster isn't allowed to enter it.
	private bool checkIfForbiddenDoor(Data_Door door) {
		return (door.connectsTo.isIn.INDEX == me.forbiddenRoomIndex);
	}

	// if the monster is in reach for a door, go through it
	private bool checkForDoors() {

		// Reached the point where the player was last seen. Go through door
		// Check if the monster can walk through the door, and if so, move them to the "other side"
		Data_Door door = currentEnvironment.getDoorAtPos (transform.position.x);

		if (door != null) {
			if (!checkIfForbiddenDoor(door)) { StartCoroutine(goThroughTheDoor(door)); }
			return true;

		} else {
			Data_Door leftDoor = currentEnvironment.getDoorOnTheLeft();
			Data_Door rightDoor = currentEnvironment.getDoorOnTheRight();
			if (leftDoor != null && transform.position.x < 0.0f) {
				if (!checkIfForbiddenDoor(leftDoor)) { StartCoroutine(goThroughTheDoor(leftDoor)); }
				return true;
			} else if (rightDoor != null && transform.position.x > 0.0f) {
				if (!checkIfForbiddenDoor(rightDoor)) { StartCoroutine(goThroughTheDoor(rightDoor)); }
				return true;
			} else {
				// no door found
				return false;
			}
		}
	}

	// The monster decides randomly what it does next.
	private void randomMovementDecision() {
		int rand = UnityEngine.Random.Range(0,6);
		switch (rand) {
		case 0:
			// Thinking
			me.remainingThinkingTime = UnityEngine.Random.Range (1.5f, 4.0f);
			me.isThinking = true;
			break;

		case 1:
			// walking left
			float pointOfInterestL = transform.position.x - UnityEngine.Random.Range (1, 5);
			float validPointOfInterestL = currentEnvironment.validatePosition (pointOfInterestL);
			if (pointOfInterestL <= validPointOfInterestL + 0.5f) {
				// the monster doesn't walk to close to the wall.
				pointOfInterestL = validPointOfInterestL + 0.5f;
			}
			me.randomTargetPos = pointOfInterestL;
			break;
		case 2:
			
			// walking right
			float pointOfInterestR = transform.position.x + UnityEngine.Random.Range (1, 5);
			float validPointOfInterestR = currentEnvironment.validatePosition (pointOfInterestR);
			if (pointOfInterestR >= validPointOfInterestR - 0.5f) {
				// the monster doesn't walk to close to the wall.
				pointOfInterestR = validPointOfInterestR - 0.5f;
			}
			me.randomTargetPos = pointOfInterestR;
			break;

		case 3:
		case 4:
		case 5:
			// going to a door
			int numberOfDoors = me.isIn.DOORS.Count;
			int selectedDoor = UnityEngine.Random.Range (0, numberOfDoors);

			bool doorFound = false;
			int counter = 0;
			foreach (Data_Door d in me.isIn.DOORS.Values) {
				if (counter == selectedDoor) {
					me.randomTargetPos = d.atPos;
					doorFound = true;
					break;
				}
				counter++;
			}
			if (!doorFound) { 
				// Confused...
				//print("The monster can't find a door.");
				me.remainingThinkingTime = 2.0f;
				me.isThinking = true;
			}
			break;

		default:
			break;
		}

		me.isRandomTargetSet = true;
	}

	// The monster approaches the target position
	private void moveToPoint(float targetPos) {
		float direction = Mathf.Sign(targetPos - transform.position.x);
		walk(direction, me.playerDetected);
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
		me.setForbiddenRoomIndex(-1); // Civilians are allowed to enter the ritual room.

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
	// Instead of making noise, the monster updates its position in the world model and checks whether it sees Toni
	protected override void makeNoise(int type, Data_Position atPos) {
		worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
	}
	// The rest stays empty for now (only relevant for Toni)...
	protected override void updateStamina(bool isRunning) {}
	protected override void regainStamina() {}
	protected override void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos) {}
	protected override void activateCooldown(float duration) { me.etherialCooldown = duration; }
	protected override void cameraFadeOut(float duration) {}
	protected override void cameraFadeIn(float duration) {}
	protected override void updateDoorUsageStatistic(Data_Door door, Data_Room currentRoom, Data_Door destinationDoor, Data_Room destinationRoom) {}
}
