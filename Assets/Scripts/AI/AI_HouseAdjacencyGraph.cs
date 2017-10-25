using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Priority_Queue;

public class AI_HouseAdjacencyGraph {

	private const int VTYPE_LEFT_WALL = -1;
	private const int VTYPE_BACK_DOOR = 0;
	private const int VTYPE_RIGHT_WALL = 1;

	public List<Vertice> Vertices;
	public int[] Door2Vertice;

	public class Vertice {
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
		Update(GS);
	}

	public Vertice GetVerticeForDoor(Data_Door D) {
		return Vertices[Door2Vertice[D.INDEX]];
	}

	public void Update(Data_GameState GS) {
		// Initialize data
		Vertices.Clear();
		Door2Vertice = new int[GS.DOORS.Count];
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
}
