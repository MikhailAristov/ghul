﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSounds : MonoBehaviour {

	public AudioSource SteppingSound;
	public AudioSource AttackSound;
	public AudioSource ItemPickupSound;
	public AudioSource ZappingSound;
	public AudioSource MonsterBreathIn;
	public AudioSource MonsterBreathOut;
	public AudioSource ToniDeathSound;
	public bool CheckDistanceToCamera;
	public Control_Camera MainCameraControl;

	private static List<AudioClip> walkingSounds;
	private static int walkingSoundsCount;
	private static List<AudioClip> runningSounds;
	private static int runningSoundsCount;

	private const float walkingSoundVolume = 0.5f;
	private const float runningSoundVolume = 1f;

	void Awake() {
		// Define all sounds
		if(walkingSounds == null) {
			walkingSounds = new List<AudioClip>(Resources.LoadAll("Toni/WalkSounds", typeof(AudioClip)).Cast<AudioClip>());
			walkingSoundsCount = walkingSounds.Count;
		}
		if(runningSounds == null) {
			runningSounds = new List<AudioClip>(Resources.LoadAll("Toni/RunSounds", typeof(AudioClip)).Cast<AudioClip>());
			runningSoundsCount = runningSounds.Count;
		}
	}

	public void makeRandomWalkingStepNoise() {
		playSteppingSound(walkingSounds[UnityEngine.Random.Range(0, walkingSoundsCount)], walkingSoundVolume);
	}

	public void makeRandomRunningStepNoise() {
		playSteppingSound(runningSounds[UnityEngine.Random.Range(0, runningSoundsCount)], runningSoundVolume);
	}

	private void playSteppingSound(AudioClip sound, float volume) {
		if(SteppingSound != null && (!CheckDistanceToCamera || MainCameraControl.canSeeObject(gameObject, -100f))) {
			SteppingSound.Stop();
			SteppingSound.clip = sound;
			SteppingSound.volume = volume;
			SteppingSound.Play();
		}
	}

	public void playAttackSound() {
		if(AttackSound != null && AttackSound.clip != null && (!CheckDistanceToCamera || MainCameraControl.canSeeObject(gameObject, -100f))) {
			AttackSound.Play();
		}
	}

	public void playItemPickup() {
		if(ItemPickupSound != null && ItemPickupSound.clip != null) {
			ItemPickupSound.Play();
		}
	}

	public void playZappingSound() {
		if(ZappingSound != null && ZappingSound.clip != null) {
			ZappingSound.Play();
		}
	}

	public void monsterBreatheIn() {
		if(MonsterBreathIn != null && MonsterBreathIn.clip != null && (!CheckDistanceToCamera || MainCameraControl.canSeeObject(gameObject, -100f))) {
			MonsterBreathIn.Play();
		}
	}

	public void monsterBreatheOut() {
		if(MonsterBreathOut != null && MonsterBreathOut.clip != null && (!CheckDistanceToCamera || MainCameraControl.canSeeObject(gameObject, -100f))) {
			MonsterBreathOut.Play();
		}
	}

	public void playToniDeath() {
		if(ToniDeathSound != null && ToniDeathSound.clip != null && (!CheckDistanceToCamera || MainCameraControl.canSeeObject(gameObject, -100f))) {
			ToniDeathSound.Play();
		}
	}
}
