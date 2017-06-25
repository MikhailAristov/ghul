using UnityEngine;
using System.Collections;

// This is the main controller for the music/jukebox subsystem
public class Control_Music : MonoBehaviour {

	private Data_GameState GS;
	private Data_PlayerCharacter TONI;
	private Data_Monster MONSTER;

	private const float TRACK_MUTING_DURATION = 1f;
	private const float TRACK_UNMUTING_DURATION = 5f;

	private float proximityTrackVolumeFactor;
	private const float minProximityTrackVolumeFactor = 0.1f;
	private const float maxProximityTrackVolumeFactor = 1f;

	private int RITUAL_ITEMS_REQUIRED;

	// Jukebox references
	private bool allPaused;
	private bool allMuted;
	private int currentTrackID;

	public AudioSource EncounterJingle;
	public AudioSource ItemPlacementJingle;
	public Control_MusicTrack[] MainTrackList;
	public AudioSource EndgameTrack;

	void Awake() {
		allPaused = false;
		allMuted = false;
		RITUAL_ITEMS_REQUIRED = Global_Settings.readInt("RITUAL_ITEMS_REQUIRED");
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
				if(currentTrackID != GS.numItemsPlaced && GS.numItemsPlaced < RITUAL_ITEMS_REQUIRED) {
					switchTracks(currentTrackID, GS.numItemsPlaced);
					currentTrackID = GS.numItemsPlaced;
				}
				MainTrackList[currentTrackID].updateProximityFactor(proximityTrackVolumeFactor);
			}
			break;
		case(Control_GameState.STATE_MONSTER_PHASE):
			if(MONSTER.worldModel.hasMetToniSinceLastMilestone) {
				if(EndgameTrack.mute) {
					EndgameTrack.Play();
					EndgameTrack.mute = false;
				}
				if(EndgameTrack.volume < 0.999f) {
					EndgameTrack.volume = Mathf.Lerp(EndgameTrack.volume, 1f, 0.001f);
				}
			}
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
			// Mute the proximity track as long as Toni and the monster have not met
			if(!MONSTER.worldModel.hasMetToniSinceLastMilestone) {
				proximityTrackVolumeFactor = 0.0001f;
			} else {
				// Otherwise, set it to depend on the current distance between them
				switch(GS.separationBetweenTwoRooms[TONI.isIn.INDEX, MONSTER.isIn.INDEX]) {
				case 0:
					proximityTrackVolumeFactor = 1f;
					break;
				case 1:
					proximityTrackVolumeFactor = 0.216f;
					break;
				case 2:
					proximityTrackVolumeFactor = 0.01f;
					break;
				default:
					proximityTrackVolumeFactor = 0.0f;
					break;
				}
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

	// Switches the next track to play
	private void switchTracks(int oldTrack, int newTrack) {
		// Mute the previous track
		MainTrackList[oldTrack].muteTrack(TRACK_MUTING_DURATION);
		// Play the item jingle
		if(!ItemPlacementJingle.isPlaying) {
			ItemPlacementJingle.Play();
		}
		// Unmute the next track after the jingle stops playing
		float delay = ItemPlacementJingle.clip.length / 2f;
		MainTrackList[newTrack].unmuteTrack(duration: TRACK_UNMUTING_DURATION, delay: delay, restart: true);
	}

	// Play the horror jingle upon the first meeting with a monster
	public void playEncounterJingle() {
		if(GS.OVERALL_STATE == Control_GameState.STATE_COLLECTION_PHASE) {
			if(!EncounterJingle.isPlaying) {
				EncounterJingle.Play();
			}
			if(MainTrackList[currentTrackID].muted) {
				MainTrackList[currentTrackID].unmuteTrack(duration: TRACK_UNMUTING_DURATION, delay: Global_Settings.read("ENCOUNTER_JINGLE_DURATION"), restart: true);
			}
		}
	}

	// Pause and unpause all audio sources in the jukebox
	private void pauseAll() {
		if(!allPaused) {
			EncounterJingle.Pause();
			foreach(Control_MusicTrack track in MainTrackList) {
				track.pause();
			}
			EndgameTrack.Pause();
			allPaused = true;
		}
	}

	private void unpauseAll() {
		if(allPaused) {
			EncounterJingle.UnPause();
			foreach(Control_MusicTrack track in MainTrackList) {
				track.unpause();
			}
			EndgameTrack.UnPause();
			allPaused = false;
		}
	}

	private void muteAll() {
		if(!allMuted) {
			foreach(Control_MusicTrack track in MainTrackList) {
				track.muteTrack(0);
			}
			EndgameTrack.mute = true;
			allMuted = true;
		}
	}
}
