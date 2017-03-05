using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Factory_PrefabController : MonoBehaviour {

	public TextAsset ItemIndex;
	public TextAsset RoomIndex;

	private Factory_PrefabItems allItems;
	private int[] itemSpawnCount;
	private int totalItemCounter;

	private Factory_PrefabRooms allRooms;
	private int[] roomSpawnCount;
	private int totalRoomCounter;

	private int totalDoorCounter;
	private GameObject prefabDoorLeftSide;
	private GameObject prefabDoorRightSide;
	private GameObject prefabDoorBack;

	// Reset all counters (for starting a new game)
	public void resetAllCounters() {
		// Reload all rooms
		loadRoomIndex();
		// Reset door index
		totalDoorCounter = 0;
		// Reload all items
		loadItemIndex();
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
			throw new System.ArgumentException("Cannot parse the prefab ID from game object name: " + gameObjectName);
		}
	}

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
	public GameObject spawnItemFromName(string oldName, Transform parent, Vector3 localPosition) {
		if(allItems.list == null) { loadItemIndex(); }
		return spawnItem(parsePrefabID(oldName), parent, localPosition);
	}

	// Loads the index of spawnable rooms into memory and initializes auxiliary variables
	private void loadRoomIndex() {
		// Load the room list from the JSON index
		allRooms = JsonUtility.FromJson<Factory_PrefabRooms>(RoomIndex.text);
		// Initialize an array to keep track of the room that had been spawned
		roomSpawnCount = new int[allRooms.list.Length];
		Array.Clear(roomSpawnCount, 0, allRooms.list.Length); // Sets all elements in the array to 0
		// Set the room counter to 1, because the ritual room always starts off already placed
		totalRoomCounter = 1;
	}

	// Spawns a random room into existence with a specified minimum number of door spawns
	public GameObject spawnRandomRoom(int minDoorSpawns, float verticalRoomSpacing) {
		if(allRooms.list == null) { loadRoomIndex(); }
		// Find a random room that has not been spawned over the limit yet
		int i; do {
			i = UnityEngine.Random.Range(0, allRooms.list.Length);
			// The while clause below checks whether the currently picked prefab has been overuser OR doesn't have enough door spawns
			// In that case, another one is picked
		} while(roomSpawnCount[i] >= allRooms.list[i].maxInstances || allRooms.list[i].maxDoors < minDoorSpawns);
		// Generate and return the room
		Vector3 pos = new Vector3(0, verticalRoomSpacing * totalRoomCounter, 0);
		return spawnRoom(i, pos);
	}

	// Spawns an room from specific prefab
	private GameObject spawnRoom(int prefabIndex, Vector3 globalPosition) {
		// Spawn the new room
		string prefabPath = "Rooms/" + allRooms.list[prefabIndex].prefabName;
		GameObject newRoom = Instantiate(Resources.Load(prefabPath, typeof(GameObject))) as GameObject;
		// Set additional properties
		newRoom.name = String.Format("Room{0:00}: {2} [prefab{1:00}]", totalRoomCounter, prefabIndex, allRooms.list[prefabIndex].displayName);
		newRoom.transform.position = globalPosition;
		// Increase the instance count and return the handle to the new instance
		roomSpawnCount[prefabIndex] += 1; totalRoomCounter += 1;
		return newRoom;
	}

	// Returns additiona prefab details from the game object name
	public Factory_PrefabRooms.RoomPrefab getRoomPrefabDetails(string gameObjectName) {
		if(allRooms.list == null) { loadRoomIndex(); }
		int prefabId = parsePrefabID(gameObjectName);
		return allRooms.list[prefabId];
	}

	// Spawns a specific room from name (for loading old game states)
	public GameObject spawnRoomFromName(string oldName, float verticalRoomSpacing) {
		if(allRooms.list == null) { loadRoomIndex(); }
		Vector3 pos = new Vector3(0, verticalRoomSpacing * totalRoomCounter, 0);
		return spawnRoom(parsePrefabID(oldName), pos);
	}

	// Loads the door prefabs into memory
	private void loadDoorPrefabs() {
		prefabDoorBack = Resources.Load("Doors/PrefabDoor_Back", typeof(GameObject)) as GameObject;
		prefabDoorLeftSide = Resources.Load("Doors/PrefabDoor_LeftSide", typeof(GameObject)) as GameObject;
		prefabDoorRightSide = Resources.Load("Doors/PrefabDoor_RightSide", typeof(GameObject)) as GameObject;
		totalDoorCounter = 0;
	}

	// Spawns a back door at the left position of the given room
	public GameObject spawnBackDoor(Transform parentRoom, float horizontalPosition) {
		if(prefabDoorBack == null) { loadDoorPrefabs(); }
		return spawnDoor(prefabDoorBack, parentRoom, new Vector3(horizontalPosition, 0, 0));
	}

	// Spawns a side door on the left edge of the given room
	public GameObject spawnLeftSideDoor(Transform parentRoom, float roomWidth) {
		if(prefabDoorLeftSide == null) { loadDoorPrefabs(); }
		return spawnDoor(prefabDoorLeftSide, parentRoom, new Vector3(-roomWidth/2, 0, 0));
	}

	// Spawns a side door on the right edge of the given room
	public GameObject spawnRightSideDoor(Transform parentRoom, float roomWidth) {
		if(prefabDoorRightSide == null) { loadDoorPrefabs(); }
		return spawnDoor(prefabDoorRightSide, parentRoom, new Vector3(roomWidth/2, 0, 0));
	}

	// Spawns a door game object from prefab and sets its properties
	private GameObject spawnDoor(GameObject prefab, Transform parent, Vector3 localPosition) {
		GameObject newDoor = Instantiate(prefab);
		newDoor.name = String.Format("Door{0:00}", totalDoorCounter++);
		newDoor.transform.parent = parent;
		newDoor.transform.localPosition = localPosition;
		return newDoor;
	}
}
