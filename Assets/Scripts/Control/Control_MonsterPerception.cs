using UnityEngine;
using System;
using System.Collections;

public class Control_MonsterPerception : MonoBehaviour {

	[NonSerialized]
	private Data_Monster me;

	[NonSerialized]
	private Data_GameState GS;

	// Noise data
	private bool newNoiseHeard;
	private Data_Door lastNoiseHeardFrom;
	private float lastNoiseVolume;

	// To make sure the game state is fully initialized before loading it, this function is called by game state class itself
	public void loadGameState(Data_GameState gameState) {
		this.GS = gameState;
		this.me = gameState.getMonster();

		me.worldModel.updateMyRoom(me.isIn, GS.monsterSeesToni);
	}

	// Update is called once per frame
	void FixedUpdate () {
		// Don't do anything if the game state is not loaded yet or suspended
		if(GS == null || GS.SUSPENDED) {
			return;
		}

		// If monster sees Toni, do not update the world model
		if(GS.monsterSeesToni) {
			me.worldModel.toniKnownToBeInRoom(me.isIn);
		} else {
			// Otherwise, predict Toni's movements according to blind transition model
			me.worldModel.predictOneTimeStep();
			// And if a noise has been heard, update the model accordingly
			if(newNoiseHeard) {
				me.worldModel.filter(lastNoiseVolume, lastNoiseHeardFrom);
				newNoiseHeard = false;
			} else {
				// If no noise has been heard, filter anyway
				me.worldModel.filterWithNullSignal();
			}
		}
	}

	// The sound system triggers this function to inform the monster of incoming sounds
	public void hearNoise(Data_Door doorway, float loudness) {
		lastNoiseVolume = loudness;
		lastNoiseHeardFrom = doorway;
		newNoiseHeard = true;
	}

	// Inform the monster that Toni has just walked through this door to its other side
	public void seeToniGoThroughDoor(Data_Door originDoor) {
		me.worldModel.toniKnownToBeInRoom(originDoor.connectsTo.isIn);
	}

	// Inform the monster if Toni rattles on the currently held door
	public void seeToniRattleAtTheDoorknob(Data_Door rattledDoor) {
		me.worldModel.toniKnownToBeInRoom(rattledDoor.isIn);
	}
}
