using UnityEngine;
using System;

// This class is the item itself, which will be positioned in an itemSpot.
[Serializable]
public class Data_Item : Data_Character {

	// Possible item states:
	// the item has just been spawned and has not yet been interacted with
	public const int STATE_INITIAL = 1;
	// the item is currently being carried by the player character
	public const int STATE_CARRIED = 2;
	// the item was carried by the player character but was dropped
	public const int STATE_DROPPED = 3;
	// the item was carried by the player character when he died
	public const int STATE_ON_CADAVER = 4;
	// the item had been placed for the ritual in the main room
	public const int STATE_PLACED = 5;

	// Pointer to the character behavior aspect of the container object
	[NonSerialized]
	public Control_Item control;

	public override Control_Character getControl() {
		throw new NotImplementedException();
	}

	// Index of the room in the global registry
	[SerializeField]
	private int _INDEX;

	public int INDEX {
		get { return _INDEX; }
		set { _INDEX = value; }
	}

	// The current state of the item (available values see above)
	[SerializeField]
	private int _itemState;

	public int state { 
		get { return _itemState; } 
		set { _itemState = value; }
	}

	// Convenience property
	public float elevation {
		get { return _pos.Y; }
	}

	// Constructor
	public Data_Item(string gameObjectName) : base(gameObjectName) {
		control = gameObj.GetComponent<Control_Item>();
		_itemState = Data_Item.STATE_INITIAL;
	}

	// Check if the item can be picked up
	public bool isTakeable() {
		return (state == Data_Item.STATE_INITIAL || state == Data_Item.STATE_ON_CADAVER || state == Data_Item.STATE_DROPPED);
	}

	// Check if the item can be seen
	public bool isVisible() {
		return (state == Data_Item.STATE_INITIAL || state == Data_Item.STATE_DROPPED || state == Data_Item.STATE_PLACED);
	}

	// Resets game object references, e.g. after a saved state load
	public void fixObjectReferences(Data_GameState GS, Factory_PrefabController prefabFactory) {
		isIn = GS.getRoomByIndex(pos.RoomId);
		// Find the game object or recreate it if necessary
		gameObj = GameObject.Find(_gameObjName);
		if(gameObj == null) {
			Debug.Assert(prefabFactory != null);
			Vector3 localPos = new Vector3(atPos, elevation, -0.1f);
			gameObj = prefabFactory.spawnItemFromName(_gameObjName, isIn.env.transform, localPos);
		}

		// Fix the rest
		control = gameObj.GetComponent<Control_Item>();
		if(state == Data_Item.STATE_CARRIED) {
			GS.getToni().carriedItem = this;
		}
	}
}
