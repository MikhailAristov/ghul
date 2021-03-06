﻿using UnityEngine;
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
	protected int RITUAL_ROOM_INDEX;

	protected float ATTACK_RANGE;
	protected float ATTACK_MARGIN;
	protected float ATTACK_DURATION;
	protected float ATTACK_COOLDOWN;

	protected const float ANIM_MIN_SPEED_FOR_WALKING = 0.01f;
	protected float animatorMovementSpeed;
	protected bool goingThroughADoor;
	protected bool attackAnimationPlaying;
	private Data_Position positionAtTheLastTimeStep;

	public bool isGoingThroughADoor {
		get { return goingThroughADoor; }
	}

	public bool isPlayerCharacter {
		get { return (this.GetType().Name == "Control_PlayerCharacter"); }
	}

	protected bool spriteIsAlignedToGrid;

	protected void FixedUpdate() {
		if(GS != null && !GS.SUSPENDED) {
			// Update character's velocity, and precalculate its absolute value for better performance
			if(positionAtTheLastTimeStep != null && positionAtTheLastTimeStep.RoomId == getMe().pos.RoomId) {
				getMe().currentVelocitySigned = (getMe().pos.X - positionAtTheLastTimeStep.X) / Time.fixedDeltaTime;
				getMe().currentVelocityAbsolute = Math.Abs(getMe().currentVelocitySigned);
			} else {
				getMe().currentVelocitySigned = 0;
				getMe().currentVelocityAbsolute = 0;
			}
			positionAtTheLastTimeStep = getMe().pos.clone();

			// Update animator movement speed via smoothing for low FPS
			animatorMovementSpeed = Mathf.Lerp(animatorMovementSpeed, getMe().currentVelocityAbsolute, 0.8f);

			// Snap the character sprite to the pixel grid when not moving
			if(animatorMovementSpeed > ANIM_MIN_SPEED_FOR_WALKING) {
				spriteIsAlignedToGrid = false;
			} else if(!spriteIsAlignedToGrid) {
				transform.Translate((Data_Position.snapToGrid(getMe().atPos) - getMe().atPos), 0, 0);
				spriteIsAlignedToGrid = true;
			}
		}
	}

	// The functions moves the character left or right
	// Direction is negative for left, positive for right
	// Returns a door object if the character runs into it
	protected virtual Data_Door walk(float direction, bool run, float deltaTime) {
		// Flip the sprite as necessary
		setSpriteFlip(direction < 0);

		// Determine movement speed
		float velocity = WALKING_SPEED;
		int noiseType = Control_Noise.NOISE_TYPE_WALK;
		if(run && canRun()) {
			velocity = RUNNING_SPEED;
			noiseType = Control_Noise.NOISE_TYPE_RUN;
			updateStamina(true);
		} else {
			updateStamina(false);
		}

		// Calculate the new position
		direction = Mathf.Clamp(direction, -1f, 1f); // limit direction to the allowed interval
		float displacement = direction * velocity * deltaTime;
		float newPosition = transform.position.x + displacement;
		float validPosition = currentEnvironment.validatePosition(newPosition);

		// If validated position is different from the calculated position, we have reached the side of the room
		if(validPosition > newPosition) {		// moving left (negative direction) gets us through the left door
			Data_Door leftDoor = currentEnvironment.getDoorOnTheLeft();
			return leftDoor;
		} else if(validPosition < newPosition) {	// moving right (positive direction) gets us through the right door
			Data_Door rightDoor = currentEnvironment.getDoorOnTheRight();
			return rightDoor;
		}

		// Move the sprite to the new valid position
		float validDisplacement = validPosition - transform.position.x;
		if(Mathf.Abs(validDisplacement) > 0.0f) {
			getMe().updatePosition(validPosition);
			transform.Translate(validDisplacement, 0, 0);
		}

		// Make noise (if necessary)
		makeWalkingNoise(validDisplacement, noiseType, getMe().pos);

		return null;
	}

	// Local character movement for cutscenes
	public void moveTo(float pos, bool run = false) {
		if(getMe().cooldown > 0) {
			pos += (getMe().atPos < pos) ? Data_Position.PIXEL_GRID_SIZE : -Data_Position.PIXEL_GRID_SIZE;
			StartCoroutine(moveToPosition(getMe().isIn.env.validatePosition(pos), run));
		}
	}

	protected IEnumerator moveToPosition(float pos, bool run) {
		do {
			float timestamp = Time.timeSinceLevelLoad;
			yield return null;
			walk(pos - getMe().atPos, run, Time.timeSinceLevelLoad - timestamp);
		} while(Mathf.Abs(pos - getMe().atPos) > Data_Position.PIXEL_GRID_SIZE && getMe().cooldown > 0);
	}

	public abstract void setSpriteFlip(bool state);

	protected abstract bool canRun();

	protected abstract void updateStamina(bool isRunning);

	protected abstract void regainStamina();

	protected abstract void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos);

	// This function transitions the character through a door
	protected IEnumerator goThroughTheDoor(Data_Door door) {
		Data_Door destinationDoor = door.connectsTo;
		Data_Room destinationRoom = destinationDoor.isIn;

		// Doors leading into locked-off room cannot be walked through
		// or if the door is currently being held shut by the monster
		if(door.state == Data_Door.STATE_HELD
		   || (destinationRoom.ToniCannotEnter && (isPlayerCharacter || GS.OVERALL_STATE == Control_GameState.STATE_TRANSFORMATION))) {
			door.control.forceClose();
			failedDoorTransitionHook(door);
			door.control.rattleDoorknob();
			goingThroughADoor = false;
			yield break;
		}

		float waitUntil = Time.timeSinceLevelLoad + DOOR_TRANSITION_DURATION;
		activateCooldown(DOOR_TRANSITION_DURATION);

		// Open door
		door.control.open();

		// Fade out and wait
		preDoorTransitionHook(door);
		yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		preRoomLeavingHook(door);

		// Move character within game state
		float newValidPosition = destinationRoom.env.validatePosition(destinationDoor.atPos);
		getMe().updatePosition(destinationRoom, newValidPosition);
		currentEnvironment = getMe().isIn.env;

		// Move character sprite
		Vector3 targetPosition = new Vector3(newValidPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);

		// Execute all necessary actions after passing through the door
		postDoorTransitionHook(door);
		goingThroughADoor = false;
	}

	// Set the cooldown
	public void activateCooldown(float duration) {
		getMe().cooldown = duration;
	}

	// Dummy functions to be implemented
	protected abstract void failedDoorTransitionHook(Data_Door doorTaken);

	protected abstract void preDoorTransitionHook(Data_Door doorTaken);

	protected abstract void preRoomLeavingHook(Data_Door doorTaken);

	protected abstract void postDoorTransitionHook(Data_Door doorTaken);

	// Play out the attack animation
	// Animation automatically cancels out if the attacker moves
	// targetPos is separated from target because the player (as a monster) can attack even without seeing the monster
	protected IEnumerator playAttackAnimation(float targetPos, Data_Character target) {
		attackAnimationPlaying = true;
		Data_Position attackOrigin = getMe().pos.clone();
		// Flip the sprite if necessary
		if(!isPlayerCharacter && GS.monsterSeesToni) {
			setSpriteFlip(targetPos < attackOrigin.X);
		}
		float attackPoint = attackOrigin.X + Math.Sign(targetPos - attackOrigin.X) * ATTACK_RANGE;
		// PHASE 1: Attack
		float waitUntil = Time.timeSinceLevelLoad + ATTACK_DURATION;
		startAttackAnimation();
		// Wait until either the attack plays out completely, or the attacker goes through a door, or moves at all
		yield return new WaitUntil(() => (Time.timeSinceLevelLoad > waitUntil || getMe().isIn.INDEX != attackOrigin.RoomId || Math.Abs(getMe().atPos - attackOrigin.X) > ATTACK_MARGIN));
		bool attackWassCanceled = (Time.timeSinceLevelLoad <= waitUntil);
		stopAttackAnimation();
		// PHASE 2: Resolve
		if(!attackWassCanceled && !target.isInvulnerable &&
		   getMe().isIn == target.isIn && Math.Abs(target.atPos - attackPoint) <= ATTACK_MARGIN) {
			target.getControl().getHit();
			postKillHook();
		}
		// PHASE 3: Cooldown
		waitUntil = Time.timeSinceLevelLoad + ATTACK_COOLDOWN;
		// Can't attack immediately after attacking, even if it was canceled (prevents spam)
		yield return new WaitUntil(() => (Time.timeSinceLevelLoad > waitUntil));
		attackAnimationPlaying = false;
	}

	public abstract void getHit();

	protected abstract void postKillHook();

	protected abstract void startAttackAnimation();

	protected abstract void stopAttackAnimation();
}
