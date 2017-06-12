using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_WalkingSounds : MonoBehaviour {

	public bool CheckDistanceToCamera;
	public AudioSource SteppingSound;
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
		playSound(walkingSounds[UnityEngine.Random.Range(0, walkingSoundsCount)], walkingSoundVolume);
	}

	public void makeRandomRunningStepNoise() {
		playSound(runningSounds[UnityEngine.Random.Range(0, runningSoundsCount)], runningSoundVolume);
	}

	private void playSound(AudioClip sound, float volume) {
		if(SteppingSound != null && (!CheckDistanceToCamera || MainCameraControl.canSeeObject(gameObject, -100f))) {
			SteppingSound.Stop();
			SteppingSound.clip = sound;
			SteppingSound.volume = volume;
			SteppingSound.Play();
		}
	}
}
