using UnityEngine;
using System;
using System.Collections;

public abstract class Control_Character : MonoBehaviour {

	[NonSerialized]
	protected Data_GameState GS;
	[NonSerialized]
	protected Environment_Room currentEnvironment;
	protected abstract Data_Character getMe();

	// General movement settings
	protected float VERTICAL_ROOM_SPACING;
	protected float WALKING_SPEED;
	protected float RUNNING_SPEED;
	protected float DOOR_TRANSITION_DURATION;

	// The functions moves the character left or right
	// Direction is negative for left, positive for right
	// Returns a door object if the character runs into it
	protected virtual Data_Door walk(float direction, bool run) {
		// Flip the sprite as necessary
		setSpriteFlip(direction < 0);

		// Determine movement speed
		float velocity = WALKING_SPEED; int noiseType = Control_Sound.NOISE_TYPE_WALK;
		if (run && canRun()) {
			velocity = RUNNING_SPEED;
			noiseType = Control_Sound.NOISE_TYPE_RUN;
			updateStamina(true);
		} else {
			updateStamina(false);
		}

		// Calculate the new position
		float displacement = direction * velocity * Time.deltaTime;
		float newPosition = transform.position.x + displacement;
		float validPosition = currentEnvironment.validatePosition(newPosition);

		// If validated position is different from the calculated position, we have reached the side of the room
		if(validPosition > newPosition) {		// moving left (negative direction) gets us through the left door
			Data_Door leftDoor = currentEnvironment.getDoorOnTheLeft();
			return leftDoor;
		}
		else if (validPosition < newPosition) {	// moving right (positive direction) gets us through the right door
			Data_Door rightDoor = currentEnvironment.getDoorOnTheRight();
			return rightDoor;
		}

		// Move the sprite to the new valid position
		float validDisplacement = validPosition - transform.position.x;
		if (Mathf.Abs(validDisplacement) > 0.0f) {
			getMe().updatePosition(validPosition);
			transform.Translate(validDisplacement, 0, 0);
		}

		// Make noise (if necessary)
		makeWalkingNoise(validDisplacement, noiseType, getMe().pos);

		return null;
	}
	protected abstract void setSpriteFlip(bool state);
	protected abstract bool canRun();
	protected abstract void updateStamina(bool isRunning);
	protected abstract void regainStamina();
	protected abstract void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos);

	// This function transitions the character through a door
	protected IEnumerator goThroughTheDoor(Data_Door door) {
		Data_Room currentRoom = getMe().isIn;
		Data_Door destinationDoor = door.connectsTo;
		Data_Room destinationRoom = destinationDoor.isIn;
		activateCooldown(DOOR_TRANSITION_DURATION);


		// Open doors
		door.gameObj.GetComponent<Control_Door>().open();
		destinationDoor.gameObj.GetComponent<Control_Door>().open();

		// Fade out and wait
		cameraFadeOut(DOOR_TRANSITION_DURATION / 2);
		yield return new WaitForSeconds(DOOR_TRANSITION_DURATION);

		// Move character within game state
		float newValidPosition = destinationRoom.env.validatePosition(destinationDoor.atPos);
		getMe().updatePosition(destinationRoom, newValidPosition);
		currentEnvironment = getMe().isIn.env;
		resetAttackStatus();

		// Move character sprite
		Vector3 targetPosition = new Vector3(newValidPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);

		updateDoorUsageStatistic(door, currentRoom, destinationDoor, destinationRoom);

		// Fade back in
		cameraFadeIn(DOOR_TRANSITION_DURATION / 2);

		// Make noise at the original door's location
		makeNoise(Control_Sound.NOISE_TYPE_DOOR, door.pos);

		// Trigger an autosave upon changing locations
		Data_GameState.saveToDisk(GS);
	}
	// Dummy functions to be implemented 
	protected abstract void activateCooldown(float duration);
	protected abstract void cameraFadeOut(float duration);
	protected abstract void cameraFadeIn(float duration);
	protected abstract void resetAttackStatus();
	protected abstract void makeNoise(int type, Data_Position atPos);
	protected abstract void updateDoorUsageStatistic(Data_Door door, Data_Room currentRoom, Data_Door destinationDoor, Data_Room destinationRoom);
}
