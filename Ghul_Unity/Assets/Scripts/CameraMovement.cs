using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour {

	public GameObject player; // required for player position
	private Transform pTrafo; // contains the player's position
	private Transform cTrafo; // contains the camera's position
	private PlayerController pController;

	private const float CRITICAL_OFFSET = 5.0f; // if the player is this far to the left or right of the camera, the scrolling is triggered

	// Use this for initialization
	void Start () {
		pTrafo = player.transform;
		cTrafo = transform;
		pController = player.GetComponent<PlayerController>();
	}
	
	// Update is called once per frame
	void Update () {
		//TODO: Camera/player jittery when screen scrolls, run is held but directional key isn't held and the player slows down. May already be a problem in the player controller.
		//		Maybe not noticeable when animated?

		// Check whether player moves out of focus
		if (pTrafo.position.x - cTrafo.position.x >= CRITICAL_OFFSET) {
			cTrafo.Translate (new Vector3(pController.GetSpeed() * Time.deltaTime,0,0));
		} else if (cTrafo.position.x - pTrafo.position.x >= CRITICAL_OFFSET) {
			cTrafo.Translate (new Vector3((-1) * pController.GetSpeed() * Time.deltaTime,0,0));
		}
	}
}
