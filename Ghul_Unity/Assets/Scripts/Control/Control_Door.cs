using UnityEngine;
using System.Collections;

public class Control_Door : MonoBehaviour {

	public GameObject ClosedSprite;
	public GameObject OpenSprite;

	private string currentState;
	private float timeUntilClosing;

	private float doorOpenDuration;
	private float doorOpenCheckFrequency;

	// Use this for initialization
	void Start () {
		currentState = "CLOSED";
		OpenSprite.SetActive(false);
		doorOpenDuration = Global_Settings.read("DOOR_OPEN_DURATION");
		doorOpenCheckFrequency = doorOpenDuration / 10f;
	}

	// Opens the door if it's closed, keeps it open longer otherwise
	public void open() {
		timeUntilClosing = doorOpenDuration;
		if(currentState != "OPEN") {
			ClosedSprite.SetActive(false);
			OpenSprite.SetActive(true);
			currentState = "OPEN";
			StartCoroutine(waitForClosure());
		}
		// If the door is already open, it just stays so for longer
	}

	// Closes the door after it has been open long enough
	private IEnumerator waitForClosure() {
		while(timeUntilClosing > 0f) {
			yield return new WaitForSeconds(doorOpenCheckFrequency);
			timeUntilClosing -= doorOpenCheckFrequency;
		}
		OpenSprite.SetActive(false);
		ClosedSprite.SetActive(true);
		currentState = "CLOSED";
	}
}
