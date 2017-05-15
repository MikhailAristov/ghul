using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_Door : MonoBehaviour {

	public GameObject ClosedSprite;
	public GameObject OpenSprite;
	private AudioSource knobSound;
	private AudioSource creakSound;

	public const int STATE_CLOSED = 0;
	public const int STATE_OPEN = 1;

	private const int SOUND_TYPE_CLOSE = 0;
	private const int SOUND_TYPE_OPEN = 1;

	private const float CREAKING_SOUND_PROBABILITY = 0.15f;

	private List<AudioClip> closingSounds;
	private List<AudioClip> openingSounds;
	private List<AudioClip> creakingSounds;

	public int currentState;
	private float timeUntilClosing;

	private float doorOpenDuration;
	private float doorOpenCheckFrequency;
	private float verticalHearingThreshold;

	// Use this for initialization
	void Awake() {
		doorOpenDuration = Global_Settings.read("DOOR_OPEN_DURATION");
		doorOpenCheckFrequency = doorOpenDuration / 10f;
		verticalHearingThreshold = Global_Settings.read("SCREEN_SIZE_VERTICAL") / 10f;

		knobSound = GetComponents<AudioSource>()[0];
		creakSound = GetComponents<AudioSource>()[1];

		// Define all sounds
		closingSounds = new List<AudioClip>(Resources.LoadAll("Doors/ClosingSounds", typeof(AudioClip)).Cast<AudioClip>());
		openingSounds = new List<AudioClip>(Resources.LoadAll("Doors/OpeningSounds", typeof(AudioClip)).Cast<AudioClip>());
		creakingSounds = new List<AudioClip>(Resources.LoadAll("Doors/CreakingSounds", typeof(AudioClip)).Cast<AudioClip>());
	}

	void Start() {
		OpenSprite.SetActive(currentState == STATE_OPEN);
		ClosedSprite.SetActive(currentState == STATE_CLOSED);
	}

	// Opens the door if it's closed, keeps it open longer otherwise
	public void open(bool silently = false, bool hold = false) {
		// If the "hold" flag is specified, the door stays open for much longer (or until someone goes through it)
		timeUntilClosing = hold ? doorOpenDuration * 10 : doorOpenDuration;
		if(currentState != STATE_OPEN) {
			ClosedSprite.SetActive(false);
			OpenSprite.SetActive(true);
			if(!silently) {
				playSound(SOUND_TYPE_OPEN);
			}
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
		playSound(SOUND_TYPE_CLOSE);
		currentState = STATE_CLOSED;
	}

	// Play the specified sound if the main camera (i.e. Toni) is within the current room
	private void playSound(int soundType) {
		if(Mathf.Abs(transform.position.y - Camera.main.transform.position.y) > verticalHearingThreshold) {
			return;
		}

		// Play a random sound of the given type
		switch(soundType) {
		case SOUND_TYPE_CLOSE:
			knobSound.clip = closingSounds[UnityEngine.Random.Range(0, closingSounds.Count)];
			knobSound.Play();
			break;
		case SOUND_TYPE_OPEN:
			// Always play an opening sound
			knobSound.clip = openingSounds[UnityEngine.Random.Range(0, openingSounds.Count)];
			knobSound.Play();
			// Also randomply play the creaking sound
			if(UnityEngine.Random.Range(0f, 1f) < CREAKING_SOUND_PROBABILITY && !creakSound.isPlaying) {
				creakSound.clip = creakingSounds[UnityEngine.Random.Range(0, creakingSounds.Count)];
				creakSound.Play();
			}
			break;
		default:
			return;
		}
	}
}
