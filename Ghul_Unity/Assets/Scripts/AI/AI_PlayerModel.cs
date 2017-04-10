using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// This class models player movement as a Markov chain with a single state for each room
// The probability of staying in the current room is determined by an expected mean staying time (in time steps) through exponential decay
// The probability of transitioning to connected rooms is assumed to be uniform across all doors
[Serializable]
public class AI_PlayerModel {

	[NonSerialized]
	private AI_PlayerParameters _playerParams;
	public AI_PlayerParameters PLAYER_PARAMETERS {
		get {
			// If it is null, try loading it from disc
			if(_playerParams == null) {
				_playerParams = Control_Persistence.loadFromDisk<AI_PlayerParameters>();
			}
			// If it's STILL null (i.e. could not be loaded, create a new set
			if(_playerParams == null) {
				_playerParams = new AI_PlayerParameters();
			}
			return _playerParams;
		}
		private set { return; }
	}

	[SerializeField]
	public double[,] TRANSITION_MATRIX;

	[SerializeField]
	private int roomCount;
	[SerializeField]
	private double[] meanWalkingDistance; // Per room
	[SerializeField]
	private double[] meanStayingTime;
	[SerializeField]
	private int[] roomDoorCount;
	[SerializeField]
	private bool[] roomHasItemSpawns;

	// Global settings
	[SerializeField]
	private float TIME_STEP;
	[SerializeField]
	private float TONI_SINGLE_STEP_LENGTH;
	[SerializeField]
	private double MEAN_TONI_VELOCITY;

	public AI_PlayerModel(Data_GameState GS) {
		// First, some global settings
		TIME_STEP 				= Global_Settings.read("TIME_STEP");
		TONI_SINGLE_STEP_LENGTH	= Global_Settings.read("CHARA_SINGLE_STEP_LENGTH");
		// Then, initialize the transition matrix
		recalculate(GS);
	}

	public void recalculate(Data_GameState GS) {
		// Update player parameters first
		Data_PlayerCharacter Toni = GS.getToni();
		PLAYER_PARAMETERS.updateMovementSpeedProbabilities(Toni.cntStandingSinceLastDeath, Toni.cntWalkingSinceLastDeath, Toni.cntRunningSinceLastDeath);
		Toni.resetMovementCounters();
		// Only retrain movement weights if the room history is large enough, otherwise keep collecting items
		if(Toni.roomHistory.Count >= AI_PlayerParameters.MIN_VALIDATION_SET_SIZE) {
			PLAYER_PARAMETERS.updateWalkingDistanceWeights(Toni.roomHistory);
			Toni.resetRoomHistory();
		}
		// Recalculate the mean velocity
		MEAN_TONI_VELOCITY = Global_Settings.read("CHARA_WALKING_SPEED") * PLAYER_PARAMETERS.PROB_WALKING + Global_Settings.read("CHARA_RUNNING_SPEED") * PLAYER_PARAMETERS.PROB_RUNNING;
		// TODO: Room staying time
		// Lastly, save the player parameters to disk
		Control_Persistence.saveToDisk(PLAYER_PARAMETERS);

		// Initialize the transition matrix
		roomCount = GS.ROOMS.Count;
		TRANSITION_MATRIX = new double[roomCount, roomCount];
		Array.Clear(TRANSITION_MATRIX, 0, roomCount * roomCount);
		// For each room, read and save the relevant informations from the game state
		meanWalkingDistance = new double[roomCount];
		meanStayingTime 	= new double[roomCount];
		roomDoorCount 		= new int[roomCount];
		roomHasItemSpawns 	= new bool[roomCount];
		for(int i = 0; i < roomCount; i++) {
			roomDoorCount[i] = GS.getRoomByIndex(i).DOORS.Count;
			roomHasItemSpawns[i] = GS.getRoomByIndex(i).hasItemSpawns;
			meanStayingTime[i] = calculateMeanStayingTime(GS, i);
		}
		// Build a Markov chain representing transition probabilities from one state/room to another after a single time step
		for(int sourceRoomIndex = 0; sourceRoomIndex < roomCount; sourceRoomIndex++) {
			// Each row in the matrix represents the transition probabilities FROM a room, and thus must add up to zero
			// The probability of staying in the current room is modelled via exponential decay, based on average staying time
			double probOfStaying = Math.Exp(-1.0 / meanStayingTime[sourceRoomIndex]);
			TRANSITION_MATRIX[sourceRoomIndex, sourceRoomIndex] = probOfStaying;
			// The probability of transitioning through a door is assumed uniformly distributed across all doors
			double probOfGoingThroughADoor = (1.0 - probOfStaying) / roomDoorCount[sourceRoomIndex];
			// Loop through all doors in the current room and add the transition probabilities to neighbouring rooms
			foreach(Data_Door door in GS.getRoomByIndex(sourceRoomIndex).DOORS.Values) {
				TRANSITION_MATRIX[sourceRoomIndex, door.connectsTo.isIn.INDEX] += probOfGoingThroughADoor;
				// This also correctly handles the case when rooms are connected by more than one door:
				// the probability of transitioning to such a room is double (or more) than to any other room
			}
		}

		AI_Util.displayMatrix("PLAYER MODEL: Room transition matrix", TRANSITION_MATRIX);
	}

