using UnityEngine;
using System.Collections;

// This is the main controller for the music/jukebox subsystem
public class Control_Music : MonoBehaviour {

	private Data_GameState GS;
	private Data_PlayerCharacter TONI;

	private float SUICIDLE_DURATION;
	private const float TRACK_MUTING_DURATION = 5f;

	private float proximityTrackVolumeFactor;

	// Jukebox references
	private bool allPaused;
	private bool allMuted;
	private int currentTrackID;

	public AudioSource AmbientNoise;
	public AudioSource EncounterJingle;
	public Control_MusicTrack[] MainTrackList;
	public GameObject EndgameTrack;

	void Awake() {
		SUICIDLE_DURATION = Global_Settings.read("SUICIDLE_DURATION");
		allPaused = false;
		allMuted = false;
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
			if(currentTrackID != GS.numItemsPlaced) {
				MainTrackList[currentTrackID].muteTrack(TRACK_MUTING_DURATION);
				MainTrackList[GS.numItemsPlaced].unmuteTrack(TRACK_MUTING_DURATION);
				currentTrackID = GS.numItemsPlaced;
			}
			MainTrackList[currentTrackID].updateProximityFactor(proximityTrackVolumeFactor);
			break;
		case(Control_GameState.STATE_MONSTER_PHASE):
			// By contrast, for the monster phase we place the audio source at the camera and regulate the volume
			if(AmbientNoise.mute) {
				AmbientNoise.mute = false;
			}
			float targetVolume = Mathf.Min(1.0f, TONI.timeWithoutAction / SUICIDLE_DURATION);
			AmbientNoise.volume = Mathf.Lerp(AmbientNoise.volume, targetVolume, 0.01f);
			break;
		default:
			// Ambient music should not play during other phases
			if(!allMuted) {
				muteAll();
			}
			break;
		}
	}

	void FixedUpdate() {
		// Update the target volume of the proximity track
		if(GS != null && !GS.SUSPENDED && GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			proximityTrackVolumeFactor = 0.5f;
		}
	}

	// Loads the game state
	public void loadGameState(Data_GameState gameState) {
		GS = gameState;
		TONI = gameState.getToni();
		// Initialize the proper track
		currentTrackID = GS.numItemsPlaced;
		for(int i = 0; i < MainTrackList.Length; i++) {
			if(i == currentTrackID) {
				MainTrackList[i].unmuteTrack(0);
			} else {
				MainTrackList[i].muteTrack(0);
			}
		}
	}

	// Play the horror jingle upon the first meeting with a monster
	public void playEncounterJingle() {
		if(!EncounterJingle.isPlaying) {
			EncounterJingle.Play();
		}
	}

	// Pause and unpause all audio sources in the jukebox
	private void pauseAll() {
		if(!allPaused) {
			AmbientNoise.Pause();
			EncounterJingle.Pause();
			foreach(Control_MusicTrack track in MainTrackList) {
				track.pause();
			}
			// TODO Endgame track
			allPaused = true;
		}
	}

	private void unpauseAll() {
		if(allPaused) {
			AmbientNoise.UnPause();
			EncounterJingle.UnPause();
			foreach(Control_MusicTrack track in MainTrackList) {
				track.unpause();
			}
			// TODO Endgame track
			allPaused = false;
		}
	}

	private void muteAll() {
		if(!allMuted) {
			AmbientNoise.mute = true;
			foreach(Control_MusicTrack track in MainTrackList) {
				track.muteTrack(0);
			}
			allMuted = true;
		}
	}
}
