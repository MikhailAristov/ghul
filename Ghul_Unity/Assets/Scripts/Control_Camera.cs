using UnityEngine;
using System.Collections;

public class Control_Camera : MonoBehaviour {

    public GameObject focusOn;

    public GameObject currentRoom;
    private Environment_Room currentEnvironment;

    private Transform trafo;
    private Transform focusTrafo;
    private const float PANNING_SPEED = 6.0f;

    // Use this for initialization
    void Start ()
    {
        trafo = transform;
        focusTrafo = focusOn.transform;
        currentEnvironment = currentRoom.GetComponent<Environment_Room>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        float targetPosition = currentEnvironment.validateCameraPosition(focusTrafo.position.x);
        float displacement = (targetPosition - trafo.position.x) * Time.deltaTime * 9.0f;
        if (Mathf.Abs(displacement) > 0.00f)
        {
            trafo.Translate(displacement, 0, 0);
        }
    }
}
