using UnityEngine;
using System.Collections;

public class Control_Door : MonoBehaviour {

	public GameObject ClosedSprite;
	private AudioSource CloseSound;
	public GameObject OpenSprite;
	private AudioSource OpenSound;

	public const int STATE_CLOSED = 0;
	public const int STATE_OPEN = 1;

	private int currentState;
	private float timeUntilClosing;

	private float doorOpenDuration;
	private float doorOpenCheckFrequency;
	private float verticalHearingThreshold;

	// Use this for initialization
	void Start () {
		currentState = STATE_CLOSED;
		OpenSprite.SetActive(false);

		doorOpenDuration = Global_Settings.read("DOOR_OPEN_DURATION");
		doorOpenCheckFrequency = doorOpenDuration / 10f;
		verticalHearingThreshold = Global_Settings.read("SCREEN_SIZE_VERTICAL") / 10f;

		CloseSound = ClosedSprite.GetComponent<AudioSource>();
		OpenSound = OpenSprite.GetComponent<AudioSource>();
	}

	// Opens the door if it's closed, keeps it open longer otherwise
	public void open() {
		timeUntilClosing = doorOpenDuration;
		if(currentState != STATE_OPEN) {
			ClosedSprite.SetActive(false);
			OpenSprite.SetActive(true);
			playSound(OpenSound);
			currentState = STATE_OPEN;
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
		playSound(CloseSound);
		currentState = STATE_CLOSED;
	}

	// Play the specified sound if the main camera (i.e. Toni) is within the current room
	private void playSound(AudioSource sound) {
		float verticalDistanceToCamera = Mathf.Abs(transform.position.y - Camera.main.transform.position.y);
		if(sound != null && verticalDistanceToCamera <= verticalHearingThreshold) {
			sound.mute = false;
			sound.Play();
		}
	}
}
