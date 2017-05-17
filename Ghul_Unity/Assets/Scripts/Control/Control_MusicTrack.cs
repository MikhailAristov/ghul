using UnityEngine;
using System;
using System.Collections;

public class Control_MusicTrack : MonoBehaviour {

	private const float INAUDIBIBILITY_THRESHOLD = 0.01f;
	private const float MUTING_STEP = 0.1f;

	private AudioSource mainTrack;
	private AudioSource proximitySubtrack;

	private float targetMainTrackVolume;
	private float targetProximityTrackVolume;
	private bool isMuted;
	private bool isPaused;

	// Use this for initialization
	void Start () {
		AudioSource[] myTracks = GetComponents<AudioSource>();
		if(myTracks.Length == 2) {
			mainTrack = myTracks[0];
			proximitySubtrack = myTracks[1];
		} else {
			throw new ArgumentException(transform.name + " doesn't have exactly 2 tracks!");
		}
		// Automute on start
		muteTrack(0);
		isMuted = true;
		isPaused = false;
	}

	// Update is called once per frame
	void Update () {
		if(!isMuted && !isPaused) {
			mainTrack.volume = Mathf.Lerp(mainTrack.volume, targetMainTrackVolume, 0.01f);
			proximitySubtrack.volume = Mathf.Lerp(proximitySubtrack.volume, targetProximityTrackVolume, 0.01f);
			proximitySubtrack.pitch = Mathf.Lerp(proximitySubtrack.pitch, (1f - targetProximityTrackVolume), 0.01f);
			if(mainTrack.volume < INAUDIBIBILITY_THRESHOLD) {
				isMuted = true;
				mainTrack.mute = true;
				proximitySubtrack.mute = true;
			}
		}
	}

	public void muteTrack(float duration) {
		// If duration is not positive, mute the track immediately
		if(duration <= 0) {
			targetMainTrackVolume = 0;
			targetProximityTrackVolume = 0;
			mainTrack.volume = 0;
			proximitySubtrack.volume = 0;
		}
		// Otherwise, do so gradually
		StopAllCoroutines();
		StartCoroutine(graduallyMuteTrack(duration));
	}

	private IEnumerator graduallyMuteTrack(float duration) {
		float waitUntil = Time.timeSinceLevelLoad;
		float timeStep = duration * MUTING_STEP;
		do {
			// Mute the track
			targetMainTrackVolume = Math.Max(0, targetMainTrackVolume - MUTING_STEP);
			// And wait
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		} while(targetMainTrackVolume > 0);
	}

	public void unmuteTrack(float duration, float delay = 0) {
		// If duration is not positive, mute the track immediately
		if(duration <= 0) {
			targetMainTrackVolume = 1;
			mainTrack.volume = 1;
		}
		// Unmute the components, if necessary
		if(isMuted) {
			mainTrack.mute = false;
			proximitySubtrack.mute = false;
			mainTrack.volume = 2 * INAUDIBIBILITY_THRESHOLD;
			proximitySubtrack.volume = 2 * INAUDIBIBILITY_THRESHOLD;
			isMuted = false;
		}
		// Otherwise, do so gradually
		StopAllCoroutines();
		StartCoroutine(graduallyUnmuteTrack(duration, delay));
	}

	private IEnumerator graduallyUnmuteTrack(float duration, float delay) {
		float waitUntil = Time.timeSinceLevelLoad;
		// Handle the delay
		if(delay > 0) {
			waitUntil += delay;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		}
		// Mute the track
		float timeStep = duration * MUTING_STEP;
		do {
			targetMainTrackVolume = Math.Min(1, targetMainTrackVolume + MUTING_STEP);
			// And wait
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		} while(targetMainTrackVolume < 1);
	}

	public void updateProximityFactor(float proximityFactor) {
		targetProximityTrackVolume = proximityFactor * targetMainTrackVolume;
	}

	public void pause() {
		if(!isPaused) {
			mainTrack.Pause();
			proximitySubtrack.Pause();
			isPaused = true;
		}
	}

	public void unpause() {
		if(isPaused) {
			mainTrack.UnPause();
			proximitySubtrack.UnPause();
			isPaused = false;
		}
	}
}
