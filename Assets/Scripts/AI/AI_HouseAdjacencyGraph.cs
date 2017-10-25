using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Priority_Queue;

public class AI_HouseAdjacencyGraph {

	private const int VTYPE_LEFT_WALL = -1;
	private const int VTYPE_BACK_DOOR = 0;
	private const int VTYPE_RIGHT_WALL = 1;

	private List<Vertice> Vertices;
	private int[] Door2Vertice;
	private int RoomCount;

	private class Vertice {
		public int Type;
		public int Room;
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
			if(r.rightmostDoor != null) {
				Door2Vertice[r.rightmostDoor.INDEX] = Vertices.IndexOf(RightWall);
			}
		}
		// Lastly, go through all the doors and connect them among each other
		float DOOR_TRANSITION_COST = Mathf.Abs(Global_Settings.read("CHARA_WALKING_SPEED") * Global_Settings.read("DOOR_TRANSITION_DURATION"));
		foreach(Data_Door d in GS.DOORS.Values) {
			GetVerticeForDoor(d).ConnectTo(GetVerticeForDoor(d.connectsTo), DOOR_TRANSITION_COST);
		}
	}

	// Searches the graph starting from a node corresponding to the door specified
	// Returns all rooms that contain vertices that lie on the search horizon specified
	// The HorizonSharpness parameter allows for a certain fuzziness in the search horizon value
	public int[] SearchForRoomsOnTheHorizon(Data_Door SearchStart, float SearchHorizon, float HorizonSharpness = 0.5f) {
		// Initialize parameters and results
		Vertice start = GetVerticeForDoor(SearchStart);
		float lowerHorizonBound = SearchHorizon - HorizonSharpness, upperHorizonBound = SearchHorizon + HorizonSharpness;
		int[] SearchResultCountPerRoom = new int[RoomCount];
		// Initialize priority queue for Dijkstra
		SimplePriorityQueue<Vertice> Q = new SimplePriorityQueue<Vertice>();
		Dictionary<Vertice, float> dist = new Dictionary<Vertice, float>();
		Dictionary<Vertice, Vertice> prev = new Dictionary<Vertice, Vertice>();
		foreach(Vertice v in Vertices) {
			float initialDistanceToStart = (v == start) ? 0 : float.MaxValue;
			Q.Enqueue(v, initialDistanceToStart);
			dist.Add(v, initialDistanceToStart);
			prev.Add(v, null);
		}
		// Search until the queue is empty
		// TODO Better termination condition
		while(Q.Count > 0) {
			// Fetch the next vertice from the priority queue
			Vertice currentVertice = Q.Dequeue(), previousVertice = prev[currentVertice];

			// If the current vertice lies beyond the search horizon, while its previous one didn't,
			// AND they both are in the same room, add this room to the result list
			if(dist[currentVertice] > lowerHorizonBound && dist[previousVertice] <= upperHorizonBound && currentVertice.Room == previousVertice.Room) {
				SearchResultCountPerRoom[currentVertice.Room] += 1;
			}

			// Otherwise, go through the neighbours that are still in the queue and update their distances as necessary
			foreach(Vertice neighbourVertice in currentVertice.Edges.Keys) {
				if(Q.Contains(neighbourVertice)) {
					// Calculate distnace to neighbour
					float distanceFromStartToNeighbour = dist[currentVertice] + currentVertice.Edges[neighbourVertice];
					// Update only if necessary
					if(distanceFromStartToNeighbour < dist[neighbourVertice]) {
						Q.UpdatePriority(neighbourVertice, distanceFromStartToNeighbour);
						dist[neighbourVertice] = distanceFromStartToNeighbour;
						prev[neighbourVertice] = currentVertice;
					}
				}
			}
		}
		return SearchResultCountPerRoom;
	}

	private Vertice GetVerticeForDoor(Data_Door D) {
		return Vertices[Door2Vertice[D.INDEX]];
	}
}
