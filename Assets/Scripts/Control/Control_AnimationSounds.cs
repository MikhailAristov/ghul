using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Control_AnimationSounds : MonoBehaviour {

	public AudioSource SteppingSound;
	public AudioSource AttackSound;
	public AudioSource ZappingSound;
	public AudioSource MonsterBreathIn;
	public AudioSource MonsterBreathOut;
	public AudioSource MonsterFootDrag;
	public AudioSource ToniDeathSound;
	public bool CheckDistanceToCamera;
	public Control_Camera MainCameraControl;
	public Control_GameState GameStateControl;

	private static List<AudioClip> walkingSounds;
	private static int walkingSoundsCount;
	private static List<AudioClip> runningSounds;
	private static int runningSoundsCount;
	private static List<AudioClip> monsterWalkingSounds;
	private static int monsterWalkingSoundsCount;
	private static List<AudioClip> monsterDraggingSounds;
	private static int monsterDraggingSoundsCount;

	private const float walkingSoundVolume = 0.5f;
	private const float runningSoundVolume = 1f;

	private bool mainCameraCanHearMe {
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
		if(monsterWalkingSounds == null) {
			monsterWalkingSounds = new List<AudioClip>(Resources.LoadAll("Monster/SteppingSounds", typeof(AudioClip)).Cast<AudioClip>());
			monsterWalkingSoundsCount = monsterWalkingSounds.Count;
		}
		if(monsterDraggingSounds == null) {
			monsterDraggingSounds = new List<AudioClip>(Resources.LoadAll("Monster/FootDraggingSounds", typeof(AudioClip)).Cast<AudioClip>());
			monsterDraggingSoundsCount = monsterDraggingSounds.Count;
		}
	}

	void FixedUpdate() {
		// Update the stereo pan
		if(mainCameraCanHearMe) {
			float stereoPan = getHorizontalSoundPan(MainCameraControl.transform.position.x - transform.position.x);
			SteppingSound.panStereo = stereoPan;
			if(AttackSound != null) {
				AttackSound.panStereo = stereoPan;
				MonsterBreathIn.panStereo = stereoPan;
				MonsterBreathOut.panStereo = stereoPan;
				MonsterFootDrag.panStereo = stereoPan;
			}
			if(ZappingSound != null) {
				ZappingSound.panStereo = stereoPan;
			}
			if(ToniDeathSound != null) {
				ToniDeathSound.panStereo = stereoPan;
			}
		}
	}

	public void makeRandomWalkingStepNoise() {
		playSteppingSound(walkingSounds[UnityEngine.Random.Range(0, walkingSoundsCount)], walkingSoundVolume);
	}

	public void makeRandomRunningStepNoise() {
		playSteppingSound(runningSounds[UnityEngine.Random.Range(0, runningSoundsCount)], runningSoundVolume);
	}

	private void playSteppingSound(AudioClip sound, float volume) {
		if(SteppingSound != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			SteppingSound.Stop();
			SteppingSound.clip = sound;
			SteppingSound.volume = volume;
			SteppingSound.Play();
		}
	}

	public void playAttackSound() {
		if(AttackSound != null && AttackSound.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			AttackSound.Play();
		}
	}

	public void playZappingSound() {
		if(ZappingSound != null && ZappingSound.clip != null) {
			ZappingSound.Play();
		}
	}

	public void monsterBreatheIn() {
		if(MonsterBreathIn != null && MonsterBreathIn.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			MonsterBreathIn.Play();
		}
	}

	public void monsterBreatheOut() {
		if(MonsterBreathOut != null && MonsterBreathOut.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			MonsterBreathOut.Play();
		}
	}

	public void makeRandomMonsterSteppingNoise() {
		playSteppingSound(monsterWalkingSounds[UnityEngine.Random.Range(0, monsterWalkingSoundsCount)], 1f);
	}

	public void makeRandomMonsterDraggingNoise() {
		if(MonsterFootDrag != null && !MonsterFootDrag.isPlaying && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			MonsterFootDrag.clip = monsterDraggingSounds[UnityEngine.Random.Range(0, monsterDraggingSoundsCount)];
			MonsterFootDrag.Play();
		}
	}

	public void playToniDeath() {
		if(ToniDeathSound != null && ToniDeathSound.clip != null && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			ToniDeathSound.Play();
		}
	}

	// Returns the appropriate 2D sound panning value for a horizontal distance to the main camera
	public static float getHorizontalSoundPan(float horizontalDistToCamera) {
		float result = Mathf.Atan2(-horizontalDistToCamera, -Camera.main.transform.position.z) / Mathf.PI * 2f;
		return Mathf.Clamp(result, -1f, 1f);
	}
}
