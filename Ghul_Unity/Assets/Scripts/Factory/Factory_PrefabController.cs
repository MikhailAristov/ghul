using UnityEngine;
using System;
using System.Collections;
using System.Text.RegularExpressions;

public class Factory_PrefabController : MonoBehaviour {

	public TextAsset ItemIndex;
	public TextAsset RoomIndex;

	private Factory_PrefabItems allItems;
	private int[] itemSpawnCount;
	private int totalItemCounter;

	// Loads the index of spawnable items into memory and initializes auxiliary variables
	private void loadItemIndex() {
		// Load the item list from the JSON index
		// JSON Serialization docu: https://docs.unity3d.com/Manual/JSONSerialization.html
		allItems = JsonUtility.FromJson<Factory_PrefabItems>(ItemIndex.text);
		// Initialize an array to keep track of the items that had been spawned, so no duplicates occur
		itemSpawnCount = new int[allItems.list.Length];
		Array.Clear(itemSpawnCount, 0, allItems.list.Length); // Sets all elements in the array to 0
		totalItemCounter = 0;
	}

	// Parses the index of the prefab that was used to spawn the object with a particular name
	private int parsePrefabID(string gameObjectName) {
		int result;
		// Match the regex [prefab..] to the name
		Regex rgx = new Regex(@"\[prefab(\d+)\]", RegexOptions.IgnoreCase);
		MatchCollection matches = rgx.Matches(gameObjectName);
		// If parsing successful, return the integer
		if(matches.Count > 0 && Int32.TryParse(matches[0].Groups[1].ToString(), out result)) {
			return result;
		} else { // Otherwise, throw exception			
			throw new System.ArgumentException("Cannot parse the prefab ID from item name: " + gameObjectName);
		}
	}

	// Spawns an item from specific prefab
	private GameObject spawnItem(int prefabIndex, Transform parent, Vector3 localPosition) {
		// Spawn the new item
		string prefabPath = "Items/" + allItems.list[prefabIndex].prefabName;
		GameObject newItem = Instantiate(Resources.Load(prefabPath, typeof(GameObject))) as GameObject;
		// Set additional properties
		newItem.name = String.Format("Item{0:00}: {2} [prefab{1:00}]", totalItemCounter, prefabIndex, allItems.list[prefabIndex].displayName);
		newItem.transform.parent = parent;
		newItem.transform.localPosition = localPosition;
		// Mark the item as spawned and return the handle to the new instance
		itemSpawnCount[prefabIndex] += 1; totalItemCounter += 1;
		return newItem;
	}

	// Spawns a random item into existence that has not been spawned yet
	public GameObject spawnRandomItem(Transform parent, Vector3 localPosition) {
		if(allItems.list == null) { loadItemIndex(); }
		// Find a random item that has not been spawned yet
		int i; do {
			i = UnityEngine.Random.Range(0, allItems.list.Length);
		} while(itemSpawnCount[i] >= allItems.list[i].maxInstances);
		// Generate and return the item
		return spawnItem(i, parent, localPosition);
	}

	// Spawns a specific item from name (for loading old game states)
	public GameObject spawnNewItemFromName(string oldName, Transform parent, Vector3 localPosition) {
		if(allItems.list == null) { loadItemIndex(); }
		return spawnItem(parsePrefabID(oldName), parent, localPosition);
	}
}
