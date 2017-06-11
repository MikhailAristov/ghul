using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Control_MainMenu : MonoBehaviour {

	public Control_GameState GameStateControl;
	public Canvas MainMenuCanvas;
	public GameObject NewGameButton;

	private bool hidden;
	private bool newGameButtonDisabled;

	void Update() {
		// Update the new game button state
		if(GameStateControl.newGameDisabled && !newGameButtonDisabled) {
			disableNewGameButton();
		} else if(newGameButtonDisabled && !GameStateControl.newGameDisabled) {
			reenableNewGameButton();
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
			MainMenuCanvas.enabled = false;
			hidden = true;
		}
	}

	private void disableNewGameButton() {
		if(!newGameButtonDisabled) {
			newGameButtonDisabled = true;
			NewGameButton.GetComponent<Image>().color = new Color(100f / 255f, 0f, 0f);
		}
	}

	private void reenableNewGameButton() {
		if(newGameButtonDisabled) {
			newGameButtonDisabled = false;
			NewGameButton.GetComponent<Image>().color = new Color(136f / 255f, 136f / 255f, 136f / 255f);
		}
	}

	// This method is called when the New Game button is activated from the main menu
	public void onNewGameSelect() {
		if(!hidden && !newGameButtonDisabled) {
			GameStateControl.startNewGame();
			hide();
		}
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
