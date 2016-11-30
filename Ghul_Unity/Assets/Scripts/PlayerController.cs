using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public GameObject movementBlockerLeft;
	public GameObject movementBlockerRight;
	private Transform blockLeftTrafo;
	private Transform blockRightTrafo;

	private float runSpeedMultiplier; // Multiplies the basic movement speed. Value is 1, if Run is not pressed.
	private const float RUN_SPEED_MULTIPLIER_INCREMENT = 0.8f; // speed multiplier increases by this amount when run button is held
	private const float RUN_SPEED_MULTIPLIER_DECREMENT = 1.2f; // speed multiplier decreases by this amount when run button is not held
	private const float MAX_RUN_SPEED_MULTIPLIER = 2.0f;
	private const float BASIC_MOVEMENT_SPEED = 5.0f;

	private float stamina;
	private const float MAX_STAMINA = 10.0f;
	private const float STAMINA_RECOVERY_SPEED = 2.0f;
	private bool isOutOfPower; // if all stamina is used up, player can't run until stamina is full again TODO: Stamina bar (?)

	private Transform trafo; // contains position, rotation, scale of the player

	// Use this for initialization
	void Start () {
		runSpeedMultiplier = 1.0f;
		stamina = MAX_STAMINA;
		isOutOfPower = false;

		trafo = transform;
		blockLeftTrafo = movementBlockerLeft.transform;
		blockRightTrafo = movementBlockerRight.transform;
	}

	// returns the current speed. Still requires multiplication by deltaTime.
	public float GetSpeed() {
		return BASIC_MOVEMENT_SPEED * runSpeedMultiplier;
	}

	// returns the current stamina value
	public float GetStamina() {
		return stamina;
	}

	public bool IsOutOfPower() {
		return isOutOfPower;
	}
	
	// Update is called once per frame
	//TODO: Move content to sub functions to avoid cluttering
	void Update () {
		//print (BASIC_MOVEMENT_SPEED * runSpeedMultiplier); //prints the speed to the console
		bool directionKeyPressed = (Input.GetAxis ("Horizontal") > 0.01f || Input.GetAxis ("Horizontal") < -0.01f);

		// Run speed handling
		if (Input.GetButton ("Run") && directionKeyPressed && !isOutOfPower) {
			runSpeedMultiplier = Mathf.Min (MAX_RUN_SPEED_MULTIPLIER, runSpeedMultiplier + RUN_SPEED_MULTIPLIER_INCREMENT * Time.deltaTime);
		} else {
			runSpeedMultiplier = Mathf.Max (1.0f, runSpeedMultiplier - RUN_SPEED_MULTIPLIER_DECREMENT * Time.deltaTime);
		}

		// Horizontal movement
		if (directionKeyPressed) {
			float movement = Input.GetAxis ("Horizontal") * Time.deltaTime * BASIC_MOVEMENT_SPEED * runSpeedMultiplier;
			trafo.Translate (movement, 0, 0);
		}

		// Boundary handling
		if (trafo.position.x <= blockLeftTrafo.position.x) {
			trafo.position = new Vector3(blockLeftTrafo.position.x, trafo.position.y, trafo.position.z);
		} else if (trafo.position.x >= blockRightTrafo.position.x) {
			trafo.position = new Vector3(blockRightTrafo.position.x, trafo.position.y, trafo.position.z);
		}

		// Stamina handling
		if (runSpeedMultiplier > 1.0f) {
			stamina = Mathf.Max (0.0f, stamina - runSpeedMultiplier * Time.deltaTime);
			if (stamina == 0.0f) {
				isOutOfPower = true;
			}
		} else {
			stamina = Mathf.Min(MAX_STAMINA, stamina + STAMINA_RECOVERY_SPEED * Time.deltaTime);
			if (stamina == MAX_STAMINA) {
				isOutOfPower = false;
			}
		}
	}
}
