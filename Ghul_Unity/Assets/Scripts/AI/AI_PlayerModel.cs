using System;
using System.Collections;
using System.Collections.Generic;

// This class models player movement as a Markov chain with a single state for each room
// The probability of staying in the current room is determined by an expected mean staying time (in time steps) through exponential decay
// The probability of transitioning to connected rooms is assumed to be uniform across all doors
public class AI_PlayerModel {

	public double[,] TRANSITION_MATRIX;
	private double[] meanStayingTime;
	private int roomCount;

	// Global settings
	private float TIME_STEP;
	private float SCREEN_SIZE_HORIZONTAL;
	private float HORIZONTAL_ROOM_MARGIN;
	private float DOOR_TRANSITION_DURATION;
	private double MEAN_TONI_VELOCITY;

	// Player model weights
	const double Param_ExplorationWalk	= 0.3;
	const double Param_ItemFetchWalk	= 1.0;
	const double Param_DoorToDoorWalk	= 0.8;
	const double Param_RunningProb		= 0.1;

	public AI_PlayerModel(Data_GameState GS) {
		// First, some global settings
		TIME_STEP 					= Global_Settings.read("TIME_STEP");
		SCREEN_SIZE_HORIZONTAL		= Global_Settings.read("SCREEN_SIZE_HORIZONTAL");
		HORIZONTAL_ROOM_MARGIN		= Global_Settings.read("HORIZONTAL_ROOM_MARGIN");
		DOOR_TRANSITION_DURATION	= Global_Settings.read("DOOR_TRANSITION_DURATION");
		MEAN_TONI_VELOCITY = (1.0 - Param_RunningProb) * Global_Settings.read("CHARA_WALKING_SPEED") + Param_RunningProb * Global_Settings.read("CHARA_RUNNING_SPEED");

		// Then, initialize the transition matrix
		recalculate(GS);
	}

	public void recalculate(Data_GameState GS) {
		// Initialize the transition matrix
		roomCount = GS.ROOMS.Count;
		TRANSITION_MATRIX = new double[roomCount, roomCount];
		Array.Clear(TRANSITION_MATRIX, 0, roomCount * roomCount);
		// For each room, calculate mean staying time in time step units
		meanStayingTime = new double[roomCount];
		for(int i = 0; i < roomCount; i++) {
			meanStayingTime[i] = calculateMeanStayingTime(GS, i);
		}
		// Build a Markov chain representing transition probabilities from one state/room to another after a single time step
		for(int sourceRoomIndex = 0; sourceRoomIndex < roomCount; sourceRoomIndex++) {
			// Each row in the matrix represents the transition probabilities FROM a room, and thus must add up to zero
			// The probability of staying in the current room is modelled via exponential decay, based on average staying time
			double probOfStaying = Math.Exp(-1.0 / meanStayingTime[sourceRoomIndex]);
			TRANSITION_MATRIX[sourceRoomIndex, sourceRoomIndex] = probOfStaying;
			// The probability of transitioning through a door is assumed uniformly distributed across all doors
			List<Data_Door> doorsHere = GS.getRoomByIndex(sourceRoomIndex).DOORS;
			double probOfGoingThroughADoor = (1.0 - probOfStaying) / doorsHere.Count;
			// Loop through all doors in the current room and add the transition probabilities to neighbouring rooms
			foreach(Data_Door door in doorsHere) {
				TRANSITION_MATRIX[sourceRoomIndex, door.connectsTo.INDEX] += probOfGoingThroughADoor;
				// This also correctly handles the case when rooms are connected by more than one door:
				// the probability of transitioning to such a room is double (or more) than to any other room
			}
		}
	}

	private double calculateMeanStayingTime(Data_GameState GS, int roomIndex) {
		// Calculate effective (traversable) size (width) of the room in question
		Data_Room thisRoom = GS.getRoomByIndex(roomIndex);
		double effectiveWidth = thisRoom.width - HORIZONTAL_ROOM_MARGIN * 2;
		// Calculate the mean time for raw room exploration
		double meanExplorationDistance = 1.5 * effectiveWidth - SCREEN_SIZE_HORIZONTAL;
		// If there are items currently in here, calculate the mean distance needed to fetch one of them
		double meanItemFetchDistance = (GS.getItemsInRoom(roomIndex).Count > 0) ? effectiveWidth / 3 : 0;
		// If there is more than one door in the room, calculate the average path between two neighbouring doors
		int doorCount = thisRoom.DOORS.Count;
		double meanDoorToDoorDistance = DOOR_TRANSITION_DURATION * MEAN_TONI_VELOCITY + ((doorCount > 1) ? (effectiveWidth / (doorCount - 1)) : 0);
		// Combine the results according to their parametrized weight
		double meanWalkingDistance = Param_ExplorationWalk * meanExplorationDistance +
		                             Param_ItemFetchWalk * meanItemFetchDistance +
		                             Param_DoorToDoorWalk * meanDoorToDoorDistance;
		// Convert the walking distance into mean staying time and return it
		double meanStayingTimeInSeconds = meanWalkingDistance / MEAN_TONI_VELOCITY;
		double meanStayingTimeInTimeSteps = meanStayingTimeInSeconds / TIME_STEP;
		return meanStayingTimeInTimeSteps;
	}
}
