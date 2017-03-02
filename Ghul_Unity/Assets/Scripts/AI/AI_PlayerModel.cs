using System;
using System.Collections;
using System.Collections.Generic;

// This class models player movement as a Markov chain with a single state for each room
// The probability of staying in the current room is determined by an expected mean staying time (in time steps) through exponential decay
// The probability of transitioning to connected rooms is assumed to be uniform across all doors
public class AI_PlayerModel {

	public double[,] TRANSITION_MATRIX;

	private int roomCount;
	private double[] meanWalkingDistance; // Per room
	private double[] meanStayingTime;
	private int[] roomDoorCount;
	private bool[] roomHasItemSpawns;

	// Global settings
	private float TIME_STEP;
	private float SCREEN_SIZE_HORIZONTAL;
	private float HORIZONTAL_ROOM_MARGIN;
	private float DOOR_TRANSITION_DURATION;
	private float TONI_SINGLE_STEP_LENGTH;
	private double MEAN_TONI_VELOCITY;

	// Player model weights
	public const double Param_ExplorationWalk	= 0.3;
	public const double Param_ItemFetchWalk		= 1.0;
	public const double Param_DoorToDoorWalk	= 0.8;
	public const double Param_RunningProb		= 0.1;

	public AI_PlayerModel(Data_GameState GS) {
		// First, some global settings
		TIME_STEP 					= Global_Settings.read("TIME_STEP");
		SCREEN_SIZE_HORIZONTAL		= Global_Settings.read("SCREEN_SIZE_HORIZONTAL");
		HORIZONTAL_ROOM_MARGIN		= Global_Settings.read("HORIZONTAL_ROOM_MARGIN");
		// Walking settings
		MEAN_TONI_VELOCITY = (1.0 - Param_RunningProb) * Global_Settings.read("CHARA_WALKING_SPEED")
								  + Param_RunningProb  * Global_Settings.read("CHARA_RUNNING_SPEED");
		DOOR_TRANSITION_DURATION	= Global_Settings.read("DOOR_TRANSITION_DURATION");
		TONI_SINGLE_STEP_LENGTH		= Global_Settings.read("CHARA_SINGLE_STEP_LENGTH");
		// Then, initialize the transition matrix
		recalculate(GS);
	}

	public void recalculate(Data_GameState GS) {
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
		// Calculate effective (traversable) size (width) of the room in question
		double effectiveWidth = GS.getRoomByIndex(roomIndex).width - HORIZONTAL_ROOM_MARGIN * 2;
		// Calculate the mean time for raw room exploration
		double meanExplorationDistance = 1.5 * effectiveWidth - SCREEN_SIZE_HORIZONTAL;
		// If there are items currently in here, calculate the mean distance needed to fetch one of them
		double meanItemFetchDistance  =roomHasItemSpawns[roomIndex] ? (effectiveWidth / 3) : 0;
		// If there is more than one door in the room, calculate the average path between two neighbouring doors
		double meanDoorToDoorDistance = DOOR_TRANSITION_DURATION * MEAN_TONI_VELOCITY
			+ ((roomDoorCount[roomIndex] > 1) ? (effectiveWidth / (roomDoorCount[roomIndex] - 1)) : 0);
		// Combine the results according to their parametrized weight
		meanWalkingDistance[roomIndex] = Param_ExplorationWalk * meanExplorationDistance +
										 Param_ItemFetchWalk * meanItemFetchDistance +
										 Param_DoorToDoorWalk * meanDoorToDoorDistance;
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
			return (probWalkingNoise * (1 - Param_RunningProb));
		case Control_Sound.NOISE_TYPE_RUN:
			// Return probability of a walking noise while running
			return (probWalkingNoise * Param_RunningProb);
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
