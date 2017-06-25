[System.Serializable]
public struct Factory_PrefabRooms {

	public RoomPrefab[] list;

	[System.Serializable]
	public struct RoomPrefab {
		public string prefabName;	// Unique name of the prefab in the asset manager
		public string displayName;	// A descriptive room summary that will appear in the hierarchy
		public UnityEngine.Vector2 size;

		public int maxInstances;	// How many times this prefab may be spawned

		/*** DOORS ***/
		public bool doorSpawnLeft;	// Indicates that a sidedoor may be spawned on the left wall
		public bool doorSpawnRight;	// Dito for the sidedoor on the right
		public float[] doorSpawns;	// An array of x-positions of all spawn points for doors in the front wall (x = 0 in the middle of the bitmap)

		// Automatically counts up the total number of door spawn points
		public int maxDoors {
			get { 
				return (doorSpawns.Length + (doorSpawnLeft ? 1 : 0) + (doorSpawnRight ? 1 : 0));
			}
		}

		/*** ITEMS ***/
		public UnityEngine.Vector2[] itemSpawns; // An array of (x,y) position of all spawn points for items (the zero point 0:0 is in the center of the bitmap)
	}
}
