using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class AI_SignalModel {

	// Will need the current player model for the sound emission likelyhoods
	[SerializeField]
	public AI_PlayerModel playerModel;

	// House adjacency graph in a separate class
	[SerializeField]
	public AI_HouseAdjacencyGraph adjacencyGraph;

	[SerializeField]
	private float[,] door2roomMinDistance;
	[SerializeField]
	private float[,] room2roomMinDistance;
	[SerializeField]
	private float[,] roomDoor2roomMaxDistance;

	[SerializeField]
	private int roomCount;
	[SerializeField]
	private int doorCount;
	[SerializeField]
	private int noiseCount;

	// f( noise source = Toni | noise occurred )
	[SerializeField]
	private double Likelihood_NoiseWasMadeByToni;
	// f( noise source = house | noise occurred )
	[SerializeField]
	private double Likelihood_NoiseWasMadeByHouse;
	// f( door | noise origin room, noise type ) = double[noise type, room index, door index]
	[SerializeField]
	private double[,,] likelihoodNoiseHeardAtDoor;
	// f( noise type, origin room | Toni is in tonisRoom ) = double[noise type, origin room index, Toni's room index]
	[SerializeField]
	private double[,,] noiseAndOriginLikelihood;
	// f( there is no signal | Toni is in tonisRoom, monster is in monsterRoom ) = double[tonisRoom index, monsterRoom index]
	[SerializeField]
	public double[,] nullSignalLikelihood;

	public AI_SignalModel(Data_GameState GS, AI_PlayerModel PM) {
		playerModel = PM;
		adjacencyGraph = new AI_HouseAdjacencyGraph(GS);
		recalculate(GS);
	}

	public void recalculate(Data_GameState GS) {
		// Set the overall parameters
		roomCount = GS.ROOMS.Count;
		doorCount = GS.DOORS.Count;
		noiseCount = Control_Noise.NOISE_TYPE_ZAP;
		// Update the adjacency graph
		adjacencyGraph.Update(GS);
		// Recalculate the likelihoods of specific noises from specific room being heard at certain doors
		precomputeDoorToRoomDistances(GS);
		precomputeDoorAudibilityLikelihoods(GS);
		// Recalculate noise making likelihoods
		precomputeNoiseMakerLikelihoods(GS);
		precomputeNoiseAndOriginLikelihoods();
		// Recalculate the null signal likelihoods
		precomputeNullSignalLikelihoods();
	}

	// Precomputes the (non-linear) min/max door-to-room and min room-to-room distances
	private void precomputeDoorToRoomDistances(Data_GameState GS) {
		// Minimal room-to-room distance is easy, as the game state already precomputes it
		AI_Util.copyMatrix(ref GS.distanceBetweenTwoRooms, ref room2roomMinDistance);
		// Now initialize the other two arrays
		AI_Util.initializeMatrix(ref door2roomMinDistance, doorCount, roomCount);
		AI_Util.initializeMatrix(ref roomDoor2roomMaxDistance, roomCount, roomCount);
		// Loop ithrough each door in each room to each door (and wall) in each room
		foreach(Data_Room originRoom in GS.ROOMS.Values) {
			foreach(Data_Door originDoor in originRoom.DOORS.Values) {
				foreach(Data_Room targetRoom in GS.ROOMS.Values) {
					// Skip the same room
					if(originRoom.INDEX == targetRoom.INDEX) {
						continue;
					}
					// Check each door in target room for the minimum and furthest distance
					float minDistance = float.MaxValue, thisDistance;
					foreach(Data_Door targetDoor in targetRoom.DOORS.Values) {
						thisDistance = GS.distanceBetweenTwoDoors[originDoor.INDEX, targetDoor.INDEX];
						minDistance = Mathf.Min(thisDistance, minDistance);
						roomDoor2roomMaxDistance[originRoom.INDEX, targetRoom.INDEX] = Mathf.Max(thisDistance, roomDoor2roomMaxDistance[originRoom.INDEX, targetRoom.INDEX]);
					}
					door2roomMinDistance[originDoor.INDEX, targetRoom.INDEX] = minDistance;
					// Lastly, check each WALL in the target room for the furthest distance, if necessary
					if(targetRoom.leftmostDoor.type != Data_Door.TYPE_LEFT_SIDE) {
						roomDoor2roomMaxDistance[originRoom.INDEX, targetRoom.INDEX] = 
							Mathf.Max(roomDoor2roomMaxDistance[originRoom.INDEX, targetRoom.INDEX], 
								GS.getDistance(originDoor, new Data_Position(targetRoom.INDEX, targetRoom.leftWalkBoundary)));
					}
					if(targetRoom.rightmostDoor.type != Data_Door.TYPE_RIGHT_SIDE) {
						roomDoor2roomMaxDistance[originRoom.INDEX, targetRoom.INDEX] = 
							Mathf.Max(roomDoor2roomMaxDistance[originRoom.INDEX, targetRoom.INDEX], 
								GS.getDistance(originDoor, new Data_Position(targetRoom.INDEX, targetRoom.rightWalkBoundary)));
					}
					// Ugly approximation, but it should work
					roomDoor2roomMaxDistance[originRoom.INDEX, targetRoom.INDEX] += (float)targetRoom.effectiveWidth / 6;
				}
			}
		}
	}

	// Precomputes the likelihoods of a noise of a specific type from a specific room being heard at a particular door
	// MUST be called after precomputeDoorToRoomDistanceBounds()
	// p( Door = d | OriginRoom = r , NoiseType = nt )
	private void precomputeDoorAudibilityLikelihoods(Data_GameState GS) {
		AI_Util.initializeMatrix(ref likelihoodNoiseHeardAtDoor, noiseCount, roomCount, doorCount);
		// For each noise, first determine its maximumum traveling distance
		double singleDoorProb = 1.0 / doorCount;
		int reachableDoorCount;
		for(int noise = 0; noise < noiseCount; noise++) {
			double maxNoiseTravelDistance = Math.Sqrt(Control_Noise.getInitialLoudness(noise) / Control_Noise.NOISE_INAUDIBLE);
			// For reach room, compute which doors are reachable
			foreach(Data_Room room in GS.ROOMS.Values) {
				reachableDoorCount = 0;
				foreach(Data_Door door in GS.DOORS.Values) {
					if(!room.DOORS.ContainsValue(door) &&
					   door2roomMinDistance[door.INDEX, room.INDEX] <= maxNoiseTravelDistance) {
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
		float meanTimeStepsBetweenHouseNoises = ((Control_Noise.RANDOM_NOISE_MAX_DELAY - Control_Noise.RANDOM_NOISE_MIN_DELAY) / 2) / Time.fixedDeltaTime;
		// So this is the probability of the house making a noise at any given point in time
		double probHouseMakingNoise = 1.0 / meanTimeStepsBetweenHouseNoises;

		// Then do the same for Toni making noises, using the player model
		double cumulativeLikelihoodOfToniMakingNoise = 0;
		foreach(Data_Room room in GS.ROOMS.Values) {
			for(int noise = Control_Noise.NOISE_TYPE_WALK; noise < noiseCount; noise++) {
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

	// Precompute noise and origin likelihoods
	private void precomputeNoiseAndOriginLikelihoods() {
		AI_Util.initializeMatrix(ref noiseAndOriginLikelihood, noiseCount, roomCount, roomCount);
		for(int noise = Control_Noise.NOISE_TYPE_WALK; noise < noiseCount; noise++) {
			for(int originRoomIndex = 0; originRoomIndex < roomCount; originRoomIndex++) {
				for(int toniRoomIndex = 0; toniRoomIndex < roomCount; toniRoomIndex++) {
					noiseAndOriginLikelihood[noise, originRoomIndex, toniRoomIndex] = getNoiseAndOriginLikelihood(noise, originRoomIndex, toniRoomIndex);
				}
			}
		}
	}

	// Precompute null signal likelihoods
	private void precomputeNullSignalLikelihoods() {
		AI_Util.initializeMatrix(ref nullSignalLikelihood, roomCount, roomCount);
		for(int toniRoomIndex = 0; toniRoomIndex < roomCount; toniRoomIndex++) {
			for(int monsterRoomIndex = 0; monsterRoomIndex < roomCount; monsterRoomIndex++) {
				nullSignalLikelihood[toniRoomIndex, monsterRoomIndex] = getNullSignalLikelihood(toniRoomIndex, monsterRoomIndex);
			}
		}
	}

	// f( perceivedVolume, atDoor | Toni is in tonisRoom )
	public double[] signalLikelihood(float volume, Data_Door door, float distToDoor) {
		double[] result = new double[roomCount];
		// This is some crazy stochastic voodoo magic...
		for(int n = Control_Noise.NOISE_TYPE_WALK; n < noiseCount; n++) {
			// Estimate the distance the signal must have traveled to the door
			float estimatedDistanceFromDoorToOrigin = Mathf.Sqrt(Control_Noise.getInitialLoudness(n) / volume) - Mathf.Abs(distToDoor);
			if(estimatedDistanceFromDoorToOrigin > 0) {
				float[] originRoomLikelihood = adjacencyGraph.SearchForRoomsOnTheHorizon(door, estimatedDistanceFromDoorToOrigin);
				for(int tonisRoom = 0; tonisRoom < roomCount; tonisRoom++) {
					for(int r = 0; r < roomCount; r++) {
						result[tonisRoom] += originRoomLikelihood[r] * likelihoodNoiseHeardAtDoor[n, r, door.INDEX] * noiseAndOriginLikelihood[n, r, tonisRoom];
					}
				}
			} 
		}
		return result;
	}

	// f( noise type, origin room | Toni is in tonisRoom )
	private double getNoiseAndOriginLikelihood(int noiseType, int origin, int tonisRoom) {
		double result = 0;
		// Case 1: The noise was made by Toni
		result += noiseLikelihood(noiseType, origin, tonisRoom, true) * originLikelihood(origin, tonisRoom, true) * Likelihood_NoiseWasMadeByToni;
		// Case 2: The noise was made by the house
		result += noiseLikelihood(noiseType, origin, tonisRoom, false) * originLikelihood(origin, tonisRoom, false) * Likelihood_NoiseWasMadeByHouse;
		return result;
	}

	// f( noise type | origin room, Toni is in tonisRoom, whether Toni was the one who made that noise )
	private double noiseLikelihood(int noiseType, int origin, int tonisRoom, bool toniMadeThisNoise) {
		return (toniMadeThisNoise ? playerModel.noiseLikelihood(noiseType, tonisRoom) : noiseLikelihoodByHouse(noiseType));
	}

	// f( noise type | the noise was made by the house )
	private double noiseLikelihoodByHouse(int noiseType) {
		// Uniform distribution
		return (1.0 / (noiseCount - 1));
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

	// f( null signal | Toni is in tonisRoom, monster is in monsterRoom)
	private double getNullSignalLikelihood(int tonisRoom, int monsterRoom) {
		double result = 0, tempSumOverToniNoise, tempSumOverHouseNoise;
		if(tonisRoom != monsterRoom) {
			for(int toniNoise = 0; toniNoise < noiseCount; toniNoise++) {
				tempSumOverToniNoise = 0;
				for(int houseNoise = 0; houseNoise < noiseCount; houseNoise++) {
					tempSumOverHouseNoise = 0;
					for(int houseNoiseOriginRoom = 0; houseNoiseOriginRoom < roomCount; houseNoiseOriginRoom++) {
						tempSumOverToniNoise += getNullSignalLikelihood(toniNoise, tonisRoom, houseNoise, houseNoiseOriginRoom, monsterRoom);
					}
					tempSumOverToniNoise += tempSumOverHouseNoise * noiseLikelihoodByHouse(houseNoise);
				}
				result += tempSumOverToniNoise * noiseLikelihood(toniNoise, tonisRoom, tonisRoom, true);
			}
		}
		return (result / roomCount);
	}

	// f( null signal | Toni makes toniNoise in tonisRoom, house makes houseNoise in houseNoiseOriginRoom, monster listens in monsterRoom)
	private double getNullSignalLikelihood(int toniNoise, int tonisRoom, int houseNoise, int houseNoiseOriginRoom, int monsterRoom) {
		double result = 1.0;
		if(toniNoise == Control_Noise.NOISE_TYPE_NONE) {
			if(houseNoise == Control_Noise.NOISE_TYPE_NONE) {
				// Case 1: Neither Toni, nor the house made any sound
				// Trivially, the monster cannot hear any sound (null signal) in this case, therefore its likelihood is 1 (max)
			} else {
				// Case 2: House made a sound, but Toni didn't
				// The monster perceives a null signal if the origin room is too far away to hear this signal
				result -= audibilityLikelihood(houseNoise, houseNoiseOriginRoom, monsterRoom);
			}
		} else {
			if(houseNoise == Control_Noise.NOISE_TYPE_NONE) {
				// Case 3: Toni made a sound, but the house didn't
				// The monster perceives a null signal if Toni's room is too far away to hear this signal
				result -= audibilityLikelihood(toniNoise, tonisRoom, monsterRoom);
			} else {
				// Case 4: Both Toni and the house made a sound during the same time step
				// In this unlikely event, fuse the two likelihoods together uniformly
				result -= (audibilityLikelihood(houseNoise, houseNoiseOriginRoom, monsterRoom) + audibilityLikelihood(toniNoise, tonisRoom, monsterRoom)) / 2;
			}
		}
		return result;
	}

	// Approx. how likely it is that the signal from origiRoom will be heard in the targetRoom
	private double audibilityLikelihood(int noiseType, int originRoom, int targetRoom) {
		double maxNoiseTravelDistance = Math.Sqrt(Control_Noise.getInitialLoudness(noiseType) / Control_Noise.NOISE_INAUDIBLE);
		if(maxNoiseTravelDistance > roomDoor2roomMaxDistance[targetRoom, originRoom]) {
			return 1.0;
		} else if(maxNoiseTravelDistance > room2roomMinDistance[targetRoom, originRoom]) {
			return (maxNoiseTravelDistance - room2roomMinDistance[targetRoom, originRoom]) / (roomDoor2roomMaxDistance[targetRoom, originRoom] - room2roomMinDistance[targetRoom, originRoom]);
		} else {
			return 0;
		}
	}

}
