using System;
using System.Collections;
using System.Collections.Generic;

public class AI_SignalModel  {

	// Will need the current player model for the sound emission likelyhoods
	private AI_PlayerModel playerModel;

	public float[,] door2roomMinSignalDistance;
	public float[,] door2roomMaxSignalDistance;

	private int roomCount;
	private int doorCount;
	private int noiseCount;

	private double Likelihood_NoiseWasMadeByToni;
	private double Likelihood_NoiseWasMadeByHouse;

	public AI_SignalModel(Data_GameState GS, AI_PlayerModel PM) {
		playerModel = PM;
		recalculate(GS);	
	}

	public void recalculate(Data_GameState GS) {
		// Set the overall parameters
		roomCount = GS.ROOMS.Count;
		doorCount = GS.DOORS.Count;
		noiseCount = 5;
		// Recalculate distance boundaries between each door and room
		precomputeDoorToRoomDistanceBounds(GS);
		// Recalculate noise making likelihoods
		precomputeNoiseMakerLikelihoods(GS);
	}

	// This function calculates for each door and room pair the shortest and the longest 
	// minimum distance that a point in that room can be removed from the door in question
	private void precomputeDoorToRoomDistanceBounds(Data_GameState GS) {
		// Initialize the arrays
		door2roomMinSignalDistance = new float[doorCount, roomCount];
		Array.Clear(door2roomMinSignalDistance, 0, doorCount * roomCount);
		door2roomMaxSignalDistance = new float[doorCount, roomCount];
		Array.Clear(door2roomMaxSignalDistance, 0, doorCount * roomCount);
		// Perform the preprocessing
		for(int d = 0; d < doorCount; d++) {
			for(int r = 0; r < roomCount; r++) {
				Data_Room room = GS.getRoomByIndex(r);
				// Set the initial value for MinSignalDistance to infinity
				door2roomMinSignalDistance[d, r] = float.MaxValue;
				// Calculate the initial value for MaxSignalDistance (voodoo magic)
				door2roomMaxSignalDistance[d, r] = Math.Max(0,
					Math.Max(room.leftmostDoor.atPos - room.leftWalkBoundary, room.rightWalkBoundary - room.rightmostDoor.atPos));
				// Go through all local door pairs within the current room and update the distance-to-remote-door boundaries accordingly
				for(int n = 0; n < room.DOORS.Count; n++) {
					int nGlobalIndex = room.DOORS[n].INDEX;
					// Update MinSignalDistance if smaller
					door2roomMinSignalDistance[d, r] = Math.Min(GS.distanceBetweenTwoDoors[d, nGlobalIndex], door2roomMinSignalDistance[d, r]);
					// Then go to other doors for MaxSignalDistance
					for(int m = n + 1; m < room.DOORS.Count; m++) {
						int mGlobalIndex = room.DOORS[m].INDEX;
						// Update MaxSignalDistance if greater
						float maxDistance = (GS.distanceBetweenTwoDoors[d, nGlobalIndex]
						                     + GS.distanceBetweenTwoDoors[d, mGlobalIndex]
						                     + GS.distanceBetweenTwoDoors[nGlobalIndex, mGlobalIndex]) / 2;
						door2roomMaxSignalDistance[d, r] = Math.Max(maxDistance, door2roomMaxSignalDistance[d, r]);
					}
				}
				// Yeah, it's ugly, but whatcha gonna do about it
			}
		}
	}

