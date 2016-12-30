using UnityEngine;
using System;
using System.Collections;

public class Factory_PrefabController : MonoBehaviour {

	public TextAsset ItemIndex;
	public TextAsset RoomIndex;

	private Factory_PrefabItems allItems;
	private bool[] itemSpawned;

	// Use this for initialization
	void Awake () {
		// Load the item list from the JSON index
		// JSON Serialization docu: https://docs.unity3d.com/Manual/JSONSerialization.html
		allItems = JsonUtility.FromJson<Factory_PrefabItems>(ItemIndex.text);
		// Initialize an array to keep track of the items that had been spawned, so no duplicates occur
		itemSpawned = new bool[allItems.list.Length];
		Array.Clear(itemSpawned, 0, allItems.list.Length); // Sets all elements in the array to false
	}

	public GameObject spawnRandomItem(GameObject parent, Vector3 localPosition) {
		// Find a random item that has not been spawned yet
		int i; do {
			i = UnityEngine.Random.Range(0, allItems.list.Length);
		} while(itemSpawned[i]);


		return null;
	}
}
