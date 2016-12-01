using UnityEngine;
using System;

public class Control_PlayerCharacter : MonoBehaviour {

    [NonSerialized]
    private Data_GameState GS;
    [NonSerialized]
    private Environment_Room currentEnvironment;
    [NonSerialized]
    private Data_Character me;

    private float VERTICAL_ROOM_SPACING;

    private float WALKING_SPEED;
    private float RUNNING_SPEED;

    private float RUNNING_STAMINA_LOSS;
    private float WALKING_STAMINA_GAIN;
    private float STANDING_STAMINA_GAIN;

    private float DOOR_COOLDOWN; // This prevents the character "flickering" between doors
    private float DOOR_COOLDOWN_DURATION; // This prevents the character "flickering" between doors

    // Use this for initialization; note that only local variables are initialized here, game state is loaded later
    void Start () {
        DOOR_COOLDOWN = Time.timeSinceLevelLoad;
        //InvokeRepeating("updatePlayerCharacterPosition", 1.0f, 1.0f);
    }

    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState)
    {
        this.GS = gameState;
        this.me = gameState.getCHARA();
        this.currentEnvironment = me.isIn.env;

        // Set general movement parameters
        WALKING_SPEED = GS.getSetting("CHARA_WALKING_SPEED");
        RUNNING_SPEED = GS.getSetting("CHARA_RUNNING_SPEED");

        RUNNING_STAMINA_LOSS = GS.getSetting("RUNNING_STAMINA_LOSS");
        WALKING_STAMINA_GAIN = GS.getSetting("WALKING_STAMINA_GAIN");
        STANDING_STAMINA_GAIN = GS.getSetting("STANDING_STAMINA_GAIN");

        VERTICAL_ROOM_SPACING = GS.getSetting("VERTICAL_ROOM_SPACING");
        DOOR_COOLDOWN_DURATION = GS.getSetting("DOOR_COOLDOWN_DURATION");
    }

    // Update is called once per frame
    void Update () {
        if (GS == null) { return; } // Don't do anything until game state is loaded

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
}
