using UnityEngine;
using System;
using System.Collections.Generic;

// Game state object representing rooms in the house
[Serializable]
public class Data_Room : IComparable<Data_Room> {

    public int INDEX;
    public GameObject gameObj;
    [NonSerialized]
    public Environment_Room env;
    public float width;
    public List<Data_Door> DOORS;

    public Data_Room(int I, GameObject O)
    {
        this.INDEX = I;
        this.gameObj = O;
        this.env = this.gameObj.GetComponent<Environment_Room>();
        Transform background = this.gameObj.transform.FindChild("Background");
        Renderer bgRenderer = background.GetComponent<Renderer>();
        this.width = bgRenderer.bounds.size[0];
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
}
