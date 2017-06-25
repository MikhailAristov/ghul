[System.Serializable]
public struct Factory_PrefabItems {

	public ItemPrefab[] list;

	[System.Serializable]
	public struct ItemPrefab {
		public string prefabName;		// Unique name of the prefab in the asset manager
		public string displayName;		// A descriptive room summary that will appear in the hierarchy
		public UnityEngine.Vector2 size;
		public int maxInstances;		// How many times this prefab may be spawned
	}
}
