using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Control_AnimationSoundsHuman : Control_AnimationSounds {

	public AudioSource ZappingSound;
	public GameObject BreathingSound;
	private List<AudioSource> BreathSounds;
	public AudioSource DeathSound;

	private static List<AudioClip> walkingSounds;
	private static List<AudioClip> runningSounds;
	private static List<AudioClip> breathingSounds;
	private static List<AudioClip> heavyBreathingSounds;

	private const float walkingSoundVolume = 0.5f;
	private const float runningSoundVolume = 1f;

	void Awake() {
		// Define all sounds
		loadAudioClips(ref walkingSounds, path: "Toni/WalkSounds");
		loadAudioClips(ref runningSounds, path: "Toni/RunSounds");
		loadAudioClips(ref breathingSounds, path: "Toni/Breathing");
		loadAudioClips(ref heavyBreathingSounds, path: "Toni/HeavyBreathing");
		// Internal references
		BreathSounds = (BreathingSound != null) ? new List<AudioSource>(BreathingSound.GetComponents<AudioSource>()) : new List<AudioSource>();
	}

	protected new void FixedUpdate() {
		base.FixedUpdate();
		// Update the stereo pan
		if(!AudioListener.pause) {
			updateStereoPan(ZappingSound);
			foreach(AudioSource src in BreathSounds) {
				updateStereoPan(src);
			}
			updateStereoPan(DeathSound);
		}
	}

	public void makeRandomWalkingStepNoise() {
		setRandomAndPlay(ref SteppingSound, ref walkingSounds, walkingSoundVolume);
	}

	public void makeRandomRunningStepNoise() {
		setRandomAndPlay(ref SteppingSound, ref runningSounds, runningSoundVolume);
	}

	public void makeRandomBreathingNoise() {
		playBreathingSound(pickRandomClip(ref breathingSounds));
	}

	public void makeRandomHeavyBreathingNoise() {
		playBreathingSound(pickRandomClip(ref heavyBreathingSounds));
	}

	private void playBreathingSound(AudioClip sound) {
		foreach(AudioSource src in BreathSounds) {
			if(src != null && !src.isPlaying) {
				src.clip = sound;
				src.Play();
				break;
			}
		}
	}

	public void playZappingSound() {
		checkAndPlay(ref ZappingSound);
	}

	public void playToniDeath() {
		checkAndPlay(ref DeathSound);
	}
}
