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

	// Jukebox references
	private bool allMuted;
	private int currentTrackID;
	private bool playChaseMusic;
	private bool tensionHigh;

	public AudioSource EncounterJingle;
	public AudioSource ItemJinglePlayer;
	public AudioClip ItemPickupJingle;
	public AudioClip[] ItemPlacementJingles;
	public Control_MusicTrack[] MainTrackList;
	public AudioSource EndgameTrack;

	void Awake() {
		allMuted = false;
		playChaseMusic = false;
		tensionHigh = false;
	}

	void Update() {
		if(GS == null || GS.SUSPENDED) {
			return;
		}

		switch(GS.OVERALL_STATE) {
		case(Control_GameState.STATE_COLLECTION_PHASE):
			if(MONSTER.worldModel.hasMetToni) {
				if(currentTrackID != GS.numItemsPlaced) {
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
		// Unmute the next track after the jingle stops playing
		float delay = ItemJinglePlayer.clip.length / 2f;
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

	private void muteAll() {
		if(!allMuted) {
			foreach(Control_MusicTrack track in MainTrackList) {
				track.muteTrack(TRACK_MUTING_DURATION);
			}
			EndgameTrack.mute = true;
			allMuted = true;
		}
	}

	// Plays the pickup jingle for the specified item, optionally with a delay
	public void playItemPickupJingle(int forItem, float delay = 0) {
		if(ItemPickupJingle != null) {
			playItemJingle(ItemPickupJingle, delay);
		}
	}

	// Plays the placement jingle for the specified item, optionally with a delay
	public void playItemPlacementJingle(int forItem, float delay = 0) {
		if(forItem >= 0 && forItem < ItemPlacementJingles.Length) {
			playItemJingle(ItemPlacementJingles[forItem], delay);
		}
	}

	// Plays the specified jingle with a specified delay
	private void playItemJingle(AudioClip jingle, float delay) {
		// This function only works during the collection phase
		if(GS.OVERALL_STATE != Control_GameState.STATE_COLLECTION_PHASE) {
			return;
		}
		// If the item player is still playing for some reason, stop it
		if(ItemJinglePlayer.isPlaying) {
			ItemJinglePlayer.Stop();
		}
		// Set the requested item pickup jingle and play it
		ItemJinglePlayer.clip = jingle;
		ItemJinglePlayer.PlayDelayed(Mathf.Max(0, delay));
	}

	// For when the monster chases Toni
	public void startPlayingChaseTrack() {
		if(!playChaseMusic) {
			MainTrackList[currentTrackID].setBeingChased(true);
			playChaseMusic = true;
		}
	}

	public void stopPlayingChaseTrack() {
		if(playChaseMusic) {
			MainTrackList[currentTrackID].setBeingChased(false);
			playChaseMusic = false;
		}
	}

	public void setTensionHigh() {
		if(!tensionHigh) {
			MainTrackList[currentTrackID].rampTensionUp(true);
			tensionHigh = true;
		}
	}

	public void setTensionLow() {
		if(tensionHigh) {
			MainTrackList[currentTrackID].rampTensionUp(false);
			tensionHigh = false;
		}
	}
}
