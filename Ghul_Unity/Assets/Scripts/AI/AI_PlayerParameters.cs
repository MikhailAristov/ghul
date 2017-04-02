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
	// ...how likely Toni is to explore the whole room
	public double WEIGHT_EXPLORATION_WALK;
	// ...how likely Toni is to try to pick up an item (if there are any)
	public double WEIGHT_ITEM_FETCH_WALK;
	// ...how likely Toni is to walk to the nearest door
	public double WEIGHT_DOOR2DOOR_WALK;

	private const double LEARNING_RATE_EXPLORATION	= 1e-4;
	private const double LEARNING_RATE_ITEM_FETCH	= 1e-3;
	private const double LEARNING_RATE_DOOR2DOOR	= 1e-4;

	// Overall stats: How likely Toni is to run or to stand still (the sum of both should be below 1.0!)
	public double PROB_RUNNING;
	public double PROB_STANDING;

	public double PROB_WALKING {
		get { return (1.0 - PROB_RUNNING - PROB_STANDING); }
		private set { return; }
	}

	private long COUNT_SPEED_MEASUREMENTS;
	// 30 minutes iff Time.fixedDeltaTime == 0.02 s :
	private const long COUNT_SPEED_MEASUREMENTS_CUTOFF = 30 * 60 * 50;

	public AI_PlayerParameters() {
		resetWalkingWeights();

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

	// Use stochastic gradient descent using the weights to update the walking weights
	public void updateWalkingDistanceWeights(List<AI_RoomHistory> roomHistory) {
		// Randomize the roomHistory order
		AI_Util.shuffleList<AI_RoomHistory>(roomHistory);
		// Go through Toni's room history
		foreach(AI_RoomHistory entry in roomHistory) {
			// Ignore entries where the walked distance is close to zero as abnormalities
			if(entry.cumulativeWalkedDistance < 0.1) {
				continue;
			}
			//Debug.LogFormat("Room history: room {0:D} -> {1:F3} m", entry.roomId, entry.cumulativeWalkedDistance);
			// Assuming the MSE loss function, first calculate the absolute loss
			double loss = WEIGHT_EXPLORATION_WALK * entry.meanExplorationDistance
			              + WEIGHT_ITEM_FETCH_WALK * entry.meanItemFetchDistance
			              + WEIGHT_DOOR2DOOR_WALK * entry.meanDoorToDoorDistance
			              - entry.cumulativeWalkedDistance;
			// Then, calculate and update the gradients
			WEIGHT_EXPLORATION_WALK -= LEARNING_RATE_EXPLORATION * 2 * entry.meanExplorationDistance * loss;
			WEIGHT_ITEM_FETCH_WALK -= LEARNING_RATE_ITEM_FETCH * 2 * entry.meanItemFetchDistance * loss;
			WEIGHT_DOOR2DOOR_WALK -= LEARNING_RATE_DOOR2DOOR * 2 * entry.meanDoorToDoorDistance * loss;
		}
		Debug.LogFormat("Player room transition model: exploration weight: {0:F3}; item fetch weight: {1:F3}; door2door weight: {2:F3}", WEIGHT_EXPLORATION_WALK, WEIGHT_ITEM_FETCH_WALK, WEIGHT_DOOR2DOOR_WALK);
		// Perform a sanity check
		sanityCheck();
	}

	// Performs sanity checks after each learning iteration and resets the system if necessary
	private void sanityCheck() {
		// Walk weights may never fall under -0.1
		if(WEIGHT_EXPLORATION_WALK < -0.1 || WEIGHT_ITEM_FETCH_WALK < -0.1 || WEIGHT_DOOR2DOOR_WALK < -0.1) {
			Debug.LogWarning("Abnormal walking weights detected, resetting...");
			resetWalkingWeights();
		}
	}

	private void resetWalkingWeights() {
		WEIGHT_EXPLORATION_WALK = 0.1;
		WEIGHT_ITEM_FETCH_WALK = 0.7;
		WEIGHT_DOOR2DOOR_WALK = 0.4;
	}
}
