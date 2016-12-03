using UnityEngine;

public class Control_Camera : MonoBehaviour
{
    private Data_GameState GS;
    private Data_Position focusOn;
    private Environment_Room currentEnvironment;

    private float PANNING_SPEED;
    private float VERTICAL_ROOM_SPACING;
    
    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState)
    {
        GS = gameState;

        // Set general movement parameters
        PANNING_SPEED = GS.getSetting("CAMERA_PANNING_SPEED");
        VERTICAL_ROOM_SPACING = GS.getSetting("VERTICAL_ROOM_SPACING");
    }

    // Update is called once per frame
    void Update()
    {
        if (GS == null) { return; } // Don't do anything if the game state is not loaded yet

        if (focusOn != null)
        {
            // Calculate the differences between the camera's current and target position
            float targetPositionX = currentEnvironment.validateCameraPosition(focusOn.X);
            float displacementY = focusOn.RoomId * VERTICAL_ROOM_SPACING - transform.position.y;
            float displacementX = targetPositionX - transform.position.x;
            // If there is a difference in the vertical direction, close it all at once (provisional)
            if (Mathf.Abs(displacementY) > 0.0f)
            {
                // Update the environment
                this.currentEnvironment = GS.getRoomByIndex(focusOn.RoomId).env;
                // Reevaluate and apply camera displacement
                displacementX = currentEnvironment.validateCameraPosition(focusOn.X) - transform.position.x;
                transform.Translate(displacementX, displacementY, 0);
                return;
            }

            // Otherwise, pan gradually
            displacementX *= Time.deltaTime * PANNING_SPEED;
            // Correct displacement
            if (Mathf.Abs(displacementX) > 0.0f)
            {
                transform.Translate(displacementX, 0, 0);
            }
        }
    }

    // Set an object to focus on
    public void setFocusOn(Data_Position pos)
    {
        focusOn = pos;
        currentEnvironment = GS.getRoomByIndex(pos.RoomId).env;
    }

    // Update the camera position
    public void updateCameraStatus()
    {
        Debug.Log("Camera looks at #" + focusOn.RoomId + " at position " + focusOn.X);
    }
}