	private double calculateMeanStayingTime(Data_GameState GS, int roomIndex) {
		Data_Room room = GS.getRoomByIndex(roomIndex);
		// Calculate mean walking distance across the room
		meanWalkingDistance[roomIndex] = PLAYER_PARAMETERS.WEIGHT_EXPLORATION_WALK * room.meanExplorationDistance +
										 PLAYER_PARAMETERS.WEIGHT_ITEM_FETCH_WALK * room.meanItemFetchDistance +
										 PLAYER_PARAMETERS.WEIGHT_DOOR2DOOR_WALK * room.meanDoorToDoorDistance;
		// Convert the walking distance into mean staying time and return it
		double meanStayingTimeInSeconds = meanWalkingDistance[roomIndex] / MEAN_TONI_VELOCITY;
		double meanStayingTimeInTimeSteps = meanStayingTimeInSeconds / TIME_STEP;
		return meanStayingTimeInTimeSteps;
	}

	// f( noise type | the room Toni was in when he made the sound )
	public double noiseLikelihood(int noiseType, int roomIndex) {
		// At any given point in time, this is the probability of Toni making a walking or running noise
		double probWalkingNoise = (meanWalkingDistance[roomIndex] / TONI_SINGLE_STEP_LENGTH) / meanStayingTime[roomIndex];
		// Likelihood depends on the noise type
		switch(noiseType) {
		case Control_Sound.NOISE_TYPE_WALK:
			// Return probability of a walking noise while NOT running
			return (probWalkingNoise * PLAYER_PARAMETERS.PROB_WALKING);
		case Control_Sound.NOISE_TYPE_RUN:
			// Return probability of a walking noise while running
			return (probWalkingNoise * PLAYER_PARAMETERS.PROB_RUNNING);
		case Control_Sound.NOISE_TYPE_DOOR:
			// The more doors the room has, the higher the chance of walking through one
			return (roomDoorCount[roomIndex] / meanStayingTime[roomIndex]);
		case Control_Sound.NOISE_TYPE_ITEM:
		case Control_Sound.NOISE_TYPE_ZAP:
			// If the room has item spawns, there is a small chance Toni will try picking an item up
			// Note that the model doesn't actually track items in the house, as that would give the
			// monster an unfair advantage of knowing at a distance where all items are
			return (roomHasItemSpawns[roomIndex] ? (1.0 / meanStayingTime[roomIndex]) : 0);
		default:
			// Most the time, actually, Toni makes no sound at all,
			// but this doesn't matter when considering the likelihood of a specific sound
			// that has already been made
			return 0;
		}
	}
}
