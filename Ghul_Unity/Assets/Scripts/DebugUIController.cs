using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DebugUIController : MonoBehaviour {

	public Text displayedText;
	public GameObject player;
	private PlayerController playerController;

	// Use this for initialization
	void Start () {
		playerController = player.GetComponent<PlayerController> ();
		displayedText.text = "Debug Window";
	}
	
	// Update is called once per frame
	void Update () {
		displayedText.text = "DebugWindow\n"
			+ "Speed: " + playerController.GetSpeed() + "\n"
			+ "Stamina: " + playerController.GetStamina() + "\n"
			+ "isOutOfPower: " + playerController.IsOutOfPower() + "\n";
	}
}
