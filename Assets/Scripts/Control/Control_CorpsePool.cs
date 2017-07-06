using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// This class manages and cycles human and monster corpse GameObjects
public class Control_CorpsePool : MonoBehaviour {

	public Control_Camera MainCameraControl;
	public GameObject HumanCorpsePrefab;
	public GameObject BloodPuddlePrefab;
	public GameObject HumanSkeletonPrefab;
	public GameObject MonsterCorpsePrefab;
	public GameObject MonsterToniCorpsePrefab;

	private List<GameObject> humanCorpses;
	private List<GameObject> bloodPuddles;
	private List<GameObject> monsterCorpses;

	private GameObject lastHumanCorpse;
	private GameObject lastBloodPuddle;
	private GameObject lastMonsterCorpse;

	private bool CorpsesMoved;

	public const float corpseVisibilityThreshold = -0.5f;

	// Use this for initialization
	void Awake() {
		humanCorpses = new List<GameObject>();
		monsterCorpses = new List<GameObject>();
		bloodPuddles = new List<GameObject>();
		CorpsesMoved = true;
	}

	// Recycle old corpses, if there is a need
	void Update() {
		if(CorpsesMoved) {
			bool allClear = true;
			// Check human corpses that are not in the pool, not fresh, and not currently in view
			foreach(GameObject corpse in humanCorpses) {
				if(corpse.transform.parent != this.transform && corpse != lastHumanCorpse) {
					// Only remove the coprse if the main camera cannot see it!
					if(!MainCameraControl.canSeeObject(corpse, corpseVisibilityThreshold)) {
						// Replace the corpse with a drying blood puddle
						if(corpse.name.StartsWith(HumanCorpsePrefab.name)) {
							placeBloodPuddle(corpse.transform.parent.gameObject, corpse.transform.localPosition, corpse.GetComponentInChildren<SpriteRenderer>().flipX);
						}
						// Move the corpse
						corpse.transform.parent = this.transform;
						corpse.transform.localPosition = Vector3.zero;
					} else {
						allClear = false;
					}
				}
			}
			// Ditto for monster corpses
			foreach(GameObject corpse in monsterCorpses) {
				if(corpse.transform.parent != this.transform && corpse != lastMonsterCorpse) {
					if(!MainCameraControl.canSeeObject(corpse, corpseVisibilityThreshold)) {
						corpse.transform.parent = this.transform;
						corpse.transform.localPosition = Vector3.zero;
					} else {
						allClear = false;
					}
				}
			}
			// Ditto for blood puddles
			foreach(GameObject puddle in bloodPuddles) {
				if(puddle.transform.parent != this.transform && puddle != lastBloodPuddle) {
					if(!MainCameraControl.canSeeObject(puddle, corpseVisibilityThreshold)) {
						puddle.transform.parent = this.transform;
						puddle.transform.localPosition = Vector3.zero;
					} else {
						allClear = false;
					}
				}
			}
			// Reset the CorpsesMoved flag only if no excessive corpses are visible anymore
			CorpsesMoved = !allClear;
		}
	}

	// Creates or recycles a human corpse GameObject and places it where specified
	public GameObject placeHumanCorpse(GameObject parent, Vector2 position, bool flipped) {
		return placeCorpse(ref humanCorpses, ref HumanCorpsePrefab, ref lastHumanCorpse, parent, position, flipped);
	}

	// Creates or recycles a human skeleton GameObject and places it where specified
	public GameObject placeHumanSkeleton(GameObject parent, Vector2 position, bool flipped) {
		return placeCorpse(ref humanCorpses, ref HumanSkeletonPrefab, ref lastHumanCorpse, parent, position, flipped);
	}

	// Creates or recycles a monster corpse GameObject and places it where specified
	public GameObject placeMonsterCorpse(GameObject parent, Vector2 position, bool flipped) {
		return placeCorpse(ref monsterCorpses, ref MonsterCorpsePrefab, ref lastMonsterCorpse, parent, position, flipped);
	}

	// Creates or recycles a Monster!Toni corpse GameObject and places it where specified
	public GameObject placeMonsterToniCorpse(GameObject parent, Vector2 position, bool flipped) {
		return placeCorpse(ref monsterCorpses, ref MonsterToniCorpsePrefab, ref lastMonsterCorpse, parent, position, flipped);
	}

	// Creates or recycles a blood puddle GameObject and places it where specified
	public GameObject placeBloodPuddle(GameObject parent, Vector2 position, bool flipped) {
		return placeCorpse(ref bloodPuddles, ref BloodPuddlePrefab, ref lastBloodPuddle, parent, position, flipped);
	}

	// Creates or recycles any corpse GameObject and places it where specified
	private GameObject placeCorpse(ref List<GameObject> list, ref GameObject prefab, ref GameObject lastCorpse, GameObject parent, Vector2 position, bool flipped) {
		GameObject result = null;
		// First check if there are any free corpses in the pool
		foreach(GameObject corpse in list) {
			// corpse.name == prefab.name ensures that a regular corpse is not placed instead of a skeleton in the opening cutscene upon restarting the game
			if(corpse.transform.parent == this.transform && corpse.name.StartsWith(prefab.name)) {
				result = corpse;
				break;
			}
		}
		// If no suitable corpse has been found, instantiate one
		if(result == null) {
			result = Instantiate(prefab) as GameObject;
			list.Add(result);
			result.name = prefab.name + " #" + list.Count.ToString();
		}
		// Move the corpse
		result.transform.parent = parent.transform;
		result.transform.localPosition = new Vector3(Data_Position.snapToGrid(position.x), Data_Position.snapToGrid(position.y), 0);
		result.GetComponentInChildren<SpriteRenderer>().flipX = flipped;
		lastCorpse = result;
		CorpsesMoved = true;
		return result;
	}

	// Moves all corpses back to pool
	public void resetAll() {
		foreach(GameObject corpse in humanCorpses) {
			corpse.transform.parent = this.transform;
			corpse.transform.localPosition = Vector3.zero;
		}
		foreach(GameObject corpse in monsterCorpses) {
			corpse.transform.parent = this.transform;
			corpse.transform.localPosition = Vector3.zero;
		}
		foreach(GameObject puddle in bloodPuddles) {
			puddle.transform.parent = this.transform;
			puddle.transform.localPosition = Vector3.zero;
		}
		CorpsesMoved = false;
	}
}
