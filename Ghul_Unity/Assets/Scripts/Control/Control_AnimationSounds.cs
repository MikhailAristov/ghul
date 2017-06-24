using UnityEngine;
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
	public Control_GameState GameStateControl;

	private static List<AudioClip> walkingSounds;
	private static int walkingSoundsCount;
	private static List<AudioClip> runningSounds;
	private static int runningSoundsCount;
	private static List<AudioClip> itemPickupSounds;
	private static int itemPickupSoundsCount;

	private const float walkingSoundVolume = 0.5f;
	private const float runningSoundVolume = 1f;

	private bool mainCameraCanSeeMe {
		get { return (MainCameraControl != null && MainCameraControl.canSeeObject(gameObject, -100f)); }
	}

	private static int currentChapter;

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
		if(itemPickupSounds == null) {
			itemPickupSounds = new List<AudioClip>(Resources.LoadAll("Toni/ItemPickupSounds", typeof(AudioClip)).Cast<AudioClip>());
			itemPickupSoundsCount = itemPickupSounds.Count;
		}
	}

	void FixedUpdate() {
		// Update the stereo pan
		if(mainCameraCanSeeMe) {
			float stereoPan = getHorizontalSoundPan(MainCameraControl.transform.position.x - transform.position.x);
			SteppingSound.panStereo = stereoPan;
			if(AttackSound != null) {
				AttackSound.panStereo = stereoPan;
				MonsterBreathIn.panStereo = stereoPan;
				MonsterBreathOut.panStereo = stereoPan;
			}
			if(ItemPickupSound != null) {
				ItemPickupSound.panStereo = stereoPan;
				ZappingSound.panStereo = stereoPan;
			}
			if(ToniDeathSound != null) {
				ToniDeathSound.panStereo = stereoPan;
			}
		}

		// Update the item pickup sound, if necessary
		if(ItemPickupSound != null && GameStateControl != null && currentChapter != GameStateControl.currentChapter) {
			ItemPickupSound.clip = itemPickupSounds[Mathf.Clamp(currentChapter - 1, 0, itemPickupSoundsCount - 1)];
			currentChapter = GameStateControl.currentChapter;
		}
	}

	public void makeRandomWalkingStepNoise() {
		playSteppingSound(walkingSounds[UnityEngine.Random.Range(0, walkingSoundsCount)], walkingSoundVolume);
	}

	public void makeRandomRunningStepNoise() {
		playSteppingSound(runningSounds[UnityEngine.Random.Range(0, runningSoundsCount)], runningSoundVolume);
	}

	private void playSteppingSound(AudioClip sound, float volume) {
		if(SteppingSound != null && (!CheckDistanceToCamera || mainCameraCanSeeMe)) {
			SteppingSound.Stop();
			SteppingSound.clip = sound;
			SteppingSound.volume = volume;
			SteppingSound.Play();
		}
	}

	public void playAttackSound() {
		if(AttackSound != null && AttackSound.clip != null && (!CheckDistanceToCamera || mainCameraCanSeeMe)) {
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
		if(MonsterBreathIn != null && MonsterBreathIn.clip != null && (!CheckDistanceToCamera || mainCameraCanSeeMe)) {
			MonsterBreathIn.Play();
		}
	}

	public void monsterBreatheOut() {
		if(MonsterBreathOut != null && MonsterBreathOut.clip != null && (!CheckDistanceToCamera || mainCameraCanSeeMe)) {
			MonsterBreathOut.Play();
		}
	}

	public void playToniDeath() {
		if(ToniDeathSound != null && ToniDeathSound.clip != null && (!CheckDistanceToCamera || mainCameraCanSeeMe)) {
			ToniDeathSound.Play();
		}
	}

	// Returns the appropriate 2D sound panning value for a horizontal distance to the main camera
	public static float getHorizontalSoundPan(float horizontalDistToCamera) {
		float result = Mathf.Atan2(-horizontalDistToCamera, -Camera.main.transform.position.z) / Mathf.PI * 2f;
		return Mathf.Clamp(result, -1f, 1f);
	}
}
