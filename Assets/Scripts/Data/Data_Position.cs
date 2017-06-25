using UnityEngine;
using System;

// This class is a tuple of 2D position of an object within the game space,
// consisting of the room index, the horizontal position along the X axis,
// and the vertical position along the Y axis
[Serializable]
public class Data_Position {

	public const float PIXEL_GRID_SIZE = 0.01f * 3.75f;
	// = m / px * upscalingFactor
	// upscalingFactor = 128 px (vertical size of room sprites) / 480 px (vertical screen size)

	[SerializeField]
	public int RoomId;
	[SerializeField]
	public float X;
	[SerializeField]
	public float Y;

	public Data_Position(int R, float X) {
		RoomId = R;
		this.X = X;
		this.Y = 0;
	}

	public Data_Position(int R, float X, float Y) {
		RoomId = R;
		this.X = X;
		this.Y = Y;
	}

	public Data_Position(int R, Vector2 pos, bool align = false) {
		RoomId = R;
		this.X = pos.x;
		this.Y = pos.y;
		if(align) {
			snapToGrid();
		}
	}

	public Data_Position clone() {
		return new Data_Position(RoomId, X, Y);
	}

	public override string ToString() {
		return String.Format("R{0:D}:{1:F2}/{2:F2}", RoomId, X, Y);
	}

	// Aligns the currently stored position values with the pixel grid
	public void snapToGrid() {
		X = snapToGrid(X);
		Y = snapToGrid(Y);
	}

	// Snaps any given float value to the pixel grid
	public static float snapToGrid(float value) {
		return PIXEL_GRID_SIZE * Mathf.Round(value / PIXEL_GRID_SIZE);
	}

	public Vector2 asLocalVector() {
		return new Vector2(this.X, this.Y);
	}
}
