using UnityEngine;
using System;
using System.Collections;

public class Control_Monster : MonoBehaviour {

    [NonSerialized]
    private Data_GameState GS;
    [NonSerialized]
    private Environment_Room currentEnvironment;
    [NonSerialized]
    private Data_Monster me;
    [NonSerialized]
    private Data_PlayerCharacter player;

    private float VERTICAL_ROOM_SPACING;

	private float MONSTER_WALKING_SPEED;
	private float MONSTER_SLOW_WALKING_SPEED;
	private float MONSTER_KILL_RADIUS;
	private float TIME_TO_REACT;
	private float DOOR_TRANSITION_DURATION;

	public bool DEBUG_DANGEROUS; // set to false to make the monster "blind"

	// Use this for initialization
	void Start () {	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState)
	{
		this.GS = gameState;
        this.me = gameState.getMonster();
		this.player = gameState.getCHARA();
		this.currentEnvironment = me.isIn.env;

		MONSTER_WALKING_SPEED = Global_Settings.read("MONSTER_WALKING_SPEED");
		MONSTER_SLOW_WALKING_SPEED = Global_Settings.read("MONSTER_SLOW_WALKING_SPEED");
		MONSTER_KILL_RADIUS = Global_Settings.read("MONSTER_KILL_RADIUS");

		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
		DOOR_TRANSITION_DURATION = Global_Settings.read("DOOR_TRANSITION_DURATION");

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

		if (me.isIn.INDEX == player.isIn.INDEX && DEBUG_DANGEROUS && !player.isInvulnerable()) {
			// The monster is in the same room as the player.
			me.playerInSight = true;
			me.playerDetected = true;
			me.playerPosLastSeen = player.gameObj.transform.position.x;

			float distanceToPlayer = Mathf.Abs(transform.position.x - player.gameObj.transform.position.x);
			if (distanceToPlayer <= MONSTER_KILL_RADIUS) {
				// Getting VERY close the monster, e.g. running past it forces CHARA to drop their item
				if(distanceToPlayer <= MONSTER_KILL_RADIUS / 10) {
					player.control.dropItem();
				}
				player.control.takeDamage();
			} else {
				// The monster moves towards the player.
				moveToPoint(player.gameObj.transform.position.x);
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
		return door.connectsTo.isIn.INDEX == me.forbiddenRoomIndex;
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
			int amountOfDoors = me.isIn.getNumberOfDoors ();
			int selectedDoor = UnityEngine.Random.Range (0, amountOfDoors);

			bool doorFound = false;
			int counter = 0;
			foreach (Data_Door d in me.isIn.DOORS) {
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
		float velocity;
		if (me.playerDetected) {
			velocity = MONSTER_WALKING_SPEED;
		} else {
			velocity = MONSTER_SLOW_WALKING_SPEED;
		}
		float direction = (-1) * Mathf.Sign(transform.position.x - targetPos);

		// Flip the sprite as necessary
		transform.Find("MonsterImage").GetComponent<SpriteRenderer>().flipX = (direction > 0.0f) ? true : false;

		// Calculate the new position
		float displacement = direction * Time.deltaTime * velocity;
		float validPosition = currentEnvironment.validatePosition(transform.position.x + displacement);

        // Move the sprite to the new valid position
        me.updatePosition(validPosition);
        float validDisplacement = validPosition - transform.position.x;
		if (Mathf.Abs(validDisplacement) > 0.0f)
		{
			transform.Translate(validDisplacement, 0, 0);
		}
	}

	// This function transitions the monster through a door
	private IEnumerator goThroughTheDoor(Data_Door door)
	{
		// Hide the sprite
		GetComponentInChildren<Renderer>().enabled = false;

		// Then wait
		me.etherialCooldown = DOOR_TRANSITION_DURATION;
		yield return new WaitForSeconds(DOOR_TRANSITION_DURATION);

		Data_Door destinationDoor = door.connectsTo;
		Data_Room destinationRoom = destinationDoor.isIn;

        // Move character within game state
        float newValidPosition = destinationRoom.env.validatePosition(destinationDoor.atPos);
        me.updatePosition(destinationRoom, newValidPosition);
        currentEnvironment = me.isIn.env;

        // Move character sprite and show it again
		Vector3 targetPosition = new Vector3(newValidPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);
		GetComponentInChildren<Renderer>().enabled = true;

		me.playerDetected = false;

		//Debug.Log(me + " walks from door #" + door + " to door #" + destinationDoor + " at position " + targetPosition);
	}
}
