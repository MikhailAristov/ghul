using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Control_WalkingSounds : MonoBehaviour {

	private static List<AudioClip> walkingSounds;
	private static List<AudioClip> runningSounds;

	void Awake() {
		
	}

	public void makeRandomWalkingStepNoise() {
		
	}

	public void PrintEvent(string s) {
		Debug.Log("PrintEvent: " + s + " called at: " + Time.time);
	}
}