	// Precomputes the likelihoods of a given noise being made by Toni or by the house
	private void precomputeNoiseMakerLikelihoods(Data_GameState GS) {
		// First, calculate the raw probability of a noise being made by the house
		// The house makes random noises every so often, and the monster knows it
		float meanTimeStepsBetweenHouseNoises = ( (Control_Sound.RANDOM_NOISE_MAX_DELAY - Control_Sound.RANDOM_NOISE_MIN_DELAY) / 2 ) / Global_Settings.read("TIME_STEP");
		// So this is the probability of the house making a noise at any given point in time
		double probHouseMakingNoise = 1.0 / meanTimeStepsBetweenHouseNoises;

		// Then do the same for Toni making noises, using the player model
		double cumulativeLikelihoodOfToniMakingNoise = 0;
		foreach(Data_Room room in GS.ROOMS.Values) {
			for(int noise = 0; noise < noiseCount; noise++) {
				cumulativeLikelihoodOfToniMakingNoise += playerModel.noiseLikelihood(noise, room);
			}
		}
		// Now normalize Toni's noise making over all rooms (uniform distribution)
		double probToniMakingNoise = cumulativeLikelihoodOfToniMakingNoise / GS.ROOMS.Count;

		// Finally, normalize the noise likelihoods
		Likelihood_NoiseWasMadeByToni = probToniMakingNoise / (probToniMakingNoise + probHouseMakingNoise);
		Likelihood_NoiseWasMadeByHouse = probHouseMakingNoise / (probToniMakingNoise + probHouseMakingNoise);
	}

	// f( perceivedVolume, atDoor | Toni is in tonisRoom )
	public double signalLikelihood(float volume, Data_Door door, Data_Room tonisRoom) {
		double result = 0;
		// This is some crazy stochastic shit...
		for(int r = 0; r < roomCount; r++) {
			for(int n = 0; n < noiseCount; n++) {
				result += signalLikelihood(volume, door, n, r) * noiseAndOriginLikelihood(n, r, door, tonisRoom);
			}
		}
		return result;
	}

	// f( perceivedVolume, atDoor | noise type, origin room )
	private double signalLikelihood(float volume, Data_Door door, int noiseType, int origin) {
		// Estimate the distance the signal must have traveled
		double estimatedDistanceToOrigin = Math.Sqrt(Control_Sound.getInitialLoudness(noiseType) / volume);
		// Check whether the room can be reached from the specified door within that distance
		if(estimatedDistanceToOrigin >= door2roomMinSignalDistance[door.INDEX, origin]
		   && estimatedDistanceToOrigin <= door2roomMaxSignalDistance[door.INDEX, origin]) {
			// TODO: Check if a better approximation is possible
			return 1.0;
			// 1.0 / (door2roomMaxSignalDistance - door2roomMinSignalDistance)?
		} else {
			return 0;
		}
	}

	// f( noise type, origin room | atDoor, Toni is in tonisRoom )
	private double noiseAndOriginLikelihood(int noiseType, int origin, Data_Door door, Data_Room tonisRoom) {
		double result = 0;
		// Case 1: The noise was made by Toni
		result += noiseLikelihood(noiseType, origin, tonisRoom, true) * originLikelihood(origin, door, tonisRoom, true) * Likelihood_NoiseWasMadeByToni;
		// Case 2: The noise was made by the house
		result += noiseLikelihood(noiseType, origin, tonisRoom, false) * originLikelihood(origin, door, tonisRoom, false) * Likelihood_NoiseWasMadeByHouse;
		return result;
	}

	// f( noise type | origin room, Toni is in tonisRoom, whether Toni was the one who made that noise )
	private double noiseLikelihood(int noiseType, int origin, Data_Room tonisRoom, bool toniMadeThisNoise) {
		return ( toniMadeThisNoise ? playerModel.noiseLikelihood(noiseType, tonisRoom) : noiseLikelihoodByHouse(noiseType) );
	}

	// f( noise type | the noise was made by the house )
	private double noiseLikelihoodByHouse(int noiseType) {
		// Uniform distribution
		return (1.0 / noiseCount);
	}

	// f( origin room | atDoor, Toni is in tonisRoom, whether Toni was the one who made that noise )
	private double originLikelihood(int origin, Data_Door door, Data_Room tonisRoom, bool toniMadeThisNoise) {
		// Otherwise, proceed depending on who sent the signal
		if(toniMadeThisNoise) {
			// Kind of trivial, but still correct...
			if(origin == tonisRoom.INDEX) {
				return 1.0;
			} else {
				return 0;
			}
		} else {
			// Uniform distribution over all rooms
			return (1.0 / roomCount);
		}
	}


}
