using UnityEngine;
using System;

[Serializable]
public class Data_Cadaver : Data_Character {

	// Constructor
	public Data_Cadaver(string gameObjectName) : base(gameObjectName) { }

	// Resets game object references, e.g. after a saved state load
	public void fixObjectReferences(Data_GameState GS)
	{
		gameObj = GameObject.Find(_gameObjName);
		isIn = GS.getRoomByIndex(_pos.RoomId);
	}
}
