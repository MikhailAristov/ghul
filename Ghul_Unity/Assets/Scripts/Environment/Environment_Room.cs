using UnityEngine;
using System;

public class Environment_Room : MonoBehaviour {

	public GameObject DangerIndicator;

	[NonSerialized]
	private Data_GameState GS;
	[NonSerialized]
	private Data_Room me;

	private float leftCameraBoundary;
	private float rightCameraBoundary;

	private float SCREEN_SIZE_HORIZONTAL;
	private float MARGIN_DOOR_ENTRANCE;

	void Awake() {
		SCREEN_SIZE_HORIZONTAL = Global_Settings.read("SCREEN_SIZE_HORIZONTAL");
		MARGIN_DOOR_ENTRANCE = Global_Settings.read("MARGIN_DOOR_ENTRANCE");
	}

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState, int ownIndex) {
		this.GS = gameState;
		me = GS.getRoomByIndex(ownIndex);

		// Set general movement parameters
		leftCameraBoundary = (SCREEN_SIZE_HORIZONTAL - me.width) / 2;
		rightCameraBoundary = (me.width - SCREEN_SIZE_HORIZONTAL) / 2;
	}

	// Checks whether a position X lies within the boundaries of the room
	// Returns X if its within the boundaries, or the closest boundary if it is not
	public float validatePosition(float pos) {
		return Mathf.Min(Mathf.Max(pos, me.leftWalkBoundary), me.rightWalkBoundary);
	}

	// Checks whether a position X lies within the allowed camera span
	// Returns X if its within the boundaries, or the closest boundary if it is not
	public float validateCameraPosition(float pos) {
		if(me.width <= SCREEN_SIZE_HORIZONTAL) {
			// In rooms smaller than the camera width, just center the view permanently
			return 0;
		} else {
			return Mathf.Min(Mathf.Max(pos, leftCameraBoundary), rightCameraBoundary);
		}
	}

	// Returns a door object if one can be accessed from the specified position, otherwise returns NULL
	public Data_Door getDoorAtPos(float pos) {
		foreach(Data_Door d in me.DOORS.Values) { // Loop through all the doors in this room
			if(Mathf.Abs(d.atPos) < me.rightWalkBoundary// Ignore side doors
			   && Mathf.Abs(d.atPos - pos) < MARGIN_DOOR_ENTRANCE) {
				return d;
			}
		}
		return null;
	}

	// Returns a door object if there is one on the specified edge of the room, otherwise returns NULL
	private Data_Door getSideDoor(bool Left) { // "Left = true" means "left edge", "false" means "right edge"
		foreach(Data_Door d in me.DOORS.Values) { // Loop through all the doors in this room
			if(Mathf.Abs(d.atPos) > me.rightWalkBoundary) { // The door must be beyond the margins
				if((d.atPos < 0.0f && Left) || (d.atPos > 0.0f && !Left)) { // The door must also be on the specified side
					return d;
				}
			}
		}
		return null;
	}
	// These are just human-readable wrappers for the above:
	public Data_Door getDoorOnTheLeft() {
		return getSideDoor(true);
	}

	public Data_Door getDoorOnTheRight() {
		return getSideDoor(false);
	}

	// Updates the size of the DangerIndicator sprite
	public void updateDangerIndicator(double dangerLevel) {
		if(DangerIndicator != null && !double.IsNaN(dangerLevel)) {
			float scaleFactor = 0.1f + 0.9f * (float)Math.Max(0.0, Math.Min(1.0, dangerLevel));
			DangerIndicator.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
		}
	}
}
