using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public GameObject movementBlockerLeft;
	public GameObject movementBlockerRight;
	private Transform blockLeftTrafo;
	private Transform blockRightTrafo;

	private float runSpeedMultiplier; // Multiplies the basic movement speed. Value is 1, if Run is not pressed.
	private const float RUN_SPEED_MULTIPLIER_INCREASEMENT = 0.8f; // speed multiplier increases by this amount when run button is held
	private const float RUN_SPEED_MULTIPLIER_DECREASEMENT = 1.2f; // speed multiplier decreases by this amount when run button is not held
	private const float MAX_RUN_SPEED_MULTIPLIER = 2.0f;
	private const float BASIC_MOVEMENT_SPEED = 5.0f;

	private Transform trafo; // contains position, rotation, scale of the player

	// Use this for initialization
	void Start () {
		runSpeedMultiplier = 1.0f;
		trafo = transform;
		blockLeftTrafo = movementBlockerLeft.transform;
		blockRightTrafo = movementBlockerRight.transform;
	}

	// returns the current speed. Still requires multiplication by deltaTime.
	public float GetSpeed() {
		return BASIC_MOVEMENT_SPEED * runSpeedMultiplier;
	}
	
	// Update is called once per frame
	void Update () {
		print (BASIC_MOVEMENT_SPEED * runSpeedMultiplier);
		bool directionKeyPressed = (Input.GetAxis ("Horizontal") > 0.01f || Input.GetAxis ("Horizontal") < -0.01f);

		// Run speed handling
		if (Input.GetButton ("Run") && directionKeyPressed) {
			runSpeedMultiplier = Mathf.Min (MAX_RUN_SPEED_MULTIPLIER, runSpeedMultiplier + RUN_SPEED_MULTIPLIER_INCREASEMENT * Time.deltaTime);
		} else {
			runSpeedMultiplier = Mathf.Max (1.0f, runSpeedMultiplier - RUN_SPEED_MULTIPLIER_DECREASEMENT * Time.deltaTime);
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
	}
}
