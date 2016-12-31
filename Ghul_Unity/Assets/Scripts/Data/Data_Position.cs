using UnityEngine;
using System;

// This class is a tuple of 2D position of an object within the game space, 
// consisting of the room index, the horizontal position along the X axis,
// and the vertical position along the Y axis
[Serializable]
public class Data_Position {

    [SerializeField]
    public int RoomId;
    [SerializeField]
	public float X;
	[SerializeField]
	public float Y;

    public Data_Position(int R, float X)
    {
        RoomId = R;
        this.X = X;
		this.Y = 0;
	}

	public Data_Position(int R, float X, float Y)
	{
		RoomId = R;
		this.X = X;
		this.Y = Y;
	}

	public Data_Position(int R, Vector2 pos)
	{
		RoomId = R;
		this.X = pos.x;
		this.Y = pos.y;
	}

    public Data_Position clone()
    {
        return new Data_Position(RoomId, X, Y);
    }
}
