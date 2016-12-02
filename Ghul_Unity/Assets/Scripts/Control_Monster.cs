using UnityEngine;

public class Control_Monster : MonoBehaviour {

	private Data_GameState GS;
	private Environment_Room currentEnvironment;
	private Data_Monster me;
	private Data_Character player;

	private float VERTICAL_ROOM_SPACING;

	private float MONSTER_WALKING_SPEED;
	private float MONSTER_KILL_RADIUS;
	private float TIME_TO_REACT;

	private float DOOR_COOLDOWN; // This prevents the character "flickering" between doors
	private float DOOR_COOLDOWN_DURATION; // This prevents the character "flickering" between doors

	private const bool DEBUG_KILLABLE = true; // set to false to make the monster "blind"

	// Use this for initialization
	void Start () {
		DOOR_COOLDOWN = Time.timeSinceLevelLoad;
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState)
	{
		this.GS = gameState;
		this.me = gameState.MONSTER;
		this.player = gameState.PLAYER_CHARACTER;
		this.currentEnvironment = me.isIn.env;

		MONSTER_WALKING_SPEED = GS.getSetting("MONSTER_WALKING_SPEED");
		MONSTER_KILL_RADIUS = GS.getSetting("MONSTER_KILL_RADIUS");

		VERTICAL_ROOM_SPACING = GS.getSetting("VERTICAL_ROOM_SPACING");
		DOOR_COOLDOWN_DURATION = GS.getSetting("DOOR_COOLDOWN_DURATION");
	}

	// Update is called once per frame
	void Update () {
		if (GS == null) { return; } // Don't do anything until game state is loaded

		if (me.isIn.INDEX == player.isIn.INDEX && DEBUG_KILLABLE) {
			// The monster is in the same room as the player.
			me.playerInSight = true;
			me.playerDetected = true;
			me.playerPosLastSeen = player.gameObj.transform.position.x;

			if (Mathf.Abs(transform.position.x - player.gameObj.transform.position.x) <= MONSTER_KILL_RADIUS) {
				player.control.beingAttacked();
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
					if (Mathf.Abs (transform.position.x - me.randomTargetPos) <= 0.1f) {
						checkForDoors();
						me.isRandomTargetSet = false;
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

	// if the monster is in reach for a door, go through it
	private void checkForDoors() {
		
			// Reached the point where the player was last seen. Go through door
			if (Time.timeSinceLevelLoad > DOOR_COOLDOWN) {
				// Check if the monster can walk through the door, and if so, move them to the "other side"
				Data_Door door = currentEnvironment.getDoorAtPos (transform.position.x);
				if (door != null) {
					goThroughTheDoor (door);
				} else {
					Data_Door leftDoor = currentEnvironment.getDoorOnTheLeft ();
					Data_Door rightDoor = currentEnvironment.getDoorOnTheRight ();
					if (leftDoor != null && transform.position.x < 0.0f) { 
						goThroughTheDoor (leftDoor); 
					} else if (rightDoor != null && transform.position.x > 0.0f) {
						goThroughTheDoor (rightDoor);
					} else {
						// no door found
						
					}
				}
			}
	}

	// The monster decides randomly what it does next.
	private void randomMovementDecision() {
		int rand = UnityEngine.Random.Range(0,4);
		switch (rand) {
		case 0:
			// Thinking
			me.remainingThinkingTime = UnityEngine.Random.Range (1, 5);
			me.isThinking = true;
			break;

		case 1:
			// walking left
			float pointOfInterestL = transform.position.x - UnityEngine.Random.Range (0, 5);
			float validPointOfInterestL = currentEnvironment.validatePosition (pointOfInterestL);
			if (pointOfInterestL <= validPointOfInterestL + 0.5f) {
				// the monster doesn't walk to close to the wall.
				pointOfInterestL = validPointOfInterestL + 0.5f;
			}
			me.randomTargetPos = pointOfInterestL;
			break;
		case 2:
			
			// walking right
			float pointOfInterestR = transform.position.x + UnityEngine.Random.Range (0, 5);
			float validPointOfInterestR = currentEnvironment.validatePosition (pointOfInterestR);
			if (pointOfInterestR >= validPointOfInterestR - 0.5f) {
				// the monster doesn't walk to close to the wall.
				pointOfInterestR = validPointOfInterestR - 0.5f;
			}
			me.randomTargetPos = pointOfInterestR;
			break;

		case 3:
			// going to a door
			int amountOfDoors = me.isIn.getAmountOfDoors ();
			int selectedDoor = UnityEngine.Random.Range (0, amountOfDoors);
			bool doorFound = false;
			foreach (Data_Door d in me.isIn.DOORS) {
				if (d.INDEX == selectedDoor) {
					me.randomTargetPos = d.atPos;
					doorFound = true;
					break;
				}
			}
			if (!doorFound) { 
				// Confused. Monster needs time to think.
				me.remainingThinkingTime = 3.0f;
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
		float velocity = MONSTER_WALKING_SPEED;
		float direction = (-1) * Mathf.Sign(transform.position.x - targetPos);

		// Flip the sprite as necessary
		transform.Find("MonsterImage").GetComponent<SpriteRenderer>().flipX = (direction > 0.0f) ? true : false;

		// Calculate the new position
		float displacement = direction * Time.deltaTime * velocity;
		float validPosition = currentEnvironment.validatePosition(transform.position.x + displacement);

		// Move the sprite to the new valid position
		float validDisplacement = validPosition - transform.position.x;
		if (Mathf.Abs(validDisplacement) > 0.0f)
		{
			transform.Translate(validDisplacement, 0, 0);
		}
	}

	// This function transitions the monster through a door
	private void goThroughTheDoor(Data_Door door)
	{
		Data_Door destinationDoor = door.connectsTo;
		Data_Room destinationRoom = destinationDoor.isIn;

		// Update door cooldown
		DOOR_COOLDOWN = Time.timeSinceLevelLoad + DOOR_COOLDOWN_DURATION;

		// Move character within game state
		me.moveToRoom(destinationRoom);
		currentEnvironment = me.isIn.env;

		// Move character sprite
		float validPosition = currentEnvironment.validatePosition(destinationDoor.atPos);
		Vector3 targetPosition = new Vector3(validPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);

		me.playerDetected = false;

		Debug.Log(me + " walks from door #" + door + " to door #" + destinationDoor + " at position " + targetPosition);
	}
}
