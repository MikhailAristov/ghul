using UnityEngine;
using System;
using System.Collections;

public abstract class Control_Character : MonoBehaviour {

	[NonSerialized]
	protected Data_GameState GS;
	[NonSerialized]
	protected Environment_Room currentEnvironment;

	protected abstract Data_Character getMe();

	public AudioSource AttackSound;

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
	protected bool goingThroughADoor;
	protected bool attackAnimationPlaying;
	private float cumulativeAttackDuration;
	private Data_Position positionAtTheLastTimeStep;

	public bool isGoingThroughADoor {
		get { return goingThroughADoor; }
		private set { return; }
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

			// Snap the character sprite to the pixel grid when not moving
			if(getMe().currentVelocityAbsolute > ANIM_MIN_SPEED_FOR_WALKING) {
				spriteIsAlignedToGrid = false;
			} else if(!spriteIsAlignedToGrid) {
				transform.Translate(new Vector2(Data_Position.snapToGrid(getMe().atPos) - getMe().atPos, 0));
				spriteIsAlignedToGrid = true;
			}

			// If the character is attacking, count up the attack duration
			if(attackAnimationPlaying) {
				cumulativeAttackDuration += Time.fixedDeltaTime;
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

	public abstract void setSpriteFlip(bool state);

	protected abstract bool canRun();

	protected abstract void updateStamina(bool isRunning);

	protected abstract void regainStamina();

	protected abstract void makeWalkingNoise(float walkedDistance, int type, Data_Position atPos);

	// This function transitions the character through a door
	protected IEnumerator goThroughTheDoor(Data_Door door) {
		Data_Door destinationDoor = door.connectsTo;
		Data_Room destinationRoom = destinationDoor.isIn;

		// Doors cannot be walked through until the transformation is over
		// (except for doors leading into the ritual room, so the monster can teleport there)
		// or if the door is currently being held shut by the monster
		if((GS.OVERALL_STATE == Control_GameState.STATE_TRANSFORMATION && destinationRoom.INDEX != RITUAL_ROOM_INDEX) ||
			door.state == Data_Door.STATE_HELD) {
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
	// Dummy functions to be implemented
	public abstract void activateCooldown(float duration);

	protected abstract void failedDoorTransitionHook(Data_Door doorTaken);

	protected abstract void preDoorTransitionHook(Data_Door doorTaken);

	protected abstract void preRoomLeavingHook(Data_Door doorTaken);

	protected abstract void postDoorTransitionHook(Data_Door doorTaken);

	// Play out the attack animation
	// Animation automatically cancels out if the attacker moves
	// targetPos is separated from target because the player (as a monster) can attack even without seeing the monster
	protected IEnumerator playAttackAnimation(float targetPos, Data_Character target) {
		attackAnimationPlaying = true;
		cumulativeAttackDuration = 0;
		Data_Position attackOrigin = getMe().pos.clone();
		bool attackIsCanceledByMoving = false;
		// Flip the sprite if necessary
		if(GS.monsterSeesToni) {
			setSpriteFlip(targetPos < attackOrigin.X);
		}
		float attackPoint = attackOrigin.X + Math.Sign(targetPos - attackOrigin.X) * ATTACK_RANGE;
		Debug.Log(getMe() + " attacks from " + getMe().pos + " to " + attackPoint + " at T+" + Time.timeSinceLevelLoad);
		// PHASE 1: Attack
		startAttackAnimation();
		while(cumulativeAttackDuration < ATTACK_DURATION) {
			// If the attacker moves from the original spot, immediately cancel the attack
			if(getMe().isIn.INDEX != attackOrigin.RoomId || Math.Abs(getMe().atPos - attackOrigin.X) > ATTACK_MARGIN) {
				Debug.LogWarning(getMe() + " moved, attack canceled!");
				attackIsCanceledByMoving = true;
				break;
			}
			yield return new WaitForSeconds(1f / 60f);
		}
		stopAttackAnimation();
		Debug.Log(getMe() + " completes attack in " + cumulativeAttackDuration + " s, " + target + " was at " + target.atPos);
		// PHASE 2: Resolve
		if(!attackIsCanceledByMoving && !target.isInvulnerable &&
		   getMe().isIn == target.isIn && Math.Abs(target.atPos - attackPoint) <= ATTACK_MARGIN) {
			target.getControl().getHit();
			postKillHook();
		}
		// PHASE 3: Cooldown
		yield return new WaitForSeconds(ATTACK_COOLDOWN); // Can't attack immediately after attacking, even if it was canceled (prevents spam)
		attackAnimationPlaying = false;
	}

	public abstract void getHit();

	protected abstract void postKillHook();

	protected abstract void startAttackAnimation();

	protected abstract void stopAttackAnimation();
}
