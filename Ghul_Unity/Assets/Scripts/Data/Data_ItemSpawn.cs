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

	public bool containsItem { 
		get { return (itemIndex > 0); }
		private set { return; }
	}
	public int itemIndex; // index of the item in this spot (-1 if empty)

	// Constructor
	public Data_ItemSpawn(int I, int R, float X, float Y) : base(R, X) { 
		INDEX = I;
		this.Y = Y;
		itemIndex = -1;
	}
}
