﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSounds : MonoBehaviour {

	public GameObject GenericSounds;
	protected List<AudioSource> GenericAudioTracks;

	protected static Dictionary<string, List<AudioClip>> AudioDatabase;

	// This dictionary stores the last indices of database tracks played, so they don't repeat
	protected static Dictionary<string, int> AudioDatabasePlayHistory;

	public bool CheckDistanceToCamera;
	protected Control_Camera MainCameraControl;
	protected float stereoPan;

	protected void Start() {
		MainCameraControl = Camera.main.GetComponent<Control_Camera>();
		StartCoroutine(precacheImportantSounds());
	}

	protected void FixedUpdate() {
		if(!AudioListener.pause && (!CheckDistanceToCamera || MainCameraControl.canHearObject(gameObject))) {
			stereoPan = getHorizontalSoundPan(MainCameraControl.transform.position.x - transform.position.x);
			if(GenericAudioTracks != null) {
				foreach(AudioSource src in GenericAudioTracks) {
					src.panStereo = stereoPan;
				}
			}
		}
	}

	// Required: object reference to the audio clip to play
	// Optional: float = sound volume (if set to greater than 0)
	public void playSound(AnimationEvent e) {
		if(GenericSounds != null && e.objectReferenceParameter != null && e.objectReferenceParameter.GetType() == typeof(UnityEngine.AudioClip)
			&& (!CheckDistanceToCamera || MainCameraControl.canHearObject(gameObject))) {
			// Extract the reference to the sound clip to be played, and the intended volume, if specified
			AudioClip soundToPlay = (AudioClip)e.objectReferenceParameter;
			float intendedVolume = (e.floatParameter > 0) ? Mathf.Clamp01(e.floatParameter) : 1f;
			// Play the sound
			playSoundInternal(soundToPlay, intendedVolume);
		}
	}

	// Required: object reference to the audio clip to play
	// Optional: float = sound volume (if set to greater than 0)
	public void playRandomFromPath(AnimationEvent e) {
		if(GenericSounds != null && e.stringParameter.Length > 0 && (!CheckDistanceToCamera || MainCameraControl.canHearObject(gameObject))) {
			// Extract the reference to the sound clip to be played, and the intended volume, if specified
			string pathKey = e.stringParameter;
			float intendedVolume = (e.floatParameter > 0) ? Mathf.Clamp01(e.floatParameter) : 1f;
			playRandomFromPathInternal(pathKey, intendedVolume);
		}
	}

	protected void playRandomFromPathInternal(string pathKey, float vol) {
		// Obtain the clip list for the key
		List<AudioClip> clipList = getClipList(pathKey);
		// Pick a random clip from the list that has not been played last time
		int indexToPlay;
		do {
			indexToPlay = UnityEngine.Random.Range(0, clipList.Count);
		} while(clipList.Count > 1 && indexToPlay == AudioDatabasePlayHistory[pathKey]);
		// Play the selected sound
		playSoundInternal(clipList[indexToPlay], vol);
		AudioDatabasePlayHistory[pathKey] = indexToPlay;
	}

	protected void playSoundInternal(AudioClip snd, float vol) {
		// Initialize the cutscene sound source list if necessary
		if(GenericAudioTracks == null) {
			GenericAudioTracks = new List<AudioSource>();
		}
		// Search for a free track to play the new clip on
		bool freeTrackFound = false;
		foreach(AudioSource src in GenericAudioTracks) {
			if(!src.isPlaying) {
				// Assign the clip to a free track and play it
				src.clip = snd;
				src.volume = vol;
				src.Play();
				// Exit loop
				freeTrackFound = true;
				break;
			}
		}
		// If no free track has been found, add a new audio source to the cutscene player and play it instead
		if(!freeTrackFound) {
			// Create and manage new audio source
			AudioSource newSrc = GenericSounds.AddComponent<AudioSource>();
			newSrc.bypassEffects = true;
			newSrc.playOnAwake = false;
			newSrc.loop = false;
			newSrc.panStereo = stereoPan;
			newSrc.dopplerLevel = 0;
			newSrc.spatialBlend = 0;
			GenericAudioTracks.Add(newSrc);
			// Assign the clip and play it
			newSrc.clip = snd;
			newSrc.volume = vol;
			newSrc.Play();
		}
	}

	// Just for taking pretty screenshots in the middle of an animation...
	public void takeScreenshot() {
		Control_Persistence.takeScreenshot();
	}

	// Returns the appropriate 2D sound panning value for a horizontal distance to the main camera
	public static float getHorizontalSoundPan(float horizontalDistToCamera) {
		float result = Mathf.Atan2(-horizontalDistToCamera, -Camera.main.transform.position.z) / Mathf.PI * 2f;
		return Mathf.Clamp(result, -1f, 1f);
	}

	protected List<AudioClip> getClipList(string resourcePath) {
		List<AudioClip> result;
		if(!AudioDatabase.TryGetValue(resourcePath, out result)) {
			result = new List<AudioClip>(Resources.LoadAll(resourcePath, typeof(AudioClip)).Cast<AudioClip>());
			AudioDatabase.Add(resourcePath, result);
			AudioDatabasePlayHistory.Add(resourcePath, -1);
		}
		return result;
	}

	protected IEnumerator precacheImportantSounds() {
		// Initialize the audio database if necessary
		if(AudioDatabase == null) {
			AudioDatabase = new Dictionary<string, List<AudioClip>>();
		}
		if(AudioDatabasePlayHistory == null) {
			AudioDatabasePlayHistory = new Dictionary<string, int>();
		}
		yield return null;
		getClipList("Toni/WalkSounds");
		getClipList("Toni/RunSounds");
		yield return null;
		getClipList("Toni/Breathing");
		getClipList("Toni/HeavyBreathing");
		yield return null;
		getClipList("Monster/SteppingSounds");
		getClipList("Monster/FootDraggingSounds");
		yield return null;
		getClipList("Monster/Groans");
		getClipList("Monster/Laughs");
		yield return null;
		getClipList("Monster/ArmLeft/Hiss");
		getClipList("Monster/ArmRight/Hiss");
		yield return null;
		getClipList("Monster/ArmLeft/Rattle");
		getClipList("Monster/ArmRight/Rattle");
		yield return null;
		getClipList("Monster/ArmLeft/Swing");
		getClipList("Monster/ArmRight/Swing");
	}
}
