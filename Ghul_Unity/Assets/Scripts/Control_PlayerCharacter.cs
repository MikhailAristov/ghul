using UnityEngine;
using System.Collections;

public class Control_PlayerCharacter : MonoBehaviour {

    public GameObject currentRoom;
    private Environment_Room currentEnvironment;
    
    private Transform trafo;
    private const float WALKING_SPEED = 5.0f;
    private const float RUNNING_SPEED = 10.0f;

    // Use this for initialization
    void Start () {
        trafo = transform;
        currentEnvironment = currentRoom.GetComponent<Environment_Room>();
    }
	
	// Update is called once per frame
	void Update () {
        bool directionKeyPressed = (Input.GetAxis("Horizontal") > 0.01f || Input.GetAxis("Horizontal") < -0.01f);
        float displacement = 0.0f;

        // Horizontal movement
        if (directionKeyPressed)
        {
            displacement = Input.GetAxis("Horizontal") * Time.deltaTime * WALKING_SPEED;
            // Validate the new position 
            displacement = currentEnvironment.validatePosition(trafo.position.x + displacement) - trafo.position.x;
            
            // Flip the sprite as necessary
            trafo.Find("Stickman").GetComponent<SpriteRenderer>().flipX = (Input.GetAxis("Horizontal") < 0.0f) ? true : false;
        }

        if (Mathf.Abs(displacement) > 0.0f)
        {
            trafo.Translate(displacement, 0, 0);
        }
    }
}
