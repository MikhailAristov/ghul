using UnityEngine;
using System.Collections;

public class Environment_Room : MonoBehaviour {

    private const float physicalBoundaryOffset = 0.9f;
    private const float cameraBoundaryOffset = 3.2f;
    private float leftWallPos;
    private float rightWallPos;

    // Use this for initialization
    void Start () {
        Vector3 ROOM_SIZE = transform.Find("Background").GetComponent<Renderer>().bounds.size;
        leftWallPos = -ROOM_SIZE[0] / 2;
        rightWallPos = ROOM_SIZE[0] / 2;
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    // Checks whether a position X lies within the boundaries of the room
    // Returns X if its within the boundaries, or the closest boundary if it is not
    public float validatePosition(float pos) {
        return Mathf.Min(Mathf.Max(pos, leftWallPos + physicalBoundaryOffset), rightWallPos - physicalBoundaryOffset);
    }

    // Checks whether a position X lies within the allowed camera span
    // Returns X if its within the boundaries, or the closest boundary if it is not
    public float validateCameraPosition(float pos)
    {
        return Mathf.Min(Mathf.Max(pos, leftWallPos + cameraBoundaryOffset), rightWallPos - cameraBoundaryOffset);
    }

}
