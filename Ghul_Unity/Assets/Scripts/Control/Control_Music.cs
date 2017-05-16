using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// This is the main controller for the music/jukebox subsystem
public class Control_Music : MonoBehaviour {

	private Data_GameState GS;
	private Data_PlayerCharacter TONI;

	private float SUICIDLE_DURATION;
	private const float TRACK_MUTING_DURATION = 5f;

	private float proximityTrackVolumeTarget;

	// Jukebox references
	private bool allPaused;
	public AudioSource AmbientNoise;
	public AudioSource EncounterJingle;
	public GameObject[] MainTrackList;
	public GameObject EndgameTrack;
	private int currentTrack;
	private AudioSource currentProximityTrack;

	void Awake() {
		SUICIDLE_DURATION = Global_Settings.read("SUICIDLE_DURATION");
		allPaused = false;
		currentTrack = -1;
	}

	void Start() {
		AmbientNoise.mute = false;
		AmbientNoise.volume = 0;
	}

	void Update() {
		if(GS == null || GS.SUSPENDED) {
			pauseAll();
			return;
		} else {
			unpauseAll();
		}

		switch(GS.OVERALL_STATE) {
		case(Control_GameState.STATE_COLLECTION_PHASE):
			if(currentTrack != GS.numItemsPlaced) {
				StartCoroutine(muteTrack(currentTrack, TRACK_MUTING_DURATION));
				StartCoroutine(unmuteTrack(GS.numItemsPlaced, TRACK_MUTING_DURATION));
				currentTrack = GS.numItemsPlaced;
				currentProximityTrack = MainTrackList[currentTrack].GetComponents<AudioSource>()[1];
			}
			modulateMonsterProximitySound();
			break;
		case(Control_GameState.STATE_MONSTER_PHASE):
			// By contrast, for the monster phase we place the audio source at the camera and regulate the volume
			float targetVolume = Mathf.Min(1.0f, TONI.timeWithoutAction / SUICIDLE_DURATION);
			AmbientNoise.volume = Mathf.Lerp(AmbientNoise.volume, targetVolume, 0.01f);
			break;
		default:
			// Ambient music should not play during other phases
			AmbientNoise.volume = 0;
			break;
		}
	}

	void FixedUpdate() {
		// Update the target volume of the proximity track
		if(GS != null && !GS.SUSPENDED && GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			proximityTrackVolumeTarget = 0.5f;
		}
	}

	// Loads the game state
	public void loadGameState(Data_GameState gameState) {
		GS = gameState;
		TONI = gameState.getToni();
	}

	// Play the horror jingle upon the first meeting with a monster
	public void playEncounterJingle() {
		if(!EncounterJingle.isPlaying) {
			EncounterJingle.Play();
		}
	}

	// Lerps the volume and the pitch of the current monster proximity track
	private void modulateMonsterProximitySound() {
		currentProximityTrack.volume = Mathf.Lerp(currentProximityTrack.volume, proximityTrackVolumeTarget, 0.001f);
		currentProximityTrack.pitch = Mathf.Lerp(currentProximityTrack.pitch, (1f - proximityTrackVolumeTarget), 0.001f);
	}

	// Pauses and unpauses a given track
	private void pauseTrack(int id) {
		foreach(AudioSource audioSrc in MainTrackList[id].GetComponents<AudioSource>()) {
			audioSrc.Pause();
		}
	}

	private void unpauseTrack(int id) {
		foreach(AudioSource audioSrc in MainTrackList[id].GetComponents<AudioSource>()) {
			audioSrc.UnPause();
		}
	}

	// Gradually mutes and unmutes a given track over the specified time
	private IEnumerator muteTrack(int id, float duration) {
		// If duration is not positive, mute the track immediately
		if(duration <= 0) {
			foreach(AudioSource audioSrc in MainTrackList[id].GetComponents<AudioSource>()) {
				audioSrc.volume = 0;
			}
			yield break;
		}
		// TODO Otherwise, do so gradually
	}

	private IEnumerator unmuteTrack(int id, float duration) {
		// If duration is not positive, unmute the track immediately
		if(duration <= 0) {
			foreach(AudioSource audioSrc in MainTrackList[id].GetComponents<AudioSource>()) {
				audioSrc.volume = 1f;
			}
			yield break;
		}
		// TODO Otherwise, do so gradually
	}

	// Pause and unpause all audio sources in the jukebox
	private void pauseAll() {
		if(!allPaused) {
			AmbientNoise.Pause();
			EncounterJingle.Pause();
			for(int i = 0; i < MainTrackList.Length; i++) {
				pauseTrack(i);
			}
			// TODO Endgame track
			allPaused = true;
		}
	}

	private void unpauseAll() {
		if(allPaused) {
			AmbientNoise.UnPause();
			EncounterJingle.UnPause();
			for(int i = 0; i < MainTrackList.Length; i++) {
				unpauseTrack(i);
			}
			// TODO Endgame track
			allPaused = false;
		}
	}
}
