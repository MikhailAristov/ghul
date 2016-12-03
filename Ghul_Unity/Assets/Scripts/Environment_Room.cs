using UnityEngine;
using System;

public class Environment_Room : MonoBehaviour {

    [NonSerialized]
    private Data_GameState GS;
    [NonSerialized]
    private Data_Room me;

    private float leftWallPos;
    private float rightWallPos;

    private float MARGIN_SIZE_PHYSICAL;
    private float MARGIN_SIZE_CAMERA;
    private float MARGIN_DOOR_ENTRANCE;

    // Use this for initialization
    void Start () {
        Vector3 ROOM_SIZE = transform.Find("Background").GetComponent<Renderer>().bounds.size;
        leftWallPos = -ROOM_SIZE[0] / 2;
        rightWallPos = ROOM_SIZE[0] / 2;
    }

    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState, int ownIndex)
    {
        this.GS = gameState;

        // Calculate room dimensions
        me = GS.getRoomByIndex(ownIndex);
        rightWallPos = me.width / 2;
        leftWallPos = -rightWallPos;

        // Set general movement parameters
        MARGIN_SIZE_PHYSICAL = GS.getSetting("HORIZONTAL_ROOM_MARGIN");
        MARGIN_SIZE_CAMERA = GS.getSetting("SCREEN_SIZE_HORIZONTAL") / 2;
        MARGIN_DOOR_ENTRANCE = GS.getSetting("MARGIN_DOOR_ENTRANCE");
    }

    // Update is called once per frame
    void Update () {
        return;
	}

    // Checks whether a position X lies within the boundaries of the room
    // Returns X if its within the boundaries, or the closest boundary if it is not
    public float validatePosition(float pos) {
        return Mathf.Min(Mathf.Max(pos, leftWallPos + MARGIN_SIZE_PHYSICAL), rightWallPos - MARGIN_SIZE_PHYSICAL);
    }

    // Checks whether a position X lies within the allowed camera span
    // Returns X if its within the boundaries, or the closest boundary if it is not
    public float validateCameraPosition(float pos)
    {
        return Mathf.Min(Mathf.Max(pos, leftWallPos + MARGIN_SIZE_CAMERA), rightWallPos - MARGIN_SIZE_CAMERA);
    }

    // Returns a door object if one can be accessed from the specified position, otherwise returns NULL
    public Data_Door getDoorAtPos(float pos)
    {
        foreach (Data_Door d in me.DOORS) // Loop through all the doors in this room
        {
            if(Mathf.Abs(d.atPos) < (rightWallPos - MARGIN_SIZE_PHYSICAL) // Ignore side doors
                && Mathf.Abs(d.atPos - pos) < MARGIN_DOOR_ENTRANCE)
            {
                return d;
            }
        }
        return null;
    }

    // Returns a door object if there is one on the specified edge of the room, otherwise returns NULL
    private Data_Door getSideDoor(bool Left) // "Left = true" means "left edge", "false" means "right edge"
    {
        foreach (Data_Door d in me.DOORS) // Loop through all the doors in this room
        {
            if (Mathf.Abs(d.atPos) > (rightWallPos - MARGIN_SIZE_PHYSICAL)) // The door must be beyond the margins
            {
                if((d.atPos < 0.0f && Left) || (d.atPos > 0.0f && !Left)) // The door must also be on the specified side
                {
                    return d;
                }
            }
        }
        return null;
    }
    // These are just human-readable wrappers for the above:
    public Data_Door getDoorOnTheLeft() { return getSideDoor(true); }
    public Data_Door getDoorOnTheRight() { return getSideDoor(false); }
}
