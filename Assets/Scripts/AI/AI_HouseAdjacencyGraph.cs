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
		Update(GS);
	}

	public void Update(Data_GameState GS) {
		// Initialize data
		Vertices.Clear();
		Door2Vertice = new int[GS.DOORS.Count];
		RoomCount = GS.ROOMS.Count;
		float DOOR_TRANSITION_COST = Mathf.Abs(Global_Settings.read("CHARA_WALKING_SPEED") * Global_Settings.read("DOOR_TRANSITION_DURATION"));
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

	// Returns the complete graph as an ajacency list
	public string ExportGraph() {
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

	// Searches the graph starting from a node corresponding to the door specified
	// Returns a probabilty distribution over rooms that contain vertices that lie on the search horizon specified
	// pertaining how likely it is that the signal originated there
	// The HorizonSharpness parameter allows for a certain fuzziness in the search horizon value
	public float[] SearchForRoomsOnTheHorizon(Data_Door SearchStart, float SearchHorizon, float HorizonSharpness = 0.5f) {
		// Initialize parameters and results
		Vertice start = GetVerticeForDoor(SearchStart);
		float lowerHorizonBound = SearchHorizon - HorizonSharpness, upperHorizonBound = SearchHorizon + HorizonSharpness;
		int[] SearchResultCountPerRoom = new int[RoomCount];
		int TotalSearchResultCount = 0;
		float[] result = new float[RoomCount];
		// Initialize priority queue for Dijkstra
		SimplePriorityQueue<Vertice> DijkstraQueue = new SimplePriorityQueue<Vertice>();
		SimplePriorityQueue<Vertice> DistanceFromStartToPreviousVertice = new SimplePriorityQueue<Vertice>();
		Dictionary<Vertice, float> dist = new Dictionary<Vertice, float>();
		Dictionary<Vertice, Vertice> prev = new Dictionary<Vertice, Vertice>();
		foreach(Vertice v in Vertices) {
			float initialDistanceToStart = (v == start) ? 0 : float.MaxValue;
			DijkstraQueue.Enqueue(v, initialDistanceToStart);
			DistanceFromStartToPreviousVertice.Enqueue(v, float.MaxValue);
			dist.Add(v, initialDistanceToStart);
			prev.Add(v, null);
		}
		// Search until the queue is empty or until none of the remaining vertices have predecessors below the horizon
		do {
			// Fetch the next vertice from the priority queue
			Vertice currentVertice = DijkstraQueue.Dequeue(), previousVertice = prev[currentVertice];
			DistanceFromStartToPreviousVertice.Remove(currentVertice);

			// If the current vertice lies beyond the search horizon, while its previous one didn't,
			// AND they both are in the same room, add this room to the result list
			if(previousVertice != null && dist[currentVertice] > lowerHorizonBound && dist[previousVertice] <= upperHorizonBound && currentVertice.Room == previousVertice.Room) {
				SearchResultCountPerRoom[currentVertice.Room] += 1;
				TotalSearchResultCount += 1;
			}

			// Otherwise, go through the neighbours that are still in the queue and update their distances as necessary
			foreach(Vertice neighbourVertice in currentVertice.Edges.Keys) {
				if(DijkstraQueue.Contains(neighbourVertice)) {
					// Calculate distnace to neighbour
					float distanceFromStartToNeighbour = dist[currentVertice] + currentVertice.Edges[neighbourVertice];
					// Update only if necessary
					if(distanceFromStartToNeighbour < dist[neighbourVertice]) {
						DijkstraQueue.UpdatePriority(neighbourVertice, distanceFromStartToNeighbour);
						dist[neighbourVertice] = distanceFromStartToNeighbour;
						DistanceFromStartToPreviousVertice.UpdatePriority(neighbourVertice, dist[currentVertice]);
						prev[neighbourVertice] = currentVertice;
					}
				}
			}
		} while(DistanceFromStartToPreviousVertice.Count > 0 &&
		        DistanceFromStartToPreviousVertice.GetPriority(DistanceFromStartToPreviousVertice.First) < upperHorizonBound);

		// Return the probability distribution (unless there is no way the noise could come from the current house, then return all zeroes)
		if(TotalSearchResultCount > 0) {
			for(int i = 0; i < RoomCount; i++) {
				result[i] = (float)SearchResultCountPerRoom[i] / TotalSearchResultCount;
			}
		}
		return result;
	}

	private Vertice GetVerticeForDoor(Data_Door D) {
		return Vertices[Door2Vertice[D.INDEX]];
	}
}
