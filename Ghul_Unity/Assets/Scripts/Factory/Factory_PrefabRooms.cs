using UnityEngine;
using System;

[Serializable]
public class Factory_PrefabRooms {
	public string prefabName;	// Unique name of the prefab in the asset manager
	public int width;			// Width of the room in px
	public int height;			// Width of the room in px

	/*** DOORS ***/
	public bool doorSpawnLeft;	// Indicates that a sidedoor may be spawned on the left wall
	public bool doorSpawnRight;	// Dito for the sidedoor on the right
	public float[] doorSpawns;	// An array of x-positions of all spawn points for doors in the front wall (x = 0 on the left edge of the bitmap)

	// Automatically counts up the total number of door spawn points
	public int maxDoors {
		get { 
			return (doorSpawns.Length + (doorSpawnLeft ? 1 : 0) + (doorSpawnRight ? 1 : 0));
		}
		set { return; }
	}

	/*** ITEMS ***/
	public Vector2[] itemSpawns; // An array of (x,y) position of all spawn points for items (the zero point 0:0 is in the top-left corner of the bitmap)
	public int countItemSpawns {
		get { 
			return itemSpawns.Length;
		}
		set { return; }
	}
}
