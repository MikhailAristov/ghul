using UnityEngine;
using System;

// This class is the item itself, which will be positioned in an itemSpot.
[Serializable]
public class Data_Item : Data_Character {

	// Possible item states:
	public const int STATE_INITIAL = 1;	// the item has just been spawned and has not yet been interacted with 
	public const int STATE_CARRIED = 2;	// the item is currently being carried by the player character
	public const int STATE_DROPPED = 3;	// the item was carried by the player character but was dropped
	public const int STATE_ON_CADAVER = 4;	// the item was carried by the player character when he died
	public const int STATE_PLACED = 5;		// the item had been placed for the ritual in the main room

	// Pointer to the character behavior aspect of the container object
	[NonSerialized]
	public Control_Item control;

	// Index of the room in the global registry
	[SerializeField]
	private int _INDEX;
	public int INDEX
	{
		get { return _INDEX; }
		private set { _INDEX = value; }
	}

	// The item spot this item is originally placed in
	[SerializeField]
	private int _originalSpawnPoint;
	public int itemSpotIndex
	{ 
		get { return _originalSpawnPoint; } 
		set { _originalSpawnPoint = value; }
	}
		
	// Current position of the item along the Y-axis, relative to the container
	[SerializeField]
	private float _localElevation;
	public float elevation
	{
		get { return _localElevation; }
		private set { return; }
	}

	// The current state of the item (available values see above)
	[SerializeField]
	private int _itemState;
	public int state
	{ 
		get { return _itemState; } 
		set { _itemState = value; }
	}

	// Constructor
	public Data_Item(string gameObjectName) : base(gameObjectName) {
		control = gameObj.GetComponent<Control_Item>();
		_itemState = Data_Item.STATE_INITIAL;
	}

	// Complete position specification
	public void updatePosition(Data_Room R, float xPos, float relElevation) {
		base.updatePosition(R, xPos);
		_localElevation = relElevation;
	}

	// Check if the item can be picked up
	public bool isTakeable() {
		return (state == Data_Item.STATE_INITIAL || state == Data_Item.STATE_ON_CADAVER || state == Data_Item.STATE_DROPPED);
	}

	// Resets game object references, e.g. after a saved state load
	public void fixObjectReferences(Data_GameState GS)
	{
		gameObj = GameObject.Find(_gameObjName);
		control = gameObj.GetComponent<Control_Item>();
		isIn = GS.getRoomByIndex(pos.RoomId);
		if(state == Data_Item.STATE_CARRIED) {
			GS.getCHARA().carriedItem = this;
		}
	}
}
