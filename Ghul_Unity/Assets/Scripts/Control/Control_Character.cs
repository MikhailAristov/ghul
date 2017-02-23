using UnityEngine;
using System;
using System.Collections;

public class Control_Character : MonoBehaviour {

	[NonSerialized]
	private Data_GameState GS;
	[NonSerialized]
	private Environment_Room currentEnvironment;
	[NonSerialized]
	private Data_Character me;

	// General movement settings
	private float VERTICAL_ROOM_SPACING;
	private float WALKING_SPEED;
	private float RUNNING_SPEED;
	private float DOOR_TRANSITION_DURATION;

	// This function transitions the character through a door
	private IEnumerator goThroughTheDoor(Data_Door door) {
		Data_Room currentRoom = me.isIn;
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
		me.updatePosition(destinationRoom, newValidPosition);
		currentEnvironment = me.isIn.env;
		resetReactionTime();

		// Move character sprite
		Vector3 targetPosition = new Vector3(newValidPosition, destinationRoom.INDEX * VERTICAL_ROOM_SPACING);
		transform.Translate(targetPosition - transform.position);

		// Increase number of door uses. Needs door spawn IDs, not door IDs.
		int spawn1Index = -1;
		int spawn2Index = -1;
		Data_Door iteratorDoor;
		// Find the corresponding door spawn IDs.
		for (int i = 0; i < currentRoom.countAllDoorSpawns; i++) {
			iteratorDoor = currentRoom.getDoorAtSpawn(i);
			if (iteratorDoor != null) {
				if (iteratorDoor.INDEX == door.INDEX) {
					spawn1Index = GS.HOUSE_GRAPH.ABSTRACT_ROOMS[currentRoom.INDEX].DOOR_SPAWNS.Values[i].INDEX; // Should work, if room ID and vertex ID really are the same...
					break;
				}
			}
		}
		for (int i = 0; i < destinationRoom.countAllDoorSpawns; i++) {
			iteratorDoor = destinationRoom.getDoorAtSpawn(i);
			if (iteratorDoor != null) {
				if (iteratorDoor.INDEX == destinationDoor.INDEX) {
					spawn2Index = GS.HOUSE_GRAPH.ABSTRACT_ROOMS[destinationRoom.INDEX].DOOR_SPAWNS.Values[i].INDEX; // Should work, if room ID and vertex ID really are the same...
					break;
				}
			}
		}
		if (spawn1Index != -1 && spawn2Index != -1) {
			GS.HOUSE_GRAPH.DOOR_SPAWNS[spawn1Index].increaseNumUses();
			GS.HOUSE_GRAPH.DOOR_SPAWNS[spawn2Index].increaseNumUses();
		} else {
			Debug.Log("Cannot find door spawn ID for at least one of these doors: " + door.INDEX + "," + destinationDoor.INDEX + ". Just got spawn IDs " + spawn1Index + ", " + spawn2Index);
		}

		// Fade back in
		cameraFadeIn(DOOR_TRANSITION_DURATION / 2);

		// Make noise at the original door's location
		makeNoise(Control_Sound.NOISE_TYPE_DOOR, door.pos);

		// Trigger an autosave upon changing locations
		Data_GameState.saveToDisk(GS);
	}
	// Dummy functions to be implemented 
	protected void activateCooldown(float duration) {}
	protected void cameraFadeOut(float duration) {}
	protected void cameraFadeIn(float duration) {}
	protected void resetReactionTime() {}
	protected void makeNoise(int type, Data_Position atPos) {}
}
