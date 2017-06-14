using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSounds : MonoBehaviour {

	public AudioSource SteppingSound;
	public AudioSource AttackSound;
	public AudioSource ZappingSound;
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
		if(AttackSound != null && AttackSound.clip != null) {
			AttackSound.Play();
		}
	}

	public void playZappingSound() {
		if(ZappingSound != null && ZappingSound.clip != null) {
			ZappingSound.Play();
		}
	}
}
