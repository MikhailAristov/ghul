[System.Serializable]
public struct Factory_PrefabItems {

	public ItemPrefab[] list;

	[System.Serializable]
	public struct ItemPrefab {
		public string prefabName;
		public string displayName;
		public UnityEngine.Vector2 size;
	}
}
