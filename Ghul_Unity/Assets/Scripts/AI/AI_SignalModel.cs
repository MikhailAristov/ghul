using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// TODO: Precompute as much as possible
public class AI_SignalModel  {

	// Will need the current player model for the sound emission likelyhoods
	private AI_PlayerModel playerModel;

	public float[,] door2roomMinSignalDistance;
	public float[,] door2roomMaxSignalDistance;

	private int roomCount;
	private int doorCount;
	private int noiseCount;

	// f( noise source = Toni | noise occurred )
	private double Likelihood_NoiseWasMadeByToni;
	// f( noise source = house | noise occurred )
	private double Likelihood_NoiseWasMadeByHouse;
	// f( door | noise origin room, noise type ) = double[noise type, room index, door index]
	public double[,,] likelihoodNoiseHeardAtDoor;

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
		// Recalculate the likelihoods of specific noises from specific room being heard at certain doors
		precomputeDoorAudibilityLikelihoods(GS);
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
				// Prepare the min distance from the target door to the left and right room edges
				float door2roomLeftEdge = float.MaxValue;
				float door2roomRightEdge = float.MaxValue;
				// Go through all local door pairs within the current room and update the distance-to-remote-door boundaries accordingly
				for(int n = 0; n < room.DOORS.Count; n++) {
					int nGlobalIndex = room.DOORS.Values[n].INDEX;
					// Update MinSignalDistance if smaller
					door2roomMinSignalDistance[d, r] = Math.Min(GS.distanceBetweenTwoDoors[d, nGlobalIndex], door2roomMinSignalDistance[d, r]);
					// Go through all door pairs
					for(int m = n + 1; m < room.DOORS.Count; m++) {
						int mGlobalIndex = room.DOORS.Values[m].INDEX;
						// Update MaxSignalDistance if greater
						float maxDistance = (GS.distanceBetweenTwoDoors[d, nGlobalIndex]
						                     + GS.distanceBetweenTwoDoors[d, mGlobalIndex]
						                     + GS.distanceBetweenTwoDoors[nGlobalIndex, mGlobalIndex]) / 2;
						door2roomMaxSignalDistance[d, r] = Math.Max(maxDistance, door2roomMaxSignalDistance[d, r]);
					}
					// Update the minimal distance from the door to the left and right edges of the room
					float doorPos = Math.Min(Math.Max(room.DOORS.Values[n].atPos, room.leftWalkBoundary), room.rightWalkBoundary);
					if(room.DOORS.Count < 2) {
						door2roomLeftEdge = GS.distanceBetweenTwoDoors[d, nGlobalIndex] + doorPos - room.leftWalkBoundary;
						door2roomRightEdge = GS.distanceBetweenTwoDoors[d, nGlobalIndex] + room.rightWalkBoundary - doorPos;
					} else {
						door2roomLeftEdge = Math.Min(GS.distanceBetweenTwoDoors[d, nGlobalIndex] + doorPos - room.leftWalkBoundary, door2roomMaxSignalDistance[d, r]);
						door2roomRightEdge = Math.Min(GS.distanceBetweenTwoDoors[d, nGlobalIndex] + room.rightWalkBoundary - doorPos, door2roomMaxSignalDistance[d, r]);
					}
					// Update max signal distance if the distance from current door to its room's boundaries is greater
					door2roomMaxSignalDistance[d, r] = Math.Max(Math.Max(door2roomLeftEdge, door2roomRightEdge), door2roomMaxSignalDistance[d, r]);
				}
				// Yeah, it's ugly, but whatcha gonna do about it
			}
		}
		//AI_Util.displayMatrix("SIGNAL MODEL: door2roomMinSignalDistance", door2roomMinSignalDistance);
		//AI_Util.displayMatrix("SIGNAL MODEL: door2roomMaxSignalDistance", door2roomMaxSignalDistance);
		//AI_Util.displayMatrix("SIGNAL MODEL: door2room ranges", AI_Util.subtractMatrices(door2roomMaxSignalDistance, door2roomMinSignalDistance));
	}

	// Precomputes the likelihoods of a noise of a specific type from a specific room being heard at a particular door
	// MUST be called after precomputeDoorToRoomDistanceBounds()
	// p( Door = d | OriginRoom = r , NoiseType = nt )
	private void precomputeDoorAudibilityLikelihoods(Data_GameState GS) {
		likelihoodNoiseHeardAtDoor = new double[noiseCount, roomCount, doorCount];
		Array.Clear(likelihoodNoiseHeardAtDoor, 0, noiseCount * roomCount * doorCount);
		// For each noise, first determine its maximumum traveling distance
		double singleDoorProb = 1.0 / doorCount; int reachableDoorCount;
		for(int noise = 0; noise < noiseCount; noise++) {
			double maxNoiseTravelDistance = Math.Sqrt(Control_Sound.getInitialLoudness(noise) / Control_Sound.NOISE_INAUDIBLE);
			// For reach room, compute which doors are reachable
			foreach(Data_Room room in GS.ROOMS.Values) {
				reachableDoorCount = 0;
				foreach(Data_Door door in GS.DOORS.Values) {
					// TODO: Check whether a door's reachability isn't dominated by another door in the same room
					if(!room.DOORS.ContainsValue(door) &&
					   door2roomMinSignalDistance[door.INDEX, room.INDEX] <= maxNoiseTravelDistance) {
						likelihoodNoiseHeardAtDoor[noise, room.INDEX, door.INDEX] = singleDoorProb;
						reachableDoorCount += 1;
					}
				}
				// Lastly, update the values based on how many doors are reachable
				for(int d = 0; d < doorCount; d++) {
					likelihoodNoiseHeardAtDoor[noise, room.INDEX, d] *= reachableDoorCount;
				}
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
				cumulativeLikelihoodOfToniMakingNoise += playerModel.noiseLikelihood(noise, room.INDEX);
			}
		}
		// Now normalize Toni's noise making over all rooms (uniform distribution)
		double probToniMakingNoise = cumulativeLikelihoodOfToniMakingNoise / GS.ROOMS.Count;

		// Finally, normalize the noise likelihoods
		Likelihood_NoiseWasMadeByToni = probToniMakingNoise / (probToniMakingNoise + probHouseMakingNoise);
		Likelihood_NoiseWasMadeByHouse = probHouseMakingNoise / (probToniMakingNoise + probHouseMakingNoise);

		UnityEngine.Debug.Log("SIGNAL MODEL: Likelihood_NoiseWasMadeByToni = " + Likelihood_NoiseWasMadeByToni + ", Likelihood_NoiseWasMadeByHouse = " + Likelihood_NoiseWasMadeByHouse);
	}

	// f( perceivedVolume, atDoor | Toni is in tonisRoom )
	public double signalLikelihood(float volume, Data_Door door, int tonisRoom) {
		double result = 0;
		// This is some crazy stochastic voodoo magic...
		for(int r = 0; r < roomCount; r++) {
			for(int n = 0; n < noiseCount; n++) {
				result += signalLikelihood(volume, door, n, r) * likelihoodNoiseHeardAtDoor[n, r, door.INDEX] * noiseAndOriginLikelihood(n, r, tonisRoom);
				if(double.IsNaN(result)) {
					throw new DivideByZeroException("signalLikelihood1");
				}
			}
		}
		return result;
	}

	// f( perceivedVolume | atDoor, noise type, origin room )
	private double signalLikelihood(float volume, Data_Door door, int noiseType, int origin) {
		// Estimate the distance the signal must have traveled
		double estimatedDistanceToOrigin = Math.Sqrt(Control_Sound.getInitialLoudness(noiseType) / volume);
		// Check whether the room can be reached from the specified door within that distance
		if(estimatedDistanceToOrigin >= door2roomMinSignalDistance[door.INDEX, origin]
		   && estimatedDistanceToOrigin <= door2roomMaxSignalDistance[door.INDEX, origin]) {
			return 1.0;
		} else {
			return 0;
		}
	}

	// f( noise type, origin room | Toni is in tonisRoom )
	private double noiseAndOriginLikelihood(int noiseType, int origin, int tonisRoom) {
		double result = 0;
		// Case 1: The noise was made by Toni
		result += noiseLikelihood(noiseType, origin, tonisRoom, true) * originLikelihood(origin, tonisRoom, true) * Likelihood_NoiseWasMadeByToni;
		// Case 2: The noise was made by the house
		result += noiseLikelihood(noiseType, origin, tonisRoom, false) * originLikelihood(origin, tonisRoom, false) * Likelihood_NoiseWasMadeByHouse;
		return result;
	}

	// f( noise type | origin room, Toni is in tonisRoom, whether Toni was the one who made that noise )
	private double noiseLikelihood(int noiseType, int origin, int tonisRoom, bool toniMadeThisNoise) {
		return ( toniMadeThisNoise ? playerModel.noiseLikelihood(noiseType, tonisRoom) : noiseLikelihoodByHouse(noiseType) );
	}

	// f( noise type | the noise was made by the house )
	private double noiseLikelihoodByHouse(int noiseType) {
		// Uniform distribution
		return (1.0 / noiseCount);
	}

	// f( origin room | Toni is in tonisRoom, whether Toni was the one who made that noise )
	private double originLikelihood(int origin, int tonisRoom, bool toniMadeThisNoise) {
		// Otherwise, proceed depending on who sent the signal
		if(toniMadeThisNoise) {
			return (origin == tonisRoom ? 1.0 : 0.0);
		} else {
			// Uniform distribution over all rooms
			return (1.0 / roomCount);
		}
	}


}
