using UnityEngine;
using System;

// This class is a tuple of 2D position of an object within the game space, 
// consisting of the room index and the horizontal position along the X axis
[Serializable]
public class Data_Position {

    [SerializeField]
    public int RoomId;
    [SerializeField]
    public float X;

    public Data_Position(int R, float X)
    {
        RoomId = R;
        this.X = X;
    }

    public Data_Position clone()
    {
        return new Data_Position(RoomId, X);
    }
}
