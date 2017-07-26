using UnityEngine;
using System;
using System.Collections;

public class Control_MusicTrack : MonoBehaviour {

	private const float INAUDIBIBILITY_THRESHOLD = 0.01f;
	private const float HIGH_TENSION_VOLUME = 0.01f;
	private const float MUTING_STEP = 0.1f;
	private float MAX_TRACK_VOLUME;

	public const float LERPING_STEP = 0.01f;
	public const float LERPING_STEP_FAST = 0.1f;

	private AudioSource mainTrack;
	private AudioSource proximitySubtrack;
	private AudioSource chaseSubtrack;

	private float targetMainTrackVolume;
	private float targetProximityTrackVolume;
	private float targetChaseTrackVolume;
	private float minChaseTrackVolume;
	private bool isMuted;

	public bool muted {
		get { return isMuted; }
	}

	void Awake() {
		MAX_TRACK_VOLUME = Mathf.Clamp01(Global_Settings.read("TOP_MUSIC_VOLUME"));
		minChaseTrackVolume = INAUDIBIBILITY_THRESHOLD;
	}

	// Use this for initialization
	void Start () {
		AudioSource[] myTracks = GetComponents<AudioSource>();
		if(myTracks.Length == 3) {
			mainTrack = myTracks[0];
			proximitySubtrack = myTracks[1];
			chaseSubtrack = myTracks[2];
		} else {
			throw new ArgumentException(transform.name + " doesn't have exactly 3 tracks!");
		}
		// Automute on start
		muteTrack(0);
		isMuted = true;
	}

	// Update is called once per frame
	void Update () {
		if(!isMuted && !AudioListener.pause) {
			mainTrack.volume = Mathf.Lerp(mainTrack.volume, targetMainTrackVolume, LERPING_STEP);
			if(Mathf.Abs(proximitySubtrack.volume - targetProximityTrackVolume) > 0.0001f * MAX_TRACK_VOLUME) {
				proximitySubtrack.volume = Mathf.Lerp(proximitySubtrack.volume, targetProximityTrackVolume, (targetMainTrackVolume > 0.9f * MAX_TRACK_VOLUME ? LERPING_STEP : LERPING_STEP_FAST));
			}
			if(Mathf.Abs(chaseSubtrack.volume - targetChaseTrackVolume) > 0.0001f * MAX_TRACK_VOLUME) {
				chaseSubtrack.volume = Mathf.Lerp(chaseSubtrack.volume, targetChaseTrackVolume, LERPING_STEP);
			}
			if(mainTrack.volume < INAUDIBIBILITY_THRESHOLD) {
				isMuted = true;
				mainTrack.mute = true;
				proximitySubtrack.mute = true;
				chaseSubtrack.mute = true;
			}
		}
	}

	public void muteTrack(float duration) {
		// If duration is not positive, mute the track immediately
		if(duration <= 0) {
			targetMainTrackVolume = 0;
			targetProximityTrackVolume = 0;
			targetChaseTrackVolume = 0;
			mainTrack.volume = 0;
			proximitySubtrack.volume = 0;
			chaseSubtrack.volume = 0;
			return;
		}
		// Otherwise, do so gradually
		StopAllCoroutines();
		StartCoroutine(graduallyMuteTrack(duration));
	}

	private IEnumerator graduallyMuteTrack(float duration) {
		float waitUntil = Time.timeSinceLevelLoad, timeStep = duration * MUTING_STEP, prevMainVolume;
		do {
			// Mute the track
			prevMainVolume = targetMainTrackVolume;
			targetMainTrackVolume = Math.Max(0, targetMainTrackVolume - MUTING_STEP);
			targetProximityTrackVolume *= (targetMainTrackVolume / prevMainVolume);
			// And wait
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		} while(targetMainTrackVolume > 0);
	}

	public void unmuteTrack(float duration, float delay = 0, bool restart = false) {
		// If duration is not positive, mute the track immediately
		if(duration <= 0) {
			targetMainTrackVolume = MAX_TRACK_VOLUME;
			mainTrack.volume = MAX_TRACK_VOLUME;
			unmuteAllComponents();
		}
		// Restart the tracks if necessary
		if(restart) {
			mainTrack.Play();
			proximitySubtrack.Play();
			chaseSubtrack.Play();
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
		// Unmute the track
		unmuteAllComponents();
		float timeStep = duration * MUTING_STEP;
		do {
			targetMainTrackVolume = Math.Min(MAX_TRACK_VOLUME, targetMainTrackVolume + MUTING_STEP);
			// And wait
			waitUntil += timeStep;
			yield return new WaitUntil(() => Time.timeSinceLevelLoad >= waitUntil);
		} while(targetMainTrackVolume < MAX_TRACK_VOLUME);
	}

	// Unmute the components, if necessary
	private void unmuteAllComponents() {
		if(isMuted) {
			mainTrack.mute = false;
			proximitySubtrack.mute = false;
			chaseSubtrack.mute = false;
			mainTrack.volume = 2 * INAUDIBIBILITY_THRESHOLD;
			isMuted = false;
		}
	}

	public void updateProximityFactor(float proximityFactor) {
		targetProximityTrackVolume = proximityFactor * targetMainTrackVolume;
	}

	public void setBeingChased(bool state) {
		targetChaseTrackVolume = state ? MAX_TRACK_VOLUME : minChaseTrackVolume;
	}

	public void rampTensionUp(bool state) {
		minChaseTrackVolume = state ? HIGH_TENSION_VOLUME * MAX_TRACK_VOLUME : INAUDIBIBILITY_THRESHOLD;
		// Update the chase track volume itself if necessary
		if((state && targetChaseTrackVolume < minChaseTrackVolume) || (!state && targetChaseTrackVolume > minChaseTrackVolume)) {
			targetChaseTrackVolume = minChaseTrackVolume;
		}
	}
}
