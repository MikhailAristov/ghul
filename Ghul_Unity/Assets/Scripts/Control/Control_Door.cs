using UnityEngine;
using System.Collections;

public class Control_Door : MonoBehaviour {

	public GameObject ClosedSprite;
	public GameObject OpenSprite;

	public string CurrentState;
	public float TimeUntilClosing;

	private float doorOpenDuration;
	private float doorOpenCheckFrequency;

	// Use this for initialization
	void Start () {
		CurrentState = "CLOSED";
		OpenSprite.SetActive(false);
		doorOpenDuration = Global_Settings.read("DOOR_OPEN_DURATION");
		doorOpenCheckFrequency = doorOpenDuration / 10f;
	}

	// Opens the door if it's closed, keeps it open longer otherwise
	public void open() {
		TimeUntilClosing = doorOpenDuration;
		if(CurrentState != "OPEN") {
			ClosedSprite.SetActive(false);
			OpenSprite.SetActive(true);
			CurrentState = "OPEN";
			StartCoroutine(waitForClosure());
		}
		// If the door is already open, it just stays so for longer
	}

	// Closes the door after it has been open long enough
	private IEnumerator waitForClosure() {
		while(TimeUntilClosing > 0f) {
			yield return new WaitForSeconds(doorOpenCheckFrequency);
			TimeUntilClosing -= doorOpenCheckFrequency;
		}
		OpenSprite.SetActive(false);
		ClosedSprite.SetActive(true);
		CurrentState = "CLOSED";
	}
}
