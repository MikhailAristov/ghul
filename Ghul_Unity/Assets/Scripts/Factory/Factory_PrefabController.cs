using UnityEngine;
using System;
using System.Collections;

public class Factory_PrefabController : MonoBehaviour {

	public TextAsset ItemIndex;
	public TextAsset RoomIndex;

	private Factory_PrefabItems allItems;
	private bool[] itemSpawned;
	private int itemCounter;

	// Use this for initialization
	void Awake () {
		loadItemIndex();
	}

	// Loads the index of spawnable items into memory and initializes auxiliary variables
	private void loadItemIndex() {
		// Load the item list from the JSON index
		// JSON Serialization docu: https://docs.unity3d.com/Manual/JSONSerialization.html
		allItems = JsonUtility.FromJson<Factory_PrefabItems>(ItemIndex.text);
		// Initialize an array to keep track of the items that had been spawned, so no duplicates occur
		itemSpawned = new bool[allItems.list.Length];
		Array.Clear(itemSpawned, 0, allItems.list.Length); // Sets all elements in the array to false
		itemCounter = 0;
	}

	// Spawns a random item into existence that has not been spawned yet
	public GameObject spawnRandomItem(Transform parent, Vector3 localPosition) {
		// Find a random item that has not been spawned yet
		int i; do {
			i = UnityEngine.Random.Range(0, allItems.list.Length);
		} while(itemSpawned[i]);

		// Spawn the new item
		string prefabPath = "Items/" + allItems.list[i].prefabName;
		GameObject newItem = Instantiate(Resources.Load(prefabPath, typeof(GameObject))) as GameObject;
		// Set additional properties
		newItem.name = String.Format("Item{0:00}: {1}", itemCounter, allItems.list[i].displayName);
		newItem.transform.parent = parent;
		newItem.transform.localPosition = localPosition;
		// Mark the item as spawned and return the handle to the new instance
		itemSpawned[i] = true; itemCounter += 0;
		return newItem;
	}
}
