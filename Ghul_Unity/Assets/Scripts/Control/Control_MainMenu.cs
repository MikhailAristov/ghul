using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Control_MainMenu : MonoBehaviour {

	public Control_GameState GameStateControl;
	public Canvas MainMenuCanvas;

	public Image MainMenuBackground;
	public Sprite MainMenuWithStartButton;
	public Sprite MainMenuWithoutStartButton;

	public Image ControlsDisplayImage;
	public Animator ControlsDisplayAnimator;

	public Control_Monster MonsterControl;
	private string inputBuffer;

	private bool hidden;
	private bool newGameButtonDisabled;

	private float displayControlsDuration = 3f;
	private float displayControlsFadeDuration = 2f;

	void Start() {
		ControlsDisplayImage.enabled = false;
		inputBuffer = "";
	}

	void Update() {
		// Update the new game button state
		if(GameStateControl.newGameDisabled && !newGameButtonDisabled) {
			disableNewGameButton();
		} else if(newGameButtonDisabled && !GameStateControl.newGameDisabled) {
			reenableNewGameButton();
		}

		// Easter egg: summon Knolli Classic
		if(!hidden && !MonsterControl.knolliObject.activeSelf && Input.inputString != "") {
			inputBuffer = inputBuffer.Substring(Mathf.Max(0, inputBuffer.Length - 10)) + Input.inputString.ToLower();
			if(inputBuffer.EndsWith("knolli")) {
				MonsterControl.getKnolliClassic();
			}
		}
	}

	public void show() {
		if(hidden) {
			MainMenuCanvas.enabled = true;
			hidden = false;
		}
	}

	public void hide() {
		if(!hidden) {
			inputBuffer = "";
			MainMenuCanvas.enabled = false;
			hidden = true;
		}
	}

	private void disableNewGameButton() {
		if(!newGameButtonDisabled) {
			newGameButtonDisabled = true;
			MainMenuBackground.sprite = MainMenuWithoutStartButton;
		}
	}

	private void reenableNewGameButton() {
		if(newGameButtonDisabled) {
			newGameButtonDisabled = false;
			MainMenuBackground.sprite = MainMenuWithStartButton;
		}
	}

	// This method is called when the New Game button is activated from the main menu
	public void onNewGameSelect() {
		if(!hidden && !newGameButtonDisabled) {
			StartCoroutine(showControlsBeforeNewGame());
			hide();
		}
	}

	private IEnumerator showControlsBeforeNewGame() {
		float waitUntil = Time.timeSinceLevelLoad + displayControlsDuration;
		// Hide the menu, show the controls
		ControlsDisplayImage.CrossFadeColor(new Color(1f, 1f, 1f, 1f), 0, false, true);
		ControlsDisplayImage.enabled = true;
		hide();
		// Wait until time or any key pressed
		Input.ResetInputAxes();
		yield return new WaitUntil(() => (Time.timeSinceLevelLoad >= waitUntil || Input.anyKey));
		// Start a new game
		GameStateControl.startNewGame();
		// Fade away the image
		ControlsDisplayAnimator.speed = 0;
		ControlsDisplayImage.CrossFadeColor(new Color(0, 0, 0, 0), displayControlsFadeDuration, false, true);
		waitUntil += displayControlsFadeDuration;
		yield return new WaitUntil(() => (Time.timeSinceLevelLoad >= waitUntil));
		ControlsDisplayImage.enabled = false;
		ControlsDisplayAnimator.speed = 1f;
	}

	// This method is called when the Continue button is activated from the main menu
	public void onContinueSelect() {
		if(!hidden) {
			GameStateControl.continueOldGame();
			hide();
		}
	}

	// This method is called when the Exit button is activated from the main menu
	public void onExitSelect() {
		if(!hidden) {
			GameStateControl.quitGame();
		}
	}
}
