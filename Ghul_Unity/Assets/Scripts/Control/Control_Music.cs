using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// This is the main controller for the music/jukebox subsystem
public class Control_Music : MonoBehaviour {

	private Data_GameState GS;

	private Data_PlayerCharacter TONI;

	public AudioSource AmbientNoise;
	private float SUICIDLE_DURATION;

	// Jukebox references
	private bool allPaused;
	public AudioSource EncounterJingle;
	public GameObject[] MainTrackList;
	public GameObject EndgameTrack;
	private int currentTrack;

	void Awake() {
		SUICIDLE_DURATION = Global_Settings.read("SUICIDLE_DURATION");
		allPaused = false;
		currentTrack = -1;
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
				//playCollectionPhaseTrack(GS.numItemsPlaced);
			}
			//moduleMonsterProximitySound();
			//AmbientNoise.transform.localPosition = Vector3.Lerp(AmbientNoise.transform.localPosition, targetAudioSourcePosition, 0.01f);
			break;
		case(Control_GameState.STATE_MONSTER_PHASE):
			// By contrast, for the monster phase we place the audio source at the camera and regulate the volume
			if(AmbientNoise.transform.localPosition.z < 0) {
				AmbientNoise.transform.localPosition = new Vector3(0, 0, 0);
			}
			float targetVolume = Mathf.Min(1.0f, TONI.timeWithoutAction / SUICIDLE_DURATION);
			AmbientNoise.volume = Mathf.Lerp(AmbientNoise.volume, targetVolume, 0.01f);
			break;
		default:
			// Ambient music should not play during other phases
			AmbientNoise.volume = 0;
			break;
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

	// Pause and unpause all audio sources in the jukebox
	private void pauseAll() {
		if(!allPaused) {
			AmbientNoise.Pause();
			EncounterJingle.Pause();
			allPaused = true;
		}
	}

	private void unpauseAll() {
		if(allPaused) {
			AmbientNoise.UnPause();
			EncounterJingle.UnPause();
			allPaused = false;
		}
	}
}
