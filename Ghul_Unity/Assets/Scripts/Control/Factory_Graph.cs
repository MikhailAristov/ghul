using UnityEngine;
using System;
using System.Collections.Generic;

public class Factory_Graph : MonoBehaviour {

	private DummyData_Graph graph;
	private bool graphCalculated;

	void Start() {
		graphCalculated = false;
	}

	// Returns the complete graph or null if it's not computed yet.
	public DummyData_Graph GetGraph() {
		if (graphCalculated)
			return graph;
		else
			return null;
	}

	// Removes a pre-existing graph. Use this before computing a new one if a graph already exists.
	public void deleteGraph() {
		graph = null;
		graphCalculated = false;
	}

	// Generates the planar graph given a basic edge-less graph. (i.e. no door spawns are connected)
	public void computePlanarGraph(DummyData_Graph g) {
		if (graphCalculated) {
			Debug.Log("Trying to generate a graph but there is already one computed. Use deleteGraph() before generating a new one.");
			return;
		}

		if (g == null) {
			Debug.Log("Cannot build a planar graph. No input graph given.");
			return;
		} else {
			graph = g;
		}

		// Step 1: Select a basic planar graph as a starting point.

		// Step 2: Connect vertices to the connected graph such that the resulting is planar again.

		// Step 3: Adjust degrees.

		graphCalculated = true;
	}
}
