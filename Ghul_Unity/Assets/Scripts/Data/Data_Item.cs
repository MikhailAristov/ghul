using UnityEngine;
using System;

// This class is the item itself, which will be positioned in an itemSpot.
[Serializable]
public class Data_Item : Data_Character {

	// Index of the room in the global registry
	[SerializeField]
	private int _INDEX;
	public int INDEX
	{
		get { return _INDEX; }
		private set { _INDEX = value; }
	}

	// The item spot this item is placed in
	[SerializeField]
	private int _itemSpotIndex;
	public int itemSpotIndex
	{ 
		get { return _itemSpotIndex; } 
		set { _itemSpotIndex = value; }
	}

	// Constructor
	public Data_Item(string gameObjectName) : base(gameObjectName) { }

	// Resets game object references, e.g. after a saved state load
	public void fixObjectReferences(Data_GameState GS)
	{
		gameObj = GameObject.Find(_gameObjName);
	}
}
