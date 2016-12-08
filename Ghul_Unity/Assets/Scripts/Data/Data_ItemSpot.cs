using UnityEngine;
using System;

// This class is a specified position in a room that can hold an item.
[Serializable]
public class Data_ItemSpot : Data_Character {

	// Index of the item spot in the global registry
	[SerializeField]
	private int _INDEX;
	public int INDEX
	{
		get { return _INDEX; }
		private set { _INDEX = value; }
	}
		
	// local y-value with regards to the room the item spot is in
	[SerializeField]
	private float _localHeight;
	public float localHeight
	{
		get { return _localHeight; }
		private set { return; }
	}

	public bool containsItem;
	public int itemIndex; // index of the item in this spot (-1 if empty)




	// Constructor
	public Data_ItemSpot(string gameObjectName) : base(gameObjectName) { 
		containsItem = false;
		itemIndex = -1;
	}

	// Adds this item spot to a specific room at a particular position, with backreference
	public void addToRoom(Data_Room R, float xPos, float localYPos)
	{
		if (isIn == null) // The item spot's location is not set yet
		{
			_pos = new Data_Position(R.INDEX, xPos);
			isIn = R;
			_localHeight = localYPos;
		}
		else
		{
			throw new System.ArgumentException("Cannot add item spot #" + this + " to room #" + R + ": item spot is already in room #" + isIn, "original");
		}
	}

	// Fill the spot with an item
	public void placeItem(int index) {
		containsItem = true;
		itemIndex = index;
	}

	// Make the spot empty
	public void removeItem() {
		containsItem = false;
		itemIndex = -1;
	}

	// Resets game object references, e.g. after a saved state load
	public void fixObjectReferences(Data_GameState GS)
	{
		gameObj = GameObject.Find(_gameObjName);
		isIn = GS.getRoomByIndex(_pos.RoomId);
	}
}
