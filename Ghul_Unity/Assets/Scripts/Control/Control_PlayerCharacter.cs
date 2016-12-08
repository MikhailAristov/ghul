using UnityEngine;
using System;
using System.Collections;

public class Control_PlayerCharacter : MonoBehaviour {

    [NonSerialized]
    private Data_GameState GS;
    [NonSerialized]
    private Environment_Room currentEnvironment;
    [NonSerialized]
    private Data_PlayerCharacter me;
	[SerializeField]
	private Data_Cadaver cadaver;

    private float VERTICAL_ROOM_SPACING;

    private float WALKING_SPEED;
    private float RUNNING_SPEED;

    private float RUNNING_STAMINA_LOSS;
    private float WALKING_STAMINA_GAIN;
    private float STANDING_STAMINA_GAIN;

    private float DOOR_COOLDOWN; // This prevents the character "flickering" between doors
    private float DOOR_COOLDOWN_DURATION; // This prevents the character "flickering" between doors

	private float TOTAL_DEATH_DURATION;
	private float DEATH_DURATION;
	private float TIME_TO_REACT;

	public Sprite tombstone; // DEBUG only - display of death
	private Sprite stickman; // DEBUG only
	private SpriteRenderer stickmanRenderer; // DEBUG only

    // Use this for initialization; note that only local variables are initialized here, game state is loaded later
    void Start () {
        DOOR_COOLDOWN = Time.timeSinceLevelLoad;
		stickmanRenderer = transform.Find("Stickman").gameObject.GetComponent<SpriteRenderer>(); // Find the child "Stickman", then its Sprite Renderer and then the renderer's sprite
		stickman = stickmanRenderer.sprite;
    }

    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState)
    {
        this.GS = gameState;
        this.me = gameState.getCHARA();
        this.currentEnvironment = me.isIn.env;
		DEATH_DURATION = me.deathDuration;
		me.startingPos = me.pos.clone();

        // Set general movement parameters
        WALKING_SPEED = GS.getSetting("CHARA_WALKING_SPEED");
        RUNNING_SPEED = GS.getSetting("CHARA_RUNNING_SPEED");

        RUNNING_STAMINA_LOSS = GS.getSetting("RUNNING_STAMINA_LOSS");
        WALKING_STAMINA_GAIN = GS.getSetting("WALKING_STAMINA_GAIN");
        STANDING_STAMINA_GAIN = GS.getSetting("STANDING_STAMINA_GAIN");

        VERTICAL_ROOM_SPACING = GS.getSetting("VERTICAL_ROOM_SPACING");
        DOOR_COOLDOWN_DURATION = GS.getSetting("DOOR_COOLDOWN_DURATION");

		TOTAL_DEATH_DURATION = GS.getSetting("TOTAL_DEATH_DURATION");
		TIME_TO_REACT = GS.getSetting("TIME_TO_REACT");

		me.remainingReactionTime = TIME_TO_REACT;
		
        // Move the character sprite directly to where the game state says it should be standing
        Vector3 savedPosition = new Vector3(me.atPos, me.isIn.INDEX * VERTICAL_ROOM_SPACING);
        transform.Translate(savedPosition - transform.position);
    }

    // Update is called once per frame
    void Update () {
		if (GS == null || GS.SUSPENDED || !me.controllable) { return; } // Don't do anything if the game state is not loaded yet or suspended

		//FOR DEBUGGIN ONLY - Dying on command
		if (Input.GetButtonDown("Die")) {
			dying();
		}

		// Action (taking an item)
		if (Input.GetButtonDown("Action")) {
			takeItem();
		}

        // Vertical "movement"
        if (Input.GetAxis("Vertical") > 0.1f && Time.timeSinceLevelLoad > DOOR_COOLDOWN)
        {
            // Check if the character can walk through the door, and if so, move them to the "other side"
            Data_Door door = currentEnvironment.getDoorAtPos(transform.position.x);
            if (door != null)
            {
                goThroughTheDoor(door);
                return;
            }
        }
			
        // Horizontal movement
        if (Input.GetAxis("Horizontal") > 0.01f || Input.GetAxis("Horizontal") < -0.01f)
        {
            // Flip the sprite as necessary
            transform.Find("Stickman").GetComponent<SpriteRenderer>().flipX = (Input.GetAxis("Horizontal") < 0.0f) ? true : false;

            // Determine movement speed
            float velocity = WALKING_SPEED;
            if (Input.GetButton("Run") && !me.exhausted) {
                velocity = RUNNING_SPEED;
                me.modifyStamina(RUNNING_STAMINA_LOSS * Time.deltaTime);
            } else {
                me.modifyStamina(WALKING_STAMINA_GAIN * Time.deltaTime);
            }

            // Calculate the new position
            float displacement = Input.GetAxis("Horizontal") * Time.deltaTime * velocity;
            float newPosition = transform.position.x + displacement;
            float validPosition = currentEnvironment.validatePosition(transform.position.x + displacement);
            
            // If validated position is different from the calculated position, we have reached the side of the room
            if(validPosition > newPosition &&            // moving left (negative direction) gets us through the left door
                Time.timeSinceLevelLoad > DOOR_COOLDOWN)
            {
                Data_Door leftDoor = currentEnvironment.getDoorOnTheLeft();
                if (leftDoor != null) { goThroughTheDoor(leftDoor); }
                return;
            }
            else if (validPosition < newPosition &&      // moving right (positive direction) gets us through the right door
                Time.timeSinceLevelLoad > DOOR_COOLDOWN)
            {
                Data_Door rightDoor = currentEnvironment.getDoorOnTheRight();
                if (rightDoor != null) { goThroughTheDoor(rightDoor); }
                return;
            }

            // Move the sprite to the new valid position
            me.updatePosition(validPosition);
            float validDisplacement = validPosition - transform.position.x;
            if (Mathf.Abs(validDisplacement) > 0.0f)
            {
                transform.Translate(validDisplacement, 0, 0);
            }
        } else
        { // Regain stamina while standing still
            me.modifyStamina(STANDING_STAMINA_GAIN * Time.deltaTime);
        }
    }

    // This function transitions the character through a door
    private void goThroughTheDoor(Data_Door door)
    {
        Data_Door destinationDoor = door.connectsTo;
        Data_Room destinationRoom = destinationDoor.isIn;

        // Update door cooldown
        DOOR_COOLDOWN = Time.timeSinceLevelLoad + DOOR_COOLDOWN_DURATION;

        // Move character within game state
        float newValidPosition = destinationRoom.env.validatePosition(destinationDoor.atPos);
        me.updatePosition(destinationRoom, newValidPosition);
        currentEnvironment = me.isIn.env;
		me.remainingReactionTime = TIME_TO_REACT;

        // Move character sprite
        Vector3 targetPosition = new Vector3(newValidPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
        transform.Translate(targetPosition - transform.position);

        Debug.Log(me + " walks from door #" + door + " to door #" + destinationDoor + " at position " + targetPosition);
        
        // Trigger an autosave upon changing locations
        Data_GameState.saveToDisk(GS);
    }

    // Update the player character's position
    public void updateCharacterPosition()
    {
        Debug.Log(me + " is in room #" + me.isIn + " at position " + me.atPos);
    }

	// Player withing the attack radius -> reduce time to react
	public void beingAttacked() {
		me.remainingReactionTime -= Time.deltaTime;
		if (me.remainingReactionTime <= 0.0f) {
			dying();
		}
	}

	// Activate the player's death scene and drop all the items
	private void dying() {
		if (!me.isDying) {
			me.isDying = true;
			stickmanRenderer.sprite = tombstone;
			// TODO: Remove this line when the dying animation exists. This line only moves the tombstone
			//		 such that it doesn't float
			transform.Find("Stickman").gameObject.transform.Translate (new Vector3 (0, -1.0f, 0));
			me.controllable = false;
			StartCoroutine (waitingForRespawn());

			// TODO: dropping the items at the cadaver's place (requires adding new item spots)
		}
	}

	// Reset the player to the starting location after the total death duration set in Data_GameState.
	private IEnumerator waitingForRespawn() {
		while (DEATH_DURATION < TOTAL_DEATH_DURATION) {
			me.deathDuration += Time.deltaTime;
			DEATH_DURATION = me.deathDuration;
			yield return null;
		}

		// Place the cadaver
		Vector3 positionOfCadaver = new Vector3(transform.position.x, transform.position.y - 1.55f, transform.position.z);
		GS.getCadaver().gameObj.transform.Translate(positionOfCadaver - GS.getCadaver().gameObj.transform.position);
		GS.getCadaver().updatePosition(me.isIn, transform.position.x);

		// Death duration is over. Reset the position.
		me.deathDuration = 0.0f;
		DEATH_DURATION = me.deathDuration;
		// TODO: Remove this line when the dying animation exists. This line only moves the character up again (see above)
		transform.Find("Stickman").gameObject.transform.Translate(new Vector3(0,1.0f,0));
		stickmanRenderer.sprite = stickman;
		me.controllable = true;
		me.resetPosition(GS);
		currentEnvironment = me.isIn.env;

		// Move character sprite
		Vector3 targetPosition = new Vector3(me.startingPos.X, me.startingPos.RoomId * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);

		me.isDying = false;
		me.remainingReactionTime = TIME_TO_REACT;

		yield return null;
	}

	// The player takes a nearby item if there is any
	private void takeItem() {
		int itemIndex = currentEnvironment.getItemIndexAtPos(transform.position.x);
		if (itemIndex >= 0) {
			// the player got the item with index itemIndex.
			me.addItemToList(itemIndex);
			currentEnvironment.removeItemAtPos(transform.position.x);
			GS.removeItemSprite(itemIndex);
			Debug.Log ("Item #" + itemIndex + " collected.");

			// Auto save when collecting an item.
			Data_GameState.saveToDisk(GS);
		}
	}
}
