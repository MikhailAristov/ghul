using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Priority_Queue;

[Serializable]
public class AI_HouseAdjacencyGraph {

	private const int VTYPE_LEFT_WALL = -1;
	private const int VTYPE_BACK_DOOR = 0;
	private const int VTYPE_RIGHT_WALL = 1;

	[SerializeField]
	private List<Vertice> Vertices;
	[SerializeField]
	private int[] Door2Vertice;
	[SerializeField]
	private int RoomCount;

	// Caching mechanism for faster access
	[SerializeField]
	private Vertice CurrentRootVertice;
	[SerializeField]
	private Dictionary<Vertice, float> DistanceToRoot;
	[SerializeField]
	private Dictionary<Vertice, Vertice> ClosestNeighbourLeadingBackToRoot;

	[Serializable]
	private class Vertice {
		[SerializeField]
		public int Type;
		[SerializeField]
		public int Room;
		[SerializeField]
		public Dictionary<Vertice, float> Edges;

		public Vertice(int t, int r) {
			Type = t;
			Room = r;
			Edges = new Dictionary<Vertice, float>(); 
		}

		public void ConnectTo(Vertice Other, float Distance) {
			if(!Edges.ContainsKey(Other)) {
				Edges.Add(Other, Distance);
				Other.ConnectTo(this, Distance);
			}
		}
	}

	public AI_HouseAdjacencyGraph(Data_GameState GS) {
		Vertices = new List<Vertice>();
		DistanceToRoot = new Dictionary<Vertice, float>();
		ClosestNeighbourLeadingBackToRoot = new Dictionary<Vertice, Vertice>();
	}

	public void Update(Data_GameState GS) {
		// Initialize data
		Vertices.Clear();
		Door2Vertice = new int[GS.DOORS.Count];
		RoomCount = GS.ROOMS.Count;
		float DOOR_TRANSITION_COST = Mathf.Abs(Global_Settings.read("CHARA_WALKING_SPEED") * Global_Settings.read("DOOR_TRANSITION_DURATION"));
		// Clear the cache
		CurrentRootVertice = null;
		DistanceToRoot.Clear();
		ClosestNeighbourLeadingBackToRoot.Clear();
		// Every left and right wall is a vertice, as is every back door
		foreach(Data_Room r in GS.ROOMS.Values) {
			// Add the left wall vertice
			Vertice LeftWall = new Vertice(VTYPE_LEFT_WALL, r.INDEX);
			Vertices.Add(LeftWall);
			int prev = Vertices.IndexOf(LeftWall);
			float prevPos = r.leftWalkBoundary;
			// For each back door, create a new vertice and an edge
			foreach(Data_Door d in r.DOORS.Values) {
				switch(d.type) {
				case Data_Door.TYPE_LEFT_SIDE:
					// If the room has a left side door, associate it with corresponding vertice and move on
					Door2Vertice[d.INDEX] = Vertices.IndexOf(LeftWall);
					break;
				case Data_Door.TYPE_BACK_DOOR:
					// For every back door, add a new vertice and associate them with each other
					Vertice newBackDoor = new Vertice(VTYPE_BACK_DOOR, r.INDEX);
					Vertices.Add(newBackDoor);
					int newIndex = Vertices.IndexOf(newBackDoor);
					Door2Vertice[d.INDEX] = newIndex;
					// Then add an edge between the new vertice and the previous on this room
					newBackDoor.ConnectTo(Vertices[prev], Mathf.Abs(d.atPos - prevPos));
					prev = newIndex;
					prevPos = d.atPos;
					break;
				default:
					break;
				}
			}
			// Lastly, add a right wall vertice and connect it to the previous one in this room
			Vertice RightWall = new Vertice(VTYPE_RIGHT_WALL, r.INDEX);
			Vertices.Add(RightWall);
			RightWall.ConnectTo(Vertices[prev], Mathf.Abs(r.rightWalkBoundary - prevPos));
			// Also associate a right side door, if any
			if(r.rightmostDoor.type == Data_Door.TYPE_RIGHT_SIDE) {
				Door2Vertice[r.rightmostDoor.INDEX] = Vertices.IndexOf(RightWall);
			}
		}
		// Lastly, go through all the doors and connect them among each other
		foreach(Data_Door d in GS.DOORS.Values) {
			GetVerticeForDoor(d).ConnectTo(GetVerticeForDoor(d.connectsTo), DOOR_TRANSITION_COST);
		}
	}

