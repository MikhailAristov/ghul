using UnityEngine;
using System;
using System.Collections.Generic;

// Game state object representing doors (connections between rooms) in the house
public class Data_Door : IComparable<Data_Door> {

    public int INDEX { get; private set; }
    public Data_Room isIn { get; private set; }
    public float atPos { get; private set; }
    public Data_Door connectsTo { get; private set; }
    public GameObject gameObj { get; private set; }

    public Data_Door(int I, GameObject O)
    {
        this.INDEX = I;
        this.gameObj = O;
    }

    public int CompareTo(Data_Door other) { return INDEX.CompareTo(other.INDEX); }
    public override string ToString() { return this.INDEX.ToString(); }

    // Adds this door to a specific room at a particular position, with backreference
    public void addToRoom(Data_Room R, float Pos)
    {
        if (this.isIn == null) // The door's location is not set yet
        {
            this.isIn = R;
            this.atPos = Pos;
        }
        else
        {
            throw new System.ArgumentException("Cannot add door #" + this + " to room #" + R + ": door is already in room #" + this.isIn, "original");
        }
    }

    // Connects the door to another door
    public void connectTo(Data_Door D)
    {
        if (D.connectsTo == null) // The other door is not connected to any other yet
        {
            this.connectsTo = D;
            D.connectTo(this);
        }
        else if (D.connectsTo == this) // The other door is connected to this one already
        {
            this.connectsTo = D;
        }
        else
        {
            throw new System.ArgumentException("Cannot connect door #" + this + " to #" + D + ": #" + D + " already connects to #" + D.connectsTo, "original");
        }
    }
}
