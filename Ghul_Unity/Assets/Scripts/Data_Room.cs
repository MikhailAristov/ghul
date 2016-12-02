using UnityEngine;
using System;
using System.Collections.Generic;

// Game state object representing rooms in the house
public class Data_Room : IComparable<Data_Room> {

    public int INDEX { get; private set; }
    public GameObject gameObj { get; private set; }
    public Environment_Room env { get; private set; }
    public float width { get; private set; }
    public List<Data_Door> DOORS { get; private set; } // The list is private, but elements are not

    private Transform background;
    private Renderer bgRenderer;

    public Data_Room(int I, GameObject O)
    {
        this.INDEX = I;
        this.gameObj = O;
        this.env = this.gameObj.GetComponent<Environment_Room>();
        this.background = this.gameObj.transform.FindChild("Background");
        this.bgRenderer = this.background.GetComponent<Renderer>();
        this.width = this.bgRenderer.bounds.size[0];
        this.DOORS = new List<Data_Door>();
    }

    public int CompareTo(Data_Room other) { return INDEX.CompareTo(other.INDEX); }
    public override string ToString() { return this.INDEX.ToString(); }

    // Adds a door to this room at a specific position
    public void addDoor(Data_Door D, float Pos)
    {
        D.addToRoom(this, Pos);
        this.DOORS.Add(D);
    }

	// Returns how many doors are located in this room
	public int getAmountOfDoors() {
		return DOORS.Count;
	}
}
