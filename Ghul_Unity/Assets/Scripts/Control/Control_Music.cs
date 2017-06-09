using UnityEngine;
using System.Collections;

// This is the main controller for the music/jukebox subsystem
public class Control_Music : MonoBehaviour {

	private Data_GameState GS;
	private Data_PlayerCharacter TONI;
	private Data_Monster MONSTER;

	private float SUICIDLE_DURATION;
	private const float TRACK_MUTING_DURATION = 5f;

	private const float MIN_PROXIMITY = 3f;
	private const float MAX_PROXIMITY = 30f;

	private float proximityTrackVolumeFactor;
	private const float minProximityTrackVolumeFactor = 0.1f;
	private const float maxProximityTrackVolumeFactor = 1f;

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
			if(MONSTER.worldModel.hasMetToni) {
				if(currentTrackID != GS.numItemsPlaced) {
					MainTrackList[currentTrackID].muteTrack(TRACK_MUTING_DURATION);
					MainTrackList[GS.numItemsPlaced].unmuteTrack(TRACK_MUTING_DURATION);
					currentTrackID = GS.numItemsPlaced;
				}
				MainTrackList[currentTrackID].updateProximityFactor(proximityTrackVolumeFactor);
			}
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
			switch(GS.separationBetweenTwoRooms[TONI.isIn.INDEX, MONSTER.isIn.INDEX]) {
			case 0:
				proximityTrackVolumeFactor = 1f;
				break;
			case 1:
				proximityTrackVolumeFactor = 0.8f;
				break;
			case 2:
				proximityTrackVolumeFactor = 0.6f;
				break;
			case 3:
				proximityTrackVolumeFactor = 0.4f;
				break;
			default:
				proximityTrackVolumeFactor = 0.2f;
				break;
			}
		}
	}

	// Loads the game state
	public void loadGameState(Data_GameState gameState) {
		GS = gameState;
		TONI = gameState.getToni();
		MONSTER = gameState.getMonster();
		// Initialize the proper track if still in the collection phase
		currentTrackID = GS.numItemsPlaced;
		for(int i = 0; i < MainTrackList.Length; i++) {
			if(i == currentTrackID && MONSTER.worldModel.hasMetToni &&
				GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
				MainTrackList[i].unmuteTrack(0);
			} else {
				MainTrackList[i].muteTrack(0);
			}
		}
	}

	// Play the horror jingle upon the first meeting with a monster
	public void playEncounterJingle() {
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			if(!EncounterJingle.isPlaying) {
				EncounterJingle.Play();
			}
			if(MainTrackList[currentTrackID].muted) {
				MainTrackList[currentTrackID].unmuteTrack(duration: TRACK_MUTING_DURATION, delay: Global_Settings.read("ENCOUNTER_JINGLE_DURATION"), restart: true);
			}
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
