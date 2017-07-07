using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSounds : MonoBehaviour {

	public GameObject GenericSounds;
	protected List<AudioSource> GenericAudioTracks;

	protected static Dictionary<string, List<AudioClip>> AudioDatabase;

	public bool CheckDistanceToCamera;
	public Control_Camera MainCameraControl;
	protected float stereoPan;

	protected void FixedUpdate() {
		if(!AudioListener.pause && MainCameraControl.canHearObject(gameObject)) {
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
			playSound(soundToPlay, intendedVolume);
		}
	}

	// Required: object reference to the audio clip to play
	// Optional: float = sound volume (if set to greater than 0)
	public void playRandomFromPath(AnimationEvent e) {
		if(GenericSounds != null && e.stringParameter.Length > 0 && (!CheckDistanceToCamera || MainCameraControl.canHearObject(gameObject))) {
			// Extract the reference to the sound clip to be played, and the intended volume, if specified
			string pathKey = e.stringParameter;
			float intendedVolume = (e.floatParameter > 0) ? Mathf.Clamp01(e.floatParameter) : 1f;
			playRandomFromPath(pathKey, intendedVolume);
		}
	}

	protected void playRandomFromPath(string pathKey, float vol) {
		// Initialize the audio database if necessary
		if(AudioDatabase == null) {
			AudioDatabase = new Dictionary<string, List<AudioClip>>();
		}
		// Obtain the clip list for the key
		List<AudioClip> clipList;
		if(!AudioDatabase.TryGetValue(pathKey, out clipList)) {
			clipList = new List<AudioClip>(Resources.LoadAll(pathKey, typeof(AudioClip)).Cast<AudioClip>());
			AudioDatabase.Add(pathKey, clipList);
		}
		// Pick a random clip from the list and play it
		playSound(clipList[UnityEngine.Random.Range(0, clipList.Count)], vol);
	}

	protected void playSound(AudioClip snd, float vol) {
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

	// Wrapper for legacy compatibility
	public void playCutsceneSound(AnimationEvent e) {
		playSound(e);
	}

	public void takeScreenshot() {
		Control_Persistence.takeScreenshot();
	}

	// Returns the appropriate 2D sound panning value for a horizontal distance to the main camera
	public static float getHorizontalSoundPan(float horizontalDistToCamera) {
		float result = Mathf.Atan2(-horizontalDistToCamera, -Camera.main.transform.position.z) / Mathf.PI * 2f;
		return Mathf.Clamp(result, -1f, 1f);
	}
}
