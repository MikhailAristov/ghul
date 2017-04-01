using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;

[Serializable]
public class AI_PlayerParameters {

	// During the calculation of Toni's expected staying time per room:
	public double WEIGHT_EXPLORATION_WALK;	// ...how likely Toni is to explore the whole room
	public double WEIGHT_ITEM_FETCH_WALK;	// ...how likely Toni is to try to pick up an item (if there are any)
	public double WEIGHT_DOOR2DOOR_WALK;	// ...how likely Toni is to walk to the nearest door

	private const double LEARNING_RATE_WEIGHTS = 1e-4;

	// Overall stats: How likely Toni is to run or to stand still (the sum of both should be below 1.0!)
	public double PROB_RUNNING;
	public double PROB_STANDING;

	public double PROB_WALKING {
		get { return (1.0 - PROB_RUNNING - PROB_STANDING); }
		private set { return; }
	}

	private long COUNT_SPEED_MEASUREMENTS;
	private const long COUNT_SPEED_MEASUREMENTS_CUTOFF = 30 * 60 * 50;	// = 30 minutes iff Time.fixedDeltaTime == 0.02 s

	public AI_PlayerParameters() {
		WEIGHT_EXPLORATION_WALK =  0.3;
		WEIGHT_ITEM_FETCH_WALK = 1.0;
		WEIGHT_DOOR2DOOR_WALK = 0.8;

		PROB_RUNNING = 0.05;
		PROB_STANDING = 0.4;
		COUNT_SPEED_MEASUREMENTS = (long)(COUNT_SPEED_MEASUREMENTS_CUTOFF / 10);
	}

	public void updateMovementSpeedProbabilities(long cntStand, long cntWalk, long cntRun) {
		// Calculate new counts
		long newCntStand = (long)(PROB_STANDING * COUNT_SPEED_MEASUREMENTS) + cntStand;
		long newCntWalk = (long)(PROB_WALKING * COUNT_SPEED_MEASUREMENTS) + cntWalk;
		long newCntRun = (long)(PROB_RUNNING * COUNT_SPEED_MEASUREMENTS) + cntRun;
		COUNT_SPEED_MEASUREMENTS = newCntStand + newCntWalk + newCntRun;
		// Update probabilities
		PROB_STANDING = (double)newCntStand / COUNT_SPEED_MEASUREMENTS;
		PROB_RUNNING = (double)newCntRun / COUNT_SPEED_MEASUREMENTS;
		COUNT_SPEED_MEASUREMENTS = Math.Min(COUNT_SPEED_MEASUREMENTS, COUNT_SPEED_MEASUREMENTS_CUTOFF);
		Debug.LogFormat("Player movement model: {0:P3} standing, {1:P3} walking, {2:P3} running", PROB_STANDING, PROB_WALKING, PROB_RUNNING);
	}

	public void updateWalkingDistanceWeights(Data_GameState GS, List<int> roomIndices, List<float> walkingDistances) {
		Debug.Assert(roomIndices.Count == walkingDistances.Count);
		// Initialize the cumulative gradient (for batch gradient descent)
		double cumulativeGradientExploration = 0;
		double cumulativeGradientItemFetch = 0;
		double cumulativeGradientDoor2Door = 0;
		// Go through Toni's room history
		for(int i = 0; i < roomIndices.Count; i++) {
			Debug.LogFormat("Room history: room {0:D} -> {1:F3} m", roomIndices[i], walkingDistances[i]);
			Data_Room room = GS.getRoomByIndex(roomIndices[i]);
			// Assuming the MSE loss function, first calculate the absolute loss
			double loss = WEIGHT_EXPLORATION_WALK * room.meanExplorationDistance
			              + WEIGHT_ITEM_FETCH_WALK * room.meanItemFetchDistance
			              + WEIGHT_DOOR2DOOR_WALK * room.meanDoorToDoorDistance
			              - walkingDistances[i];
			// Then, calculate and update the gradients
			cumulativeGradientExploration += 2 * room.meanExplorationDistance * loss;
			cumulativeGradientItemFetch += 2 * room.meanItemFetchDistance * loss;
			cumulativeGradientDoor2Door += 2 * room.meanDoorToDoorDistance * loss;
		}
		// Update all weights with gradient and learning rate
		WEIGHT_EXPLORATION_WALK = WEIGHT_EXPLORATION_WALK - LEARNING_RATE_WEIGHTS * cumulativeGradientExploration; //Math.Max(0.01, );
		WEIGHT_ITEM_FETCH_WALK = WEIGHT_ITEM_FETCH_WALK - LEARNING_RATE_WEIGHTS * cumulativeGradientItemFetch; // Math.Max(0.01, );
			WEIGHT_DOOR2DOOR_WALK = WEIGHT_DOOR2DOOR_WALK - LEARNING_RATE_WEIGHTS * cumulativeGradientDoor2Door; // Math.Max(0.01, );
		Debug.LogFormat("Player room transition model: exploration weight: {0:F3}; item fetch weight: {1:F3}; door2door weight: {2:F3}", WEIGHT_EXPLORATION_WALK, WEIGHT_ITEM_FETCH_WALK, WEIGHT_DOOR2DOOR_WALK);
	}
}
