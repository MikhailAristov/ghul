using UnityEngine;
using System.Collections;

public class Control_PlayerCharacter : MonoBehaviour {

    private Data_GameState GS;
    private Environment_Room currentEnvironment;
    private Data_Character me;

    private float WALKING_SPEED;
    private float RUNNING_SPEED;
    private float VERTICAL_ROOM_SPACING;

    // Use this for initialization; note that only local variables are initialized here, game state is loaded later
    void Start () {
        return;
    }

    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState)
    {
        this.GS = gameState;
        this.me = gameState.PLAYER_CHARACTER;
        this.currentEnvironment = me.isIn.gameObj.GetComponent<Environment_Room>();

        // Set general movement parameters
        WALKING_SPEED = GS.getSetting("CHARA_WALKING_SPEED");
        RUNNING_SPEED = GS.getSetting("CHARA_RUNNING_SPEED");
        VERTICAL_ROOM_SPACING = GS.getSetting("VERTICAL_ROOM_SPACING");
    }

    // Update is called once per frame
    void Update () {
        if (GS == null) { return; } // Don't do anything until game state is loaded

        bool directionKeyPressed = (Input.GetAxis("Horizontal") > 0.01f || Input.GetAxis("Horizontal") < -0.01f);
        float displacement = 0.0f;

        // Horizontal movement
        if (directionKeyPressed)
        {
            displacement = Input.GetAxis("Horizontal") * Time.deltaTime * WALKING_SPEED;
            // Validate the new position 
            displacement = currentEnvironment.validatePosition(transform.position.x + displacement) - transform.position.x;

            // Flip the sprite as necessary
            transform.Find("Stickman").GetComponent<SpriteRenderer>().flipX = (Input.GetAxis("Horizontal") < 0.0f) ? true : false;

            if (Mathf.Abs(displacement) > 0.0f)
            {
                transform.Translate(displacement, 0, 0);
            }
        }
        /*
        // Vertical "movement"
        if(Input.GetAxis("Vertical") > 0.1f)
        {
            // Check if the character can walk through the door
            Data_Door door = currentEnvironment.getDoorAtPos(transform.position.x);
            // If so, move them to the "other side" of the door
            if(door != null)
            {   
                Data_Door targetDoor = door.connectsTo;
                Debug.Log(me + " walks from door #" + door + " to door #" + targetDoor);
                Vector3 targetPosition = new Vector3(targetDoor.atPos, targetDoor.INDEX * VERTICAL_ROOM_SPACING);
                //transform.Translate(transform.position - targetPosition);
            }
        }
        */
    }
}
