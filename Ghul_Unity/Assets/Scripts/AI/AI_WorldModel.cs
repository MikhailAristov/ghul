using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class AI_WorldModel {

	[SerializeField]
	private AI_PlayerModel playerModel;
	[SerializeField]
	private AI_SignalModel signalModel;

	public AI_PlayerParameters playerParameters {
		get { return playerModel.PLAYER_PARAMETERS; }
		private set { return; }
	}

	[SerializeField]
	private int roomCount;
	[SerializeField]
	public double[] probabilityThatToniIsInRoom;
	[SerializeField]
	private double[] newVector; // Performance optimization

	[SerializeField]
	private int monsterRoomIndex;
	[SerializeField]
	public int mostLikelyTonisRoomIndex;
	[SerializeField]
	private int secondMostLikelyTonisRoomIndex;
	public double certainty {
		get {
			try {
				return probabilityThatToniIsInRoom[mostLikelyTonisRoomIndex] - probabilityThatToniIsInRoom[secondMostLikelyTonisRoomIndex];
			} catch(IndexOutOfRangeException) {
				Debug.LogWarning("index out of range!");
				return 0;
			}
		}
		private set { return; }
	}

	public AI_WorldModel(Data_GameState GS) {
		// Initialize global parameters
		roomCount = GS.ROOMS.Count;
		// Initialize the room probability vector
		probabilityThatToniIsInRoom = new double[roomCount];
		newVector = new double[roomCount];
		toniKnownToBeInRoom(GS.getRoomByIndex((int)Global_Settings.read("RITUAL_ROOM_INDEX")));
		// Initialize player and signal model subsystems
		playerModel = new AI_PlayerModel(GS);
		signalModel = new AI_SignalModel(GS, playerModel);
	}

	public void reset(Data_GameState GS) {
		double uniformDistribution = 1.0 / roomCount;
		for(int i = 0; i < roomCount; i++) {
			probabilityThatToniIsInRoom[i] = uniformDistribution;
		}
		playerModel.recalculate(GS);
		signalModel.recalculate(GS);
	}

	// Update the world model with the knowledge that Toni is currently seen in a specific room
	public void toniKnownToBeInRoom(Data_Room room) {
		Array.Clear(probabilityThatToniIsInRoom, 0, roomCount);
		probabilityThatToniIsInRoom[room.INDEX] = 1f;
		updateMostLikelyRoomIndices();
	}

	// Update the world model after entering a new room
	public void updateMyRoom(Data_Room room, bool toniIsHere) {
		monsterRoomIndex = room.INDEX;
		if(toniIsHere) {
			toniKnownToBeInRoom(room);
		}
	}

	// Update the world model in absence of measurements
	// MUST be called every FixedUpdate unless Toni is directly visible!
	public void predictOneTimeStep() {
		// Simple matrix multiplication of player model transition matrix (transposed)
		// and the current position distribution vector
		for(int matrixRow = 0; matrixRow < roomCount; matrixRow++) {
			newVector[matrixRow] = 0;
			for(int matrixColumn = 0; matrixColumn < roomCount; matrixColumn++) {
				newVector[matrixRow] += playerModel.TRANSITION_MATRIX[matrixColumn, matrixRow] * probabilityThatToniIsInRoom[matrixColumn];
			}
		}
		// Toni is obviously not in the current room, otherwise toniKnownToBeInRoom() would have been called instead
		double normalizationConstant = 1.0 - newVector[monsterRoomIndex];
		for(int j = 0; j < roomCount; j++) {
			probabilityThatToniIsInRoom[j] = ((j == monsterRoomIndex) ? 0 : newVector[j] / normalizationConstant);
		}
		updateMostLikelyRoomIndices();
	}

	// Update the world model with a given measurement
	// MUST be called in a FixedUpdate after predictOneTimeStep()!
	public void filter(float loudness, Data_Door door) {
		// A simple Bayesian Wonham filter
		double normalization = 0;
		for(int i = 0; i < roomCount; i++) {
			// Posteriore := likelihood * priore (normalization constant to be applied later)
			newVector[i] = signalModel.signalLikelihood(loudness, door, i) * probabilityThatToniIsInRoom[i];
			normalization += newVector[i];
		}
		// Lastly, normalize the probabilities to sum up to 1
		for(int j = 0; j < roomCount; j++) {
			probabilityThatToniIsInRoom[j] = newVector[j] / normalization;
		}
		updateMostLikelyRoomIndices();
	}

	private void updateMostLikelyRoomIndices() {
		mostLikelyTonisRoomIndex = -1; secondMostLikelyTonisRoomIndex = -1;
		double highestProbability = -1.0; double secondHighestProbability = -2.0;
		for(int i = 0; i < roomCount; i++) {
			if(probabilityThatToniIsInRoom[i] > highestProbability) {
				// Shift the ranking by one
				secondMostLikelyTonisRoomIndex = mostLikelyTonisRoomIndex;
				secondHighestProbability = highestProbability;
				mostLikelyTonisRoomIndex = i;
				highestProbability = probabilityThatToniIsInRoom[i];
			} else if(probabilityThatToniIsInRoom[i] > secondHighestProbability) {
				secondMostLikelyTonisRoomIndex = i;
				secondHighestProbability = probabilityThatToniIsInRoom[i];
			}
		}
	}
}
