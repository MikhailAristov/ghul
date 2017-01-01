using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// This is the main controller for the sound subsystem
public class Control_Sound : MonoBehaviour {

	private Data_GameState GS;

	private Data_PlayerCharacter CHARA;
	private Data_Monster MONSTER;
	private float DIST;

	public bool ShowMonsterDistance;
	public Text MonsterDistanceText;
	public AudioSource AmbientNoise;

	// Loads the game state
	public void loadGameState(Data_GameState gameState) {
		GS = gameState;
		CHARA = gameState.getCHARA();
		MONSTER = gameState.getMonster();
	}

	// Update is called once per frame
	void Update () {
		if (GS == null || GS.SUSPENDED) {  // Don't do anything if the game state is not loaded yet or suspended
			AmbientNoise.volume = 0.0f; // Just turn off the ambient noise...
			return; 
		}

		// First, calculate the distance between chara and monster
		DIST = GS.getDistance(CHARA.pos, MONSTER.pos);
		if(Debug.isDebugBuild && ShowMonsterDistance) { // For debugging only
			MonsterDistanceText.text = string.Format("{0:0.0} m", DIST);
		}
		// TODO the volume of the noise needs tweaking
		AmbientNoise.volume = Mathf.Min(1.0f, Mathf.Exp(-0.3f * DIST));
	}
}
