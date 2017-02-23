using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AI_WorldModel {

	private int myCurrentRoom;
	private int noOfRooms;
	public float[] probabilityThatToniIsInRoom;

	public AI_WorldModel(int totalRooms, int curRoom) {
		noOfRooms = totalRooms;
		myCurrentRoom = curRoom;
		probabilityThatToniIsInRoom = new float[noOfRooms];
		toniKnownToBeInRoom(0); // Ritual room...
	}

	// Update the world model with the knowledge that Toni is currently seen in a specific room
	public void toniKnownToBeInRoom(int index) {
		Array.Clear(probabilityThatToniIsInRoom, 0, noOfRooms);
		probabilityThatToniIsInRoom[index] = 1f;
	}

	// Update the world model after entering a new room
	public void updateMyRoom(int index, bool toniIsHere) {
		myCurrentRoom = index;
		if(toniIsHere) {
			toniKnownToBeInRoom(index);
		} else { // This room is empty, update other probabilities, as well
			float scaleUpFactor = 1f - probabilityThatToniIsInRoom[index];
			for(int i = 0; i < noOfRooms; i++) {
				probabilityThatToniIsInRoom[i] = (i == index) ? 0f : probabilityThatToniIsInRoom[i] / scaleUpFactor;
			}
		}
	}

	// Update the world model in absence of measurements
	public void predict(float timeElapsed) {
		
	}
}
