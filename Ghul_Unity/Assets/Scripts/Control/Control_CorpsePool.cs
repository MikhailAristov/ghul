using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// This class manages and cycles human and monster corpse GameObjects
public class Control_CorpsePool : MonoBehaviour {

	public Control_Camera MainCameraControl;
	public GameObject HumanCorpsePrefab;
	public GameObject MonsterCorpsePrefab;

	private List<GameObject> humanCorpses;
	private List<GameObject> monsterCorpses;

	private GameObject lastHumanCorpse;
	private GameObject lastMonsterCorpse;

	public bool CorpsesMoved;

	public const float corpseVisibilityThreshold = -0.2f;

	// Use this for initialization
	void Awake() {
		humanCorpses = new List<GameObject>();
		monsterCorpses = new List<GameObject>();
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
			// Reset the CorpsesMoved flag only if no excessive corpses are visible anymore
			CorpsesMoved = !allClear;
		}
	}

	// Creates or recycles a human corpse GameObject and places it where specified
	public GameObject placeHumanCorpse(GameObject parent, Vector2 position) {
		return placeCorpse(ref humanCorpses, ref HumanCorpsePrefab, ref lastHumanCorpse, "Human Corpse #", parent, position);
	}

	// Creates or recycles a monster corpse GameObject and places it where specified
	public GameObject placeMonsterCorpse(GameObject parent, Vector2 position) {
		return placeCorpse(ref monsterCorpses, ref MonsterCorpsePrefab, ref lastMonsterCorpse, "Monster Corpse #", parent, position);
	}

	// Creates or recycles any corpse GameObject and places it where specified
	private GameObject placeCorpse(ref List<GameObject> list, ref GameObject prefab, ref GameObject lastCorpse, string name, GameObject parent, Vector2 position) {
		GameObject result = null;
		// First check if there are any free corpses in the pool
		foreach(GameObject corpse in list) {
			if(corpse.transform.parent == this.transform) {
				result = corpse;
				break;
			}
		}
		// If no suitable corpse has been found, instantiate one
		if(result == null) {
			result = Instantiate(prefab, position, Quaternion.identity, parent.transform) as GameObject;
			list.Add(result);
			result.name = name + list.Count.ToString();
		} else {
			// Move the corpse
			result.transform.parent = parent.transform;
			result.transform.localPosition = position;
		}
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
		CorpsesMoved = false;
	}
}
