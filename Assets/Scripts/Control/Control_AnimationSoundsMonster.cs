using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Control_AnimationSoundsMonster : Control_AnimationSounds {

	public Control_GameState gameStateControl;

	public AudioSource AttackSound;
	public AudioSource MonsterVoice;
	public AudioSource BreathIn;
	public AudioSource BreathOut;
	public AudioSource FootDragSound;

	public AudioSource ArmHiss;
	public AudioSource ArmRattle;
	public AudioSource ArmSwing;

	private static List<AudioClip> monsterGroans;
	private static List<AudioClip> monsterLaughs;

	private static List<AudioClip> walkingSounds;
	private static List<AudioClip> footDraggingSounds;

	private static List<AudioClip> rightArmHisses;
	private static List<AudioClip> rightArmRattles;
	private static List<AudioClip> rightArmSwingSounds;
	private static List<AudioClip> leftArmHisses;
	private static List<AudioClip> leftArmRattles;
	private static List<AudioClip> leftArmSwingSounds;

	private int lastLaughedInChapter;

	void Awake() {
		// Define all sounds
		loadAudioClips(ref monsterGroans, path: "Monster/Groans");
		loadAudioClips(ref monsterLaughs, path: "Monster/Laughs");
		loadAudioClips(ref walkingSounds, path: "Monster/SteppingSounds");
		loadAudioClips(ref footDraggingSounds, path: "Monster/FootDraggingSounds");
		// And the snake arm sounds, too...
		loadAudioClips(ref rightArmHisses, path: "Monster/ArmRight/Hiss");
		loadAudioClips(ref rightArmRattles, path: "Monster/ArmRight/Rattle");
		loadAudioClips(ref rightArmSwingSounds, path: "Monster/ArmRight/Swing");
		loadAudioClips(ref leftArmHisses, path: "Monster/ArmLeft/Hiss");
		loadAudioClips(ref leftArmRattles, path: "Monster/ArmLeft/Rattle");
		loadAudioClips(ref leftArmSwingSounds, path: "Monster/ArmLeft/Swing");
		// For the monster voice
		lastLaughedInChapter = 0;
	}

	protected new void FixedUpdate() {
		base.FixedUpdate();
		// Update the stereo pan
		if(!AudioListener.pause) {
			updateStereoPan(AttackSound);
			updateStereoPan(MonsterVoice);
			updateStereoPan(BreathIn);
			updateStereoPan(BreathOut);
			updateStereoPan(FootDragSound);
		}
	}

	public void playAttackSound() {
		checkAndPlay(ref AttackSound);
	}

	public void monsterBreatheIn() {
		checkAndPlay(ref BreathIn);
	}

	public void monsterBreatheOut() {
		checkAndPlay(ref BreathOut);
	}

	public void makeRandomMonsterSteppingNoise() {
		setRandomAndPlay(ref SteppingSound, ref walkingSounds);
	}

	public void makeRandomMonsterDraggingNoise() {
		setRandomAndPlay(ref FootDragSound, ref footDraggingSounds);
	}

	public void playRandomVoiceSnippet() {
		if(MonsterVoice != null && !MonsterVoice.isPlaying && (!CheckDistanceToCamera || mainCameraCanHearMe)) {
			if(lastLaughedInChapter != gameStateControl.currentChapter) {
				MonsterVoice.clip = pickRandomClip(ref monsterLaughs);
				lastLaughedInChapter = gameStateControl.currentChapter;
			} else {
				MonsterVoice.clip = pickRandomClip(ref monsterGroans);
			}
			MonsterVoice.Play();
		}
	}

	public void makeRandomRightArmHiss() {
		setRandomAndPlay(ref ArmHiss, ref rightArmHisses, 0.5f);
	}

	public void makeRandomLeftArmHiss() {
		setRandomAndPlay(ref ArmHiss, ref leftArmHisses, 0.5f);
	}

	public void makeRandomRightArmRattle() {
		setRandomAndPlay(ref ArmRattle, ref rightArmRattles);
	}

	public void makeRandomLeftArmRattle() {
		setRandomAndPlay(ref ArmRattle, ref leftArmRattles);
	}

	public void makeRandomRightArmSwingSound() {
		setRandomAndPlay(ref ArmSwing, ref rightArmSwingSounds);
	}

	public void makeRandomLeftArmSwingSound() {
		setRandomAndPlay(ref ArmSwing, ref leftArmSwingSounds);
	}
}
