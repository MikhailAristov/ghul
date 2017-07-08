using UnityEngine;
using System.Collections.Generic;

public class Control_AnimationSoundsMonster : Control_AnimationSounds {

	public Control_GameState gameStateControl;
	private int lastLaughedInChapter = 0;

	public void playRandomMonsterVoiceSnippet() {
		if(GenericSounds != null && (!CheckDistanceToCamera || MainCameraControl.canHearObject(gameObject))) {
			if(lastLaughedInChapter != gameStateControl.currentChapter) {
				lastLaughedInChapter = gameStateControl.currentChapter;
				playRandomFromPathInternal("Monster/Laughs", 1f);
			} else {
				playRandomFromPathInternal("Monster/Groans", 0.3f);
			}
		}
	}
}
