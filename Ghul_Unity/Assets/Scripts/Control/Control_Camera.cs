using UnityEngine;
using UnityEngine.UI;
using System;

public class Control_Camera : MonoBehaviour {
	// Used for scene transitions
	public Image fadeoutImage;
	// Used to represent immediate danger
	public Image redoutImage;
	// To simulate lighting effects
	public Image darknessImage;

	private Data_GameState GS;
	private Data_Position focusOn;
	private Environment_Room currentEnvironment;

	private float SCREEN_SIZE_HORIZONTAL;
	private float SCREEN_SIZE_VERTICAL;
	private float PANNING_SPEED;
	private float VERTICAL_ROOM_SPACING;

	// There must be this much space between the center of the monster sprite
	// and the edge of the camera's view field to see it "clearly"
	private static float monsterVisibilityThreshold = 0.4f;

	void Awake() {
		SCREEN_SIZE_HORIZONTAL = Global_Settings.read("SCREEN_SIZE_HORIZONTAL");
		SCREEN_SIZE_VERTICAL = Global_Settings.read("SCREEN_SIZE_VERTICAL");
		// Set general movement parameters
		PANNING_SPEED = Global_Settings.read("CAMERA_PANNING_SPEED");
		VERTICAL_ROOM_SPACING = Global_Settings.read("VERTICAL_ROOM_SPACING");
	}

	void Start() {
		fadeoutImage.CrossFadeAlpha(0, 0, false);
		redoutImage.CrossFadeAlpha(0, 0, false);
		darknessImage.CrossFadeAlpha(0, 0, false);
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState) {
		GS = gameState;
	}

	// Update is called once per frame
	void Update() {
		if(GS == null) {
			return;
		} // Don't do anything if the game state is not loaded yet

		if(focusOn != null) {
			// Calculate the differences between the camera's current and target position
			float targetPositionX = currentEnvironment.validateCameraPosition(focusOn.X);
			float displacementY = focusOn.RoomId * VERTICAL_ROOM_SPACING - transform.position.y;
			float displacementX = targetPositionX - transform.position.x;
			// If there is a difference in the vertical direction, close it all at once (provisional)
			if(Mathf.Abs(displacementY) > 0) {
				// Update the environment
				this.currentEnvironment = GS.getRoomByIndex(focusOn.RoomId).env;
				// Reevaluate and apply camera displacement
				displacementX = currentEnvironment.validateCameraPosition(focusOn.X) - transform.position.x;
				// Move and fade back in
				transform.Translate(displacementX, displacementY, 0);
				return;
			}

			// Otherwise, pan gradually
			displacementX *= Time.deltaTime * PANNING_SPEED;
			// Correct displacement
			if(Mathf.Abs(displacementX) > 0) {
				transform.Translate(displacementX, 0, 0);
			}
		}
	}

	// Set an object to focus on
	public void setFocusOn(Data_Position pos) {
		focusOn = pos;
		currentEnvironment = GS.getRoomByIndex(pos.RoomId).env;
	}

	// Update the camera position
	public void updateCameraStatus() {
		Debug.Log("Camera looks at #" + focusOn.RoomId + " at position " + focusOn.X);
	}

	// Asynchronously fades to black
	public void fadeOut(float duration) {
		fadeoutImage.CrossFadeAlpha(1f, duration, false);
	}

	// Asynchronously fades back from black
	public void fadeIn(float duration) {
		fadeoutImage.CrossFadeAlpha(0, duration, false);
	}

	// Set red overlay intensity
	public void setRedOverlay(float intensity) {
		intensity = Mathf.Min(0.9f, Mathf.Max(0, intensity));
		redoutImage.CrossFadeAlpha(intensity, 0, false);
	}

	// Asynchronously fades back from red
	public void resetRedOverlay() {
		redoutImage.CrossFadeAlpha(0, 1f, false);
	}

	// Checks if the monster is clearly visible on camera
	public bool isMonsterClearlyVisible() {
		if(GS == null) {
			return false;
		}
		// First, check the monster's vertical position
		Vector3 monsterPos = GS.getMonster().control.transform.position;
		if(Math.Abs(monsterPos.y - transform.position.y) < SCREEN_SIZE_VERTICAL / 2) {
			// Then, check the monster horizontal position
			float leftBound = transform.position.x - SCREEN_SIZE_HORIZONTAL / 2 + monsterVisibilityThreshold;
			float rightBound = transform.position.x + SCREEN_SIZE_HORIZONTAL / 2 - monsterVisibilityThreshold;
			return (leftBound <= monsterPos.x && monsterPos.x <= rightBound);
		} else {
			return false;
		}
	}
}