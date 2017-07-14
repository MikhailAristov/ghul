using UnityEngine;
using System.Collections;

// This is the main controller for the (virtual) noise subsystem
public class Control_Noise : MonoBehaviour {

	private Data_GameState GS;
	private Data_Monster MONSTER;
	private IEnumerator noiseCoroutine;

	// Random noise settings
	public const float RANDOM_NOISE_MIN_DELAY = 0.5f;
	public const float RANDOM_NOISE_MAX_DELAY = 5.0f;

	// Noise types:
	public const int NOISE_TYPE_NONE = 0;
	public const int NOISE_TYPE_WALK = 1;
	public const int NOISE_TYPE_RUN = 2;
	public const int NOISE_TYPE_DOOR = 3;
	public const int NOISE_TYPE_ITEM = 4;
	public const int NOISE_TYPE_ZAP = 5;

	// Noise loudness values:
	// This is the effective volume at which the noise is no longer transmitted to the monster
	public const float NOISE_INAUDIBLE = 0.2f;
	// Quiet noises are audible over the estimated average minimum distance between rooms (empirically: 7.5 m)
	public const float NOISE_VOL_QUIET = NOISE_INAUDIBLE * 7.5f * 7.5f;
	// Medium noises are audible over the estimated average distance between rooms (empirically: 11.5 m)
	public const float NOISE_VOL_MEDIUM = NOISE_INAUDIBLE * 11.5f * 11.5f;
	// Loud noises are audible over the estimated maximum possible distance between rooms (empirically: 30 m)
	public const float NOISE_VOL_LOUD = NOISE_INAUDIBLE * 30f * 30f;

	void Awake() {
		noiseCoroutine = generateRandomNoises();
	}

	// Loads the game state
	public void loadGameState(Data_GameState gameState) {
		GS = gameState;
		MONSTER = gameState.getMonster();
		// (Re)Start sub-controllers
		StopCoroutine(noiseCoroutine);
		StartCoroutine(noiseCoroutine);
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
		switch(noiseType) {
		default:
		case NOISE_TYPE_NONE:
			return 0;
		case NOISE_TYPE_WALK:
			return NOISE_VOL_QUIET;
		case NOISE_TYPE_DOOR:
		case NOISE_TYPE_ITEM:
			return NOISE_VOL_MEDIUM;
		case NOISE_TYPE_RUN:
		case NOISE_TYPE_ZAP:
			return NOISE_VOL_LOUD;
		}
	}

	// Transmits the noise to the monster
	private void transmitNoiseToMonster(float loudness, Data_Position origin) {
		// If the monster is in the same room as the noise, ignore this call
		if(MONSTER.isIn.INDEX == origin.RoomId) { return; }
		// Otherwise, find the door through which the monster would hear the noise
		float distance = float.MaxValue; int doorId = -1;
		foreach(Data_Door d in MONSTER.isIn.DOORS.Values) {
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
			MONSTER.perception.hearNoise(GS.getDoorByIndex(doorId), effectiveLoudness);
		}
	}

	// Generate random noises to confuse the monster
	private IEnumerator generateRandomNoises() {
		while(true) {
			// First, wait a random interval
			float nextDelay = UnityEngine.Random.Range(RANDOM_NOISE_MIN_DELAY, RANDOM_NOISE_MAX_DELAY);
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
