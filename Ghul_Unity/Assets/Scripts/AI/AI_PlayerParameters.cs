using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;

[Serializable]
public class AI_PlayerParameters {

	// During the calculation of Toni's expected staying time per room:
	public double WEIGHT_EXPLORATION_WALK;	// ...how likely Toni is to explore the whole room
	public double WEIGHT_ITEM_FETCH_WALK;	// ...how likely Toni is to try to pick up an item (if there are any)
	public double WEIGHT_DOOR2DOOR_WALK;	// ...how likely Toni is to 

	// Overall stats: How likely Toni is to run or to stand still (the sum of both should be below 1.0!)
	public double PROB_RUNNING;
	public double PROB_STANDING;
	public double PROB_WALKING {
		get { return (1.0 - PROB_RUNNING - PROB_STANDING); }
		private set { return; }
	}

	public AI_PlayerParameters() {
		WEIGHT_EXPLORATION_WALK =  0.3;
		WEIGHT_ITEM_FETCH_WALK = 1.0;
		WEIGHT_DOOR2DOOR_WALK = 0.8;

		PROB_RUNNING = 0.1;
		PROB_STANDING = 0.3;
	}
}
