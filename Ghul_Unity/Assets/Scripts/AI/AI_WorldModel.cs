using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AI_WorldModel {

	private AI_PlayerModel playerModel;
	private AI_SignalModel signalModel;

	private int roomCount;
	public double[] probabilityThatToniIsInRoom;
	private double[] newVector; // Performance optimization

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

	public void recalculateAllModels(Data_GameState GS) {
		playerModel.recalculate(GS);
		signalModel.recalculate(GS);
	}

	// Update the world model with the knowledge that Toni is currently seen in a specific room
	public void toniKnownToBeInRoom(Data_Room room) {
		Array.Clear(probabilityThatToniIsInRoom, 0, roomCount);
		probabilityThatToniIsInRoom[room.INDEX] = 1f;
	}

	// Update the world model after entering a new room
	public void updateMyRoom(Data_Room room, bool toniIsHere) {
		if(toniIsHere) {
			toniKnownToBeInRoom(room);
		} else { // This room is empty, update other probabilities, as well
			double scaleUpFactor = 1.0 - probabilityThatToniIsInRoom[room.INDEX];
			for(int i = 0; i < roomCount; i++) {
				probabilityThatToniIsInRoom[i] = (i == room.INDEX) ? 0 : probabilityThatToniIsInRoom[i] / scaleUpFactor;
			}
		}
	}

	// Update the world model in absence of measurements
	// MUST be called every FixedUpdate unless Toni is directly visible!
	public void predictOneTimeStep() {
		// Simple matrix multiplication of player model transition matrix
		// and the current position distribution vector
		for(int matrixRow = 0; matrixRow < roomCount; matrixRow++) {
			newVector[matrixRow] = 0;
			for(int matrixColumn = 0; matrixColumn < roomCount; matrixColumn++) {
				newVector[matrixRow] += playerModel.TRANSITION_MATRIX[matrixRow, matrixColumn] * probabilityThatToniIsInRoom[matrixColumn];
			}
		}
		// Replace the old distribution with the new vector
		newVector.CopyTo(probabilityThatToniIsInRoom, 0);
	}

	// Update the world model with a given measurement
	// MUST be called in a FixedUpdate after predictOneTimeStep()!
	public void filter(float loudness, Data_Door door) {
		// A simple Bayesian Wonham filter
		for(int i = 0; i < roomCount; i++) {
			// Posteriore := likelihood * priore (normalization constant to be applied later)
			newVector[i] = signalModel.signalLikelihood(loudness, door, i) * probabilityThatToniIsInRoom[i];
		}
		// Now calculate the normalization constant and update the probabilities with it
		double normalizationConstant = newVector.Sum();
		for(int j = 0; j < roomCount; j++) {
			probabilityThatToniIsInRoom[j] = newVector[j] / normalizationConstant;
		}
	}
}
