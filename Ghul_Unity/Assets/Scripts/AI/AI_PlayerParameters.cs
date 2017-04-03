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

	private const float MAX_LEARNING_TIME_IN_SEC = 0.1f;
	private const double MIN_ERROR_IMPROVEMENT = 1e-6;
	private const int MAX_LEARNING_ITERATIONS_WO_IMPROVEMENT = 3;
	private const int MAX_TRAINING_SET_SIZE = 100;
	private List<AI_RoomHistory> roomHistory;
	private double currentWeightsCumulativeError;

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
	public void updateWalkingDistanceWeights(List<AI_RoomHistory> validationSet) {
		double tmpWeightExplore = WEIGHT_EXPLORATION_WALK;
		double tmpWeightItemFetch = WEIGHT_ITEM_FETCH_WALK;
		double tmpWeightDoor2Door = WEIGHT_DOOR2DOOR_WALK;

		// Remove outliers from the validation set
		for(int i = 0; i < validationSet.Count; i++) {
			if(validationSet[i].cumulativeWalkedDistance < 0.1) {
				validationSet.RemoveAt(i--);
			}
		}
		if(validationSet.Count == 0) {
			return;
		}

		// Double the current cumulative error to give some leeway to relearning
		currentWeightsCumulativeError *= 2;

		// Trim the training set to max size if necessary
		if(roomHistory.Count > MAX_TRAINING_SET_SIZE) {
			roomHistory.RemoveRange(0, roomHistory.Count - MAX_TRAINING_SET_SIZE);			
		}
		// Copy the room history to a separate training set, so it won't get shuffled out of chronological order
		List<AI_RoomHistory> trainingSet = new List<AI_RoomHistory>(roomHistory);
		// Only execute training if training set is bigger than the validation set, otherwise jump straight to adding validation set to training set
		if(roomHistory.Count >= Math.Min(validationSet.Count, MAX_TRAINING_SET_SIZE)) {
			// Repeat training until validation set stops improving error or until time runs out
			float trainingStop = Time.timeSinceLevelLoad + MAX_LEARNING_TIME_IN_SEC;
			int iterationsWithoutImprovement = 0;
			// Learning rate factor for simulated annealing
			double learningRateFactor = 1.0;
			do {
				// Shuffle the training set for stochastic gradient descent
				AI_Util.shuffleList<AI_RoomHistory>(roomHistory);
				// Update the weights using training set
				foreach(AI_RoomHistory entry in trainingSet) {
					// Ignore entries where the walked distance is close to zero as abnormalities
					if(entry.cumulativeWalkedDistance < 0.1) {
						continue;
					}
					// Assuming the MSE loss function, first calculate the absolute loss
					double loss = calculateLoss(tmpWeightExplore, tmpWeightItemFetch, tmpWeightDoor2Door, entry);
					// Then, calculate and update the gradients
					tmpWeightExplore -= learningRateFactor * LEARNING_RATE_EXPLORATION * 2 * entry.meanExplorationDistance * loss;
					tmpWeightItemFetch -= learningRateFactor * LEARNING_RATE_ITEM_FETCH * 2 * entry.meanItemFetchDistance * loss;
					tmpWeightDoor2Door -= learningRateFactor * LEARNING_RATE_DOOR2DOOR * 2 * entry.meanDoorToDoorDistance * loss;
				}
				learningRateFactor /= 2;
				// Now validate the new weights
				double newCumulativeError = calculateMeanSquaredError(tmpWeightExplore, tmpWeightItemFetch, tmpWeightDoor2Door, validationSet);
				// Check if new error is better than the last one
				if((currentWeightsCumulativeError - newCumulativeError) > MIN_ERROR_IMPROVEMENT) {
					WEIGHT_EXPLORATION_WALK = tmpWeightExplore;
					WEIGHT_ITEM_FETCH_WALK = tmpWeightItemFetch;
					WEIGHT_DOOR2DOOR_WALK = tmpWeightDoor2Door;
					currentWeightsCumulativeError = newCumulativeError;
					iterationsWithoutImprovement = 0;
				} else {
					iterationsWithoutImprovement += 1;
				}
			} while(Time.timeSinceLevelLoad < trainingStop && iterationsWithoutImprovement <= MAX_LEARNING_ITERATIONS_WO_IMPROVEMENT);
		}
		// Add the current validation set to the training set to be used next time
		roomHistory.AddRange(validationSet);
		Debug.LogFormat("Player room transition model: error: {0:F6}; exploration weight: {1:F3}; item fetch weight: {2:F3}; door2door weight: {3:F3}", currentWeightsCumulativeError, WEIGHT_EXPLORATION_WALK, WEIGHT_ITEM_FETCH_WALK, WEIGHT_DOOR2DOOR_WALK);
		// Perform a sanity check
		sanityCheck();
	}

	// Convenience functions
	private double calculateLoss(double weightExplore, double weightItemFetch, double weightDoor2Door, AI_RoomHistory dataEntry) {
		return weightExplore * dataEntry.meanExplorationDistance + weightItemFetch * dataEntry.meanItemFetchDistance + weightDoor2Door * dataEntry.meanDoorToDoorDistance - dataEntry.cumulativeWalkedDistance;
	}
	// MSE
	private double calculateMeanSquaredError(double weightExplore, double weightItemFetch, double weightDoor2Door, List<AI_RoomHistory> dataSet) {
		double result = 0;
		foreach(AI_RoomHistory entry in dataSet) {
			double loss = calculateLoss(weightExplore, weightItemFetch, weightDoor2Door, entry);
			result += loss * loss;
		}
		result /= dataSet.Count;
		return result;
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
		roomHistory = new List<AI_RoomHistory>();
		currentWeightsCumulativeError = double.MaxValue;
	}
}
