using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSounds : MonoBehaviour {

	public AudioSource SteppingSound;

	public bool CheckDistanceToCamera;
	public Control_Camera MainCameraControl;
	protected float stereoPan;

	protected bool mainCameraCanHearMe {
		get { return (MainCameraControl != null && MainCameraControl.canSeeObject(gameObject, -100f)); }
	}

	protected void FixedUpdate () {
		if(mainCameraCanHearMe) {
			stereoPan = getHorizontalSoundPan(MainCameraControl.transform.position.x - transform.position.x);
			SteppingSound.panStereo = stereoPan;
		}
	}

	protected void playSteppingSound(AudioClip sound, float volume) {
		if(SteppingSound != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			SteppingSound.Stop();
			SteppingSound.clip = sound;
			SteppingSound.volume = volume;
			SteppingSound.Play();
		}
	}

	protected static List<AudioClip> loadAudioClips(string resourceLocation) {
		return new List<AudioClip>(Resources.LoadAll(resourceLocation, typeof(AudioClip)).Cast<AudioClip>());
	}

	protected static AudioClip pickRandomClip(ref List<AudioClip> fromList) {
		return fromList[UnityEngine.Random.Range(0, fromList.Count)];
	}

	// Returns the appropriate 2D sound panning value for a horizontal distance to the main camera
	public static float getHorizontalSoundPan(float horizontalDistToCamera) {
		float result = Mathf.Atan2(-horizontalDistToCamera, -Camera.main.transform.position.z) / Mathf.PI * 2f;
		return Mathf.Clamp(result, -1f, 1f);
	}
}
