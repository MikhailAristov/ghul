using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// This is the main controller for the sound subsystem
public class Control_Sound : MonoBehaviour {

	private Data_GameState GS;

	private Data_PlayerCharacter CHARA;
	private Data_Monster MONSTER;
	private float DIST;

	public Text MonsterDistanceText;
	public AudioSource AmbientNoise;

	// Noise types:
	public const int NOISE_TYPE_WALK = 0;
	public const int NOISE_TYPE_RUN = 1;
	public const int NOISE_TYPE_DOOR = 2;
	public const int NOISE_TYPE_ITEM = 3;
	public const int NOISE_TYPE_ZAP = 4;
	// TODO public const int NOISE_TYPE_HIDE = 5;

	// Noise loudness values:
	public const float NOISE_INAUDIBLE = 0.2f; // This is the effective volume at which the noise is no longer transmitted to the monster
	public const float NOISE_VOL_QUIET = 1.0f;
	public const float NOISE_VOL_MEDIUM = 2.0f;
	public const float NOISE_VOL_LOUD = 4.0f;
	// TODO: Fine-tune the values above

	void Start() {
		AmbientNoise.volume = 0.0f;
	}

	// Loads the game state
	public void loadGameState(Data_GameState gameState) {
		GS = gameState;
		CHARA = gameState.getToni();
		MONSTER = gameState.getMonster();
		// (Re)Start sub-controllers
		StopCoroutine("generateRandomNoises");
		StopCoroutine("controlAmbientMusic");
		StopCoroutine("calculateCharaMonsterDistance");
		StartCoroutine("calculateCharaMonsterDistance");
		StartCoroutine("controlAmbientMusic");
		StartCoroutine("generateRandomNoises");
	}

	// Continuously recalculates the proximity of monster to chara
	// Because this is computationally intensive, this function is called less often than Update()
	private IEnumerator calculateCharaMonsterDistance() {
		while(true) {
			if(!GS.SUSPENDED) {
				DIST = GS.getDistance(CHARA.pos, MONSTER.pos);
				if(Debug.isDebugBuild) {
					MonsterDistanceText.text = string.Format("{0:0.0} m", DIST);
				}
			}
			yield return new WaitForSeconds(0.2f);
		}
	}

	// Continuously regulates the volume of the ambient creepy music based on the proximity of monster to chara
	private IEnumerator controlAmbientMusic() {
		while(true) {
			if (!GS.RITUAL_PERFORMED) {
				float targetVolume = GS.SUSPENDED ? 0.0f : Mathf.Min(1.0f, Mathf.Exp(-0.3f * DIST));
				AmbientNoise.volume = Mathf.Lerp(AmbientNoise.volume, targetVolume, 0.6f);
			}
			yield return new WaitForSeconds(0.1f);
		}
	}

	// Whenever chara makes noise, this is where it is "heard"
	public void makeNoise(int noiseType, Data_Position origin) {
		// Calculate the intrinsic loudness of the noise
		float loudness = getInitialLoudness(noiseType);
		// Transmit the noise to the monster
		transmitNoiseToMonster(loudness, origin);
	}

	// Returns the initial noise loudness by type
	public static float getInitialLoudness(int noiseType) {
		float result;
		switch(noiseType) {
		case NOISE_TYPE_WALK:
		default:
			result = NOISE_VOL_QUIET;
			break;
		case NOISE_TYPE_DOOR:
		case NOISE_TYPE_ITEM:
			result = NOISE_VOL_MEDIUM;
			break;
		case NOISE_TYPE_RUN:
		case NOISE_TYPE_ZAP:
			result = NOISE_VOL_LOUD;
			result = NOISE_VOL_LOUD;
			break;
		}
		return result;
	}

	// Transmits the noise to the monster
	private void transmitNoiseToMonster(float loudness, Data_Position origin) {
		// If the monster is in the same room as the noise, ignore this call
		if(MONSTER.isIn.INDEX == origin.RoomId) { return; }
		// Otherwise, find the door through which the monster would hear the noise
		float distance = float.MaxValue; int doorId = -1;
		foreach(Data_Door d in MONSTER.isIn.DOORS) {
			float tentativeDist = GS.getDistance(d, origin);
			if(tentativeDist < distance) {
				distance = tentativeDist;
				doorId = d.INDEX;
			}
		}
		// Propagate the loudness across this distance
		// See: https://en.wikipedia.org/wiki/Inverse-square_law
		float effectiveLoudness = loudness / (distance * distance);
		// Inform the monster of the noise if it is above the cutoff threshold
		if(effectiveLoudness > NOISE_INAUDIBLE) {
			MONSTER.control.hearNoise(GS.getDoorByIndex(doorId), effectiveLoudness);
		}
	}

	// Generate random noises to confuse the monster
	private IEnumerator generateRandomNoises() {
		while(true) {
			// First, wait a random interval
			float nextDelay = UnityEngine.Random.Range(0.5f, 5.0f);
			yield return new WaitForSeconds(nextDelay);
			// Only proceed if the game isn't suspended
			if(!GS.SUSPENDED) {
				// Generate a random fake signal from "somewhere" within the house
				float loudness = getInitialLoudness(UnityEngine.Random.Range(NOISE_TYPE_WALK, NOISE_TYPE_ZAP));
				Data_Room originRoom = GS.getRandomRoom(false);
				Data_Position origin = new Data_Position(originRoom.INDEX, UnityEngine.Random.Range(originRoom.leftWalkBoundary, originRoom.rightWalkBoundary));
				// Transmit the signal to the monster (as long as it's not in the same room)
				transmitNoiseToMonster(loudness, origin);
			}
		}
	}
}
