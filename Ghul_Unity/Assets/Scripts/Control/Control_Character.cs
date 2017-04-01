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
	protected int RITUAL_ROOM_INDEX;

	protected float ATTACK_RANGE;
	protected float ATTACK_MARGIN;
	protected float ATTACK_DURATION;
	protected float ATTACK_COOLDOWN;

	protected LineRenderer attackArmRenderer;
	protected bool attackAnimationPlaying;
	private float cumulativeAttackDuration;
	private Data_Position positionAtTheLastTimeStep;

	protected void FixedUpdate() {
		if(GS != null && !GS.SUSPENDED) {
			// Update character's velocity
			if(positionAtTheLastTimeStep != null && positionAtTheLastTimeStep.RoomId == getMe().pos.RoomId) {
				getMe().currentVelocity = (getMe().pos.X - positionAtTheLastTimeStep.X) / Time.fixedDeltaTime;
			} else {
				getMe().currentVelocity = 0;
			}
			positionAtTheLastTimeStep = getMe().pos.clone();

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
		float velocity = WALKING_SPEED; int noiseType = Control_Sound.NOISE_TYPE_WALK;
		if (run && canRun()) {
			velocity = RUNNING_SPEED;
			noiseType = Control_Sound.NOISE_TYPE_RUN;
			updateStamina(true);
		} else {
			updateStamina(false);
		}

		// Calculate the new position
		direction = Math.Max(-1f, Math.Min(1f, direction)); // limit direction to the allowed interval
		float displacement = direction * velocity * deltaTime;
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

		// Doors cannot be walked through until the transformation is over
		// except for doors leading into the ritual room, so the monster can teleport there
		if(GS.OVERALL_STATE == Control_GameState.STATE_TRANSFORMATION && destinationRoom.INDEX != RITUAL_ROOM_INDEX) {
			yield break;
		}

		activateCooldown(DOOR_TRANSITION_DURATION);

		// Open doors
		door.gameObj.GetComponent<Control_Door>().open();
		destinationDoor.gameObj.GetComponent<Control_Door>().open();

		// Fade out and wait
		cameraFadeOut(DOOR_TRANSITION_DURATION / 2);
		yield return new WaitForSeconds(DOOR_TRANSITION_DURATION);

		doBeforeLeavingRoom(door);

		// Move character within game state
		float newValidPosition = destinationRoom.env.validatePosition(destinationDoor.atPos);
		getMe().updatePosition(destinationRoom, newValidPosition);
		currentEnvironment = getMe().isIn.env;

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
	protected abstract void doBeforeLeavingRoom(Data_Door doorTaken);
	protected abstract void makeNoise(int type, Data_Position atPos);
	protected abstract void updateDoorUsageStatistic(Data_Door door, Data_Room currentRoom, Data_Door destinationDoor, Data_Room destinationRoom);

	// Play out the attack animation
	// Animation automatically cancels out if the attacker moves
	// targetPos is separated from target because the player (as a monster) can attack even without seeing the monster
	protected IEnumerator playAttackAnimation(float targetPos, Data_Character target) {
		attackAnimationPlaying = true;
		cumulativeAttackDuration = 0;
		float attackOrigin = getMe().atPos;
		bool attackIsCanceledByMoving = false;
		// Flip the sprite if necessary
		if(GS.monsterSeesToni) {
			setSpriteFlip(targetPos < attackOrigin);
		}
		float attackPoint = attackOrigin + Math.Sign(targetPos - attackOrigin) * ATTACK_RANGE;
		Debug.Log(getMe() + " attacks from " + getMe().pos + " to " + attackPoint + " at T+" + Time.timeSinceLevelLoad);
		// PHASE 1: Attack
		while(cumulativeAttackDuration < ATTACK_DURATION) {
			// If the attacker moves from the original spot, immediately cancel the attack
			if(Math.Abs(getMe().atPos - attackOrigin) > ATTACK_MARGIN) {
				Debug.LogWarning(getMe() + " moved, attack canceled!");
				attackIsCanceledByMoving = true;
				break;
			} else if(!GS.SUSPENDED) {
				// TODO play one frame forward
				attackArmRenderer.SetPosition(1, new Vector3((cumulativeAttackDuration / ATTACK_DURATION) * (attackPoint - attackOrigin), 0, 0));
			}
			yield return new WaitForSeconds(1f/60f);
		}
		Debug.Log(getMe() + " completes attack in " + cumulativeAttackDuration + " s, Toni was at " + GS.getToni().atPos);
		// PHASE 2: Resolve
		if(!attackIsCanceledByMoving && !target.isInvulnerable &&
			GS.monsterSeesToni && Math.Abs(target.atPos - attackPoint) <= ATTACK_MARGIN) {
			target.getControl().getHit();
			postKillHook();
		}
		// PHASE 3: Cooldown
		attackArmRenderer.SetPosition(1, new Vector2(0, 0));
		yield return new WaitForSeconds(ATTACK_COOLDOWN); // Can't attack immediately after attacking, even if it was canceled (prevents spam)
		attackAnimationPlaying = false;
	}
	public abstract void getHit();
	protected abstract void postKillHook();
}
