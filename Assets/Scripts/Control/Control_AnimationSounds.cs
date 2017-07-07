using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSounds : MonoBehaviour {

	public AudioSource SteppingSound;

	public GameObject CutsceneSound;
	protected List<AudioSource> CutsceneAudioTracks;

	public bool CheckDistanceToCamera;
	public Control_Camera MainCameraControl;
	protected float stereoPan;

	protected bool mainCameraCanHearMe {
		get { return (MainCameraControl != null && MainCameraControl.canSeeObject(gameObject, -100f)); }
	}

	protected void FixedUpdate() {
		if(!AudioListener.pause && mainCameraCanHearMe) {
			stereoPan = getHorizontalSoundPan(MainCameraControl.transform.position.x - transform.position.x);
			updateStereoPan(SteppingSound);
			if(CutsceneAudioTracks != null) {
				foreach(AudioSource src in CutsceneAudioTracks) {
					updateStereoPan(src);
				}
			}
		}
	}

	protected static void loadAudioClips(ref List<AudioClip> tgt, string path) {
		if(tgt == null) {
			tgt = new List<AudioClip>(Resources.LoadAll(path, typeof(AudioClip)).Cast<AudioClip>());
		}
	}

	protected void checkAndPlay(ref AudioSource src) {
		if(src != null && src.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			src.Play();
		}
	}

	protected void setRandomAndPlay(ref AudioSource src, ref List<AudioClip> soundList, float volume = 1f) {
		if(src != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			src.Stop();
			src.clip = pickRandomClip(ref soundList);
			src.volume = volume;
			src.Play();
		}
	}

	protected static AudioClip pickRandomClip(ref List<AudioClip> fromList) {
		return fromList[UnityEngine.Random.Range(0, fromList.Count)];
	}

	// Returns the appropriate 2D sound panning value for a horizontal distance to the main camera
	public static float getHorizontalSoundPan(float horizontalDistToCamera) {
		float result = Mathf.Atan2(-horizontalDistToCamera, -Camera.main.transform.position.z) / Mathf.PI * 2f;
		return Mathf.Clamp(result, -1f, 1f);
	}

	protected void updateStereoPan(AudioSource src) {
		if(src != null) {
			src.panStereo = stereoPan;
		}
	}

	public void takeScreenshot() {
		Control_Persistence.takeScreenshot();
	}

	// Requires a parameter: object reference to the audio clip to play
	// Optional parameter: float = sound volume (if set to greater than 0)
	public void playCutsceneSound(AnimationEvent e) {
		if(CutsceneSound != null && e.objectReferenceParameter != null && e.objectReferenceParameter.GetType() == typeof(UnityEngine.AudioClip)) {
			// Initialize the cutscene sound source list if necessary
			if(CutsceneAudioTracks == null) {
				CutsceneAudioTracks = new List<AudioSource>();
			}
			// Extract the reference to the sound clip to be played, and the intended volume, if specified
			AudioClip soundToPlay = (AudioClip)e.objectReferenceParameter;
			float volume = (e.floatParameter > 0) ? Mathf.Clamp01(e.floatParameter) : 1f;
			// Search for a free track to play the new clip on
			bool freeTrackFound = false;
			foreach(AudioSource src in CutsceneAudioTracks) {
				if(!src.isPlaying) {
					// Assign the clip to a free track and play it
					src.clip = soundToPlay;
					src.volume = volume;
					src.Play();
					// Exit loop
					freeTrackFound = true;
					break;
				}
			}
			// If no free track has been found, add a new audio source to the cutscene player and play it instea
			if(!freeTrackFound) {
				// Create and manage new audio source
				AudioSource newSrc = CutsceneSound.AddComponent<AudioSource>();
				newSrc.bypassEffects = true;
				newSrc.panStereo = stereoPan;
				newSrc.dopplerLevel = 0;
				newSrc.spatialBlend = 0;
				CutsceneAudioTracks.Add(newSrc);
				// Assign the clip and play it
				newSrc.clip = soundToPlay;
				newSrc.volume = volume;
				newSrc.Play();
			}
		}
	}
}
