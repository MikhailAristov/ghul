using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSoundsHuman : Control_AnimationSounds {

	public AudioSource ZappingSound;
	public GameObject BreathingSound;
	private List<AudioSource> BreathSounds;
	public AudioSource DeathSound;
	public AudioSource Transformation;

	private static List<AudioClip> walkingSounds;
	private static List<AudioClip> runningSounds;
	private static List<AudioClip> breathingSounds;
	private static List<AudioClip> heavyBreathingSounds;

	private const float walkingSoundVolume = 0.5f;
	private const float runningSoundVolume = 1f;

	void Awake() {
		// Define all sounds
		if(walkingSounds == null) {
			walkingSounds = loadAudioClips("Toni/WalkSounds");
		}
		if(runningSounds == null) {
			runningSounds = loadAudioClips("Toni/RunSounds");
		}
		if(breathingSounds == null) {
			breathingSounds = loadAudioClips("Toni/Breathing");
		}
		if(heavyBreathingSounds == null) {
			heavyBreathingSounds = loadAudioClips("Toni/HeavyBreathing");
		}
		// Internal references
		BreathSounds = (BreathingSound != null) ? new List<AudioSource>(BreathingSound.GetComponents<AudioSource>()) : new List<AudioSource>();
	}

	protected new void FixedUpdate() {
		base.FixedUpdate();
		// Update the stereo pan
		if(ZappingSound != null) {
			ZappingSound.panStereo = stereoPan;
			foreach(AudioSource src in BreathSounds) {
				src.panStereo = stereoPan;
			}
			Transformation.panStereo = stereoPan;
		}
		if(DeathSound != null) {
			DeathSound.panStereo = stereoPan;
		}
	}

	public void makeRandomWalkingStepNoise() {
		playSteppingSound(pickRandomClip(ref walkingSounds), walkingSoundVolume);
	}

	public void makeRandomRunningStepNoise() {
		playSteppingSound(pickRandomClip(ref runningSounds), runningSoundVolume);
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

	public void playToniTransformation() {
		checkAndPlay(ref Transformation);
	}
}
