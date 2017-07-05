using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSoundsMonster : Control_AnimationSounds {

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
		if(AttackSound != null && AttackSound.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			AttackSound.Play();
		}
	}

	public void monsterBreatheIn() {
		if(BreathIn != null && BreathIn.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			BreathIn.Play();
		}
	}

	public void monsterBreatheOut() {
		if(BreathOut != null && BreathOut.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			BreathOut.Play();
		}
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
}
