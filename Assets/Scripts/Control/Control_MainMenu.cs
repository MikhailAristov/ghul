using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Control_MainMenu : MonoBehaviour {

	public Control_GameState GameStateControl;
	public Canvas MainMenuCanvas;
	public Button NewGameButton;
	public Button ContinueButton;
	public GameObject EscapeBlinker;

	public Image ControlsDisplayImage;
	public Animator ControlsDisplayAnimator;

	public Control_Monster MonsterControl;
	private string inputBuffer;

	private bool isLaunchMenu;

	private bool hidden {
		get { return !MainMenuCanvas.enabled; }
	}

	private bool newGameButtonDisabled {
		get { return !NewGameButton.interactable; }
	}

	private bool continueButtonDisabled {
		get { return !ContinueButton.interactable; }
	}

	private float displayControlsDuration = 3f;
	private float displayControlsFadeDuration = 2f;

	void Start() {
		ControlsDisplayImage.enabled = false;
		isLaunchMenu = true;
		inputBuffer = "";
	}

	void Update() {
		// Update the new game button state
		if(GameStateControl.newGameDisabled && !newGameButtonDisabled) {
			disableNewGameButton();
		} else if(newGameButtonDisabled && !GameStateControl.newGameDisabled) {
			reenableNewGameButton();
		}

		// Dito for the continue button
		if((!isLaunchMenu || GameStateControl.InExpoMode) && !continueButtonDisabled) {
			hideContinueButton();
		} 

		// And the escape blinker
		if(!isLaunchMenu && !EscapeBlinker.activeSelf) {
			showEscapeBlinker();
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
			Cursor.visible = true;
			AudioListener.pause = true;
			// Move the mouse the center of the screen
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.lockState = CursorLockMode.None;   
		}
	}

	public void hide() {
		if(!hidden) {
			inputBuffer = "";
			isLaunchMenu = false;
			MainMenuCanvas.enabled = false;
			Cursor.visible = false;
			AudioListener.pause = false;
		}
	}

	private void disableNewGameButton() {
		if(!newGameButtonDisabled) {
			NewGameButton.interactable = false;
		}
	}

	private void reenableNewGameButton() {
		if(newGameButtonDisabled) {
			NewGameButton.interactable = true;
		}
	}

	private void hideContinueButton() {
		if(!continueButtonDisabled) {
			ContinueButton.interactable = false;
		}
	}

	private void showEscapeBlinker() {
		EscapeBlinker.SetActive(true);
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
