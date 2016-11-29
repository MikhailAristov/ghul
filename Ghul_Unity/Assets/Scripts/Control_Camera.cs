using UnityEngine;

public class Control_Camera : MonoBehaviour
{

    public GameObject GameState;
    private Data_GameState GS;

    public GameObject focusOn;
    private Transform focusTrafo;

    private Environment_Room currentEnvironment;

    private float PANNING_SPEED;

    // Use this for initialization; note that only local variables can be initialized here, game state is loaded later
    void Start()
    {
        return;
    }

    // To make sure the game state is fully initialized before loading it, this function is called by game state class itself
    public void loadGameState(Data_GameState gameState)
    {
        this.GS = gameState;

        // Set general movement parameters
        PANNING_SPEED = GS.getSetting("CAMERA_PANNING_SPEED");
    }

    // Update is called once per frame
    void Update()
    {
        if (GS == null) { return; } // Don't do anything until game state is loaded

        if (this.focusOn != null)
        {
            // Calculate the differences between the camera's current and target position
            float targetPositionX = currentEnvironment.validateCameraPosition(focusTrafo.position.x);
            float displacementY = focusTrafo.position.y - transform.position.y;
            float displacementX = targetPositionX - transform.position.x;
            // If there is a difference in the vertical direction, close it all at once (provisional)
            if (Mathf.Abs(displacementY) > 0.0f)
            {
                transform.Translate(displacementX, displacementY, 0);
                return;
            }

            // Otherwise, pan gradually
            displacementX *= Time.deltaTime * this.PANNING_SPEED;
            // Correct displacement
            if (Mathf.Abs(displacementX) > 0.0f)
            {
                transform.Translate(displacementX, 0, 0);
            }
        }
    }

    // Set an object to focus on
    public void setFocusOn(GameObject gameObj, Environment_Room env)
    {
        this.focusOn = gameObj;
        this.focusTrafo = gameObj.transform;
        this.currentEnvironment = env;
    }
}