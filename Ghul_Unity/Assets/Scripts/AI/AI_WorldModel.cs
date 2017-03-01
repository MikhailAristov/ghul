using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AI_WorldModel {

	private AI_PlayerModel playerModel;
	private AI_SignalModel signalModel;

	private Data_Room myCurrentRoom;
	private int roomCount;

	public float[] probabilityThatToniIsInRoom;

	public AI_WorldModel(Data_GameState GS) {
		// Initialize global parameters
		roomCount = GS.ROOMS.Count;
		myCurrentRoom = GS.getMonster().isIn;
		// Initialize the room probability vector
		probabilityThatToniIsInRoom = new float[roomCount];
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
		myCurrentRoom = room;
		if(toniIsHere) {
			toniKnownToBeInRoom(room);
		} else { // This room is empty, update other probabilities, as well
			float scaleUpFactor = 1f - probabilityThatToniIsInRoom[room.INDEX];
			for(int i = 0; i < roomCount; i++) {
				probabilityThatToniIsInRoom[i] = (i == room.INDEX) ? 0 : probabilityThatToniIsInRoom[i] / scaleUpFactor;
			}
		}
	}

	// Update the world model in absence of measurements
	// MUST be called every FixedUpdate unless Toni is directly visible!
	public void predictOneTimeStep() {
		
	}
}