	// Searches the graph starting from a node corresponding to the door specified
	// Returns a probabilty distribution over rooms that contain vertices that lie on the search horizon specified
	// pertaining how likely it is that the signal originated there
	// The HorizonSharpness parameter allows for a certain fuzziness in the search horizon value
	public double[] SearchForRoomsOnTheHorizon(Data_Door SearchStart, float SearchHorizon, float HorizonSharpness = 0.5f) {
		Debug.Assert(Door2Vertice != null);

		// Initialize search parameters 
		Vertice NewSearchRoot = GetVerticeForDoor(SearchStart);
		float lowerHorizonBound = SearchHorizon - HorizonSharpness, upperHorizonBound = SearchHorizon + HorizonSharpness;

		// Don't update the previously calculated distances if the root hasn't changed
		if(CurrentRootVertice != NewSearchRoot) {
			UpdateShortestPaths(NewSearchRoot);
		} 

		// Look for vertices that lie beyond the (fuzzy) search horizon but whose neighbours still lie within it
		// AND they both are in the same room. Add these rooms to the result list
		int[] SearchResultCountPerRoom = new int[RoomCount];
		int TotalSearchResultCount = 0;
		foreach(Vertice v in Vertices) {
			Vertice previousVertice = ClosestNeighbourLeadingBackToRoot[v];
			if(previousVertice != null && v.Room == previousVertice.Room &&
			   DistanceToRoot[v] > lowerHorizonBound && DistanceToRoot[previousVertice] < upperHorizonBound) {
				SearchResultCountPerRoom[v.Room] += 1;
				TotalSearchResultCount += 1;
			}
		}

		// Return the probability distribution (unless there is no way the noise could come from the current house, then return all zeroes)
		double[] result = new double[RoomCount];
		if(TotalSearchResultCount > 0) {
			for(int i = 0; i < RoomCount; i++) {
				result[i] = (double)SearchResultCountPerRoom[i] / TotalSearchResultCount;
			}
		}
		return result;
	}

	private void UpdateShortestPaths(Vertice NewRoot) {
		// Clear the cache
		CurrentRootVertice = NewRoot;
		DistanceToRoot.Clear();
		ClosestNeighbourLeadingBackToRoot.Clear();
		// Initialize priority queue for Dijkstra's algorithm
		SimplePriorityQueue<Vertice> VerticePriorityQueue = new SimplePriorityQueue<Vertice>();
		foreach(Vertice v in Vertices) {
			float initialDistanceToRoot = (v == NewRoot) ? 0 : float.MaxValue;
			VerticePriorityQueue.Enqueue(v, initialDistanceToRoot);
			DistanceToRoot.Add(v, initialDistanceToRoot);
			ClosestNeighbourLeadingBackToRoot.Add(v, null);
		}
		// Search until the queue is empty
		while(VerticePriorityQueue.Count > 0) {
			// Fetch the next vertice from the priority queue
			Vertice currentVertice = VerticePriorityQueue.Dequeue();

			// Go through the neighbours that are still in the queue and update their distances as necessary
			foreach(Vertice neighbourVertice in currentVertice.Edges.Keys) {
				if(VerticePriorityQueue.Contains(neighbourVertice)) {
					// Calculate distnace to neighbour
					float distanceFromStartToNeighbour = DistanceToRoot[currentVertice] + currentVertice.Edges[neighbourVertice];
					// Update only if necessary
					if(distanceFromStartToNeighbour < DistanceToRoot[neighbourVertice]) {
						VerticePriorityQueue.UpdatePriority(neighbourVertice, distanceFromStartToNeighbour);
						DistanceToRoot[neighbourVertice] = distanceFromStartToNeighbour;
						ClosestNeighbourLeadingBackToRoot[neighbourVertice] = currentVertice;
					}
				}
			}
		}
	}

	private Vertice GetVerticeForDoor(Data_Door D) {
		return Vertices[Door2Vertice[D.INDEX]];
	}

	// Returns the complete graph as an ajacency list
	public override string ToString() {
		string result = "House adjacency graph/list";
		for(int i = 0; i < Vertices.Count; i++) {
			Vertice v = Vertices[i];
			string vType = (v.Type == VTYPE_LEFT_WALL) ? "L" : ((v.Type == VTYPE_RIGHT_WALL) ? "R" : "D");
			result += String.Format("\r\n{0:00} (R{2:00}/{1}): ", i, vType, v.Room);
			foreach(Vertice n in v.Edges.Keys) {
				result += String.Format("{0:00}[{1:F}m] ", Vertices.IndexOf(n), v.Edges[n]);
			}
		}
		return result;
	}
}
