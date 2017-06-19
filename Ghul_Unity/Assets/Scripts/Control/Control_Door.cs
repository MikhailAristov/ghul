using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_Door : MonoBehaviour {

	[NonSerialized]
	private Data_Door me;

	public GameObject ClosedSprite;
	public GameObject OpenSprite;
	private AudioSource knobSound;
	private AudioSource creakSound;

	private const int SOUND_TYPE_CLOSE = 0;
	private const int SOUND_TYPE_OPEN = 1;
	private const int SOUND_TYPE_RATTLE = 2;

	private const float CREAKING_SOUND_PROBABILITY = 0.15f;

	private static List<AudioClip> closingSounds;
	private static List<AudioClip> openingSounds;
	private static List<AudioClip> creakingSounds;
	private static List<AudioClip> rattlingSounds;
	private static int lastCreakingSound;

	private float timeUntilClosing;
	private float doorOpenDuration;
	private float doorOpenCheckFrequency;
	private float timeUntilLettingGo;
	private const float doorHeldDuration = 0.1f;
	private float verticalHearingThreshold;

	// Returns whether the door is currently being held open (to guide Monster Toni)
	public bool isHeldOpen {
		get { return (me.state == Data_Door.STATE_OPEN && timeUntilClosing > doorOpenDuration); }
	}

	// Use this for initialization
	void Awake() {
		doorOpenDuration = Global_Settings.read("DOOR_OPEN_DURATION");
		doorOpenCheckFrequency = doorOpenDuration / 10f;
		verticalHearingThreshold = Global_Settings.read("SCREEN_SIZE_VERTICAL") / 10f;

		knobSound = GetComponents<AudioSource>()[0];
		creakSound = GetComponents<AudioSource>()[1];

		// Define all sounds
		if(closingSounds == null) {
			closingSounds = new List<AudioClip>(Resources.LoadAll("Doors/ClosingSounds", typeof(AudioClip)).Cast<AudioClip>());
		}
		if(openingSounds == null) {
			openingSounds = new List<AudioClip>(Resources.LoadAll("Doors/OpeningSounds", typeof(AudioClip)).Cast<AudioClip>());
		}
		if(creakingSounds == null) {
			creakingSounds = new List<AudioClip>(Resources.LoadAll("Doors/CreakingSounds", typeof(AudioClip)).Cast<AudioClip>());
			lastCreakingSound = -1;
		}
		if(rattlingSounds == null) {
			rattlingSounds = new List<AudioClip>(Resources.LoadAll("Doors/RattlingSounds", typeof(AudioClip)).Cast<AudioClip>());
		}
	}

	void FixedUpdate() {
		// If the door is being held, check whether it timed out
		if(me != null && me.connectsTo.state == Data_Door.STATE_HELD) {
			timeUntilLettingGo -= Time.fixedDeltaTime;
			if(timeUntilLettingGo < 0) {
				me.connectsTo.state = Data_Door.STATE_CLOSED;
			}
		}
	}

	// Links the control object to the data object
	public void loadGameState(Data_Door d) {
		this.me = d;
		OpenSprite.SetActive(me.state == Data_Door.STATE_OPEN);
		ClosedSprite.SetActive(me.state != Data_Door.STATE_OPEN);
	}

	// Opens the door if it's closed, keeps it open longer otherwise
	public void open(bool silently = false, bool holdOpen = false, bool forceCreak = false) {
		// If the "hold" flag is specified, the door stays open for much longer (or until someone goes through it)
		timeUntilClosing = holdOpen ? doorOpenDuration * 10 : doorOpenDuration;
		if(me.state != Data_Door.STATE_OPEN) {
			ClosedSprite.SetActive(false);
			OpenSprite.SetActive(true);
			if(!silently) {
				playSound(SOUND_TYPE_OPEN, forceCreak);
			}
			me.state = Data_Door.STATE_OPEN;
			StartCoroutine(waitForClosure());
			// Also, open the door connected to you on the other side
			me.connectsTo.control.open(silently, holdOpen, forceCreak);
		}
		// If the door is already open, it just stays so for longer
	}

	// Forces the door shut immediately
	public void forceClose(bool silently = false) {
		if(me.state != Data_Door.STATE_CLOSED) {
			timeUntilClosing = 0;
			OpenSprite.SetActive(false);
			ClosedSprite.SetActive(true);
			if(!silently) {
				playSound(SOUND_TYPE_CLOSE);
			}
			me.state = Data_Door.STATE_CLOSED;
			me.connectsTo.control.forceClose(silently);
		}
	}

	// Forcibly holds the door shut
	public void hold() {
		// If the door is still open, force it shut
		if(me.state == Data_Door.STATE_OPEN) {
			forceClose();
		}
		// Now that it is closed, set the held flag for the OTHER side
		if(me.connectsTo.state == Data_Door.STATE_CLOSED) {
			me.connectsTo.state = Data_Door.STATE_HELD;
			Debug.Log("holding door " + me);
		}
		// If it is already being held and this function is called again,
		// extend the duration until it is let go
		if(me.connectsTo.state == Data_Door.STATE_HELD) {
			timeUntilLettingGo = doorHeldDuration;
		}
	}

	// Rattles the door when it cannot be opened
	public void rattleDoorknob() {
		if(me.state != Data_Door.STATE_OPEN) {
			playSound(SOUND_TYPE_RATTLE);
		}
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
		me.state = Data_Door.STATE_CLOSED;
	}

	// Play the specified sound if the main camera (i.e. Toni) is within the current room
	private void playSound(int soundType, bool forceCreak = false) {
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
			if((UnityEngine.Random.Range(0f, 1f) < CREAKING_SOUND_PROBABILITY || forceCreak) && !creakSound.isPlaying) {
				int randomCreakingSound = 0;
				// Pick a random creaking sounds that has not been used before
				do {
					randomCreakingSound = UnityEngine.Random.Range(0, creakingSounds.Count);
				} while(randomCreakingSound == lastCreakingSound);
				lastCreakingSound = randomCreakingSound;
				creakSound.clip = creakingSounds[randomCreakingSound];
				creakSound.Play();
			}
			break;
		case SOUND_TYPE_RATTLE:
			knobSound.clip = rattlingSounds[UnityEngine.Random.Range(0, rattlingSounds.Count)];
			knobSound.Play();
			break;
		default:
			return;
		}
	}
}
