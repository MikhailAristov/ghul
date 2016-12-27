using UnityEngine;
using System;

// This class is a specified position in a room that can hold an item.
[Serializable]
public class Data_ItemSpawn : Data_Position {
	
	// Index of the item spot in the global registry
	[SerializeField]
	private int _INDEX;
	public int INDEX {
		get { return _INDEX; }
		set { _INDEX = value; }
	}
		
	// local y-value relative to the room the item spot is in
	[SerializeField]
	public float Y;

	public bool containsItem { 
		get { return (itemIndex > 0); }
		private set { return; }
	}
	public int itemIndex; // index of the item in this spot (-1 if empty)

	// Constructor
	public Data_ItemSpawn(int I, int R, float X, float Y) : base(R, Y) { 
		INDEX = I;
		this.Y = Y;
		itemIndex = -1;
	}

	// Fill the spot with an item
	public void placeItem(int index) {
		itemIndex = index;
	}

	// Make the spot empty
	public void removeItem() {
		itemIndex = -1;
	}
}
