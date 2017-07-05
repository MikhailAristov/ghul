using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSoundsMonster : Control_AnimationSounds {

	public Control_GameState gameStateControl;

	public AudioSource AttackSound;
	public AudioSource MonsterVoice;
	public AudioSource BreathIn;
	public AudioSource BreathOut;
	public AudioSource FootDragSound;

	private static List<AudioClip> monsterGroans;
	private static List<AudioClip> monsterLaughs;
	private static List<AudioClip> walkingSounds;
	private static List<AudioClip> footDraggingSounds;

	void Awake() {
		// Define all sounds
		if(monsterGroans == null) {
			monsterGroans = loadAudioClips("Monster/Groans");
		}
		if(monsterLaughs == null) {
			monsterLaughs = loadAudioClips("Monster/Laughs");
		}
		if(walkingSounds == null) {
			walkingSounds = loadAudioClips("Monster/SteppingSounds");
		}
		if(footDraggingSounds == null) {
			footDraggingSounds = loadAudioClips("Monster/FootDraggingSounds");
		}
	}

	protected new void FixedUpdate() {
		base.FixedUpdate();
		// Update the stereo pan
		if(AttackSound != null) {
			AttackSound.panStereo = stereoPan;
			BreathIn.panStereo = stereoPan;
			BreathOut.panStereo = stereoPan;
			FootDragSound.panStereo = stereoPan;
		}
	}

	public void playAttackSound() {
		checkAndPlay(ref AttackSound);
	}

	public void monsterBreatheIn() {
		checkAndPlay(ref BreathIn);
	}

	public void monsterBreatheOut() {
		checkAndPlay(ref BreathOut);
	}

	public void makeRandomMonsterSteppingNoise() {
		playSteppingSound(pickRandomClip(ref walkingSounds), 1f);
	}

	public void makeRandomMonsterDraggingNoise() {
		if(FootDragSound != null && !FootDragSound.isPlaying && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			FootDragSound.clip = pickRandomClip(ref footDraggingSounds);
			FootDragSound.Play();
		}
	}

	public void playRandomVoiceSnippet() {
		if(MonsterVoice != null && !MonsterVoice.isPlaying && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			MonsterVoice.clip = gameStateControl.monsterMetToni ? pickRandomClip(ref monsterGroans) : pickRandomClip(ref monsterLaughs);
			MonsterVoice.Play();
		}
	}
}
