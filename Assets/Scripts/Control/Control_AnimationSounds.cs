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

	protected void FixedUpdate() {
		if(!AudioListener.pause && mainCameraCanHearMe) {
			stereoPan = getHorizontalSoundPan(MainCameraControl.transform.position.x - transform.position.x);
			updateStereoPan(SteppingSound);
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
}
