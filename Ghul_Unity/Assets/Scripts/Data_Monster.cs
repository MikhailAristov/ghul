using UnityEngine;

public class Data_Monster {

	public string Name { get; private set; }
	public GameObject gameObj { get; private set; }
	public Control_Monster control { get; private set; }
	public Data_Room isIn { get; private set; }
	public float pos { get; private set; }
	public bool playerInSight { get; set; }
	public bool playerDetected { get; set; } // defines whether the monster knows where to go
	public float playerPosLastSeen { get; set; }
	public bool isRandomTargetSet { get; set; } // define whether the monster made up its mind where to go
	public float randomTargetPos { get; set; }
	public bool isThinking { get; set; }
	public float remainingThinkingTime { get; set; }

	public Data_Monster(string N, GameObject O) {
		this.Name = N;
		this.gameObj = O;
		this.control = O.GetComponent<Control_Monster>();
		this.playerInSight = false;
		this.playerDetected = false;
		this.playerPosLastSeen = 0.0f;
		this.isRandomTargetSet = false;
		this.randomTargetPos = 0.0f;
		this.isThinking = false;
		this.remainingThinkingTime = 0.0f;
	}

	public override string ToString() { return this.Name; }

	public void moveToRoom(Data_Room R) {
		this.isIn = R;
	}

	public void updatePosition(float Pos) {
		this.pos = Pos;
	}
}
