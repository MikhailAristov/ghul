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
		Name = N;
		gameObj = O;
		control = O.GetComponent<Control_Monster>();
		playerInSight = false;
		playerDetected = false;
		playerPosLastSeen = 0.0f;
		isRandomTargetSet = false;
		randomTargetPos = 0.0f;
		isThinking = false;
		remainingThinkingTime = 0.0f;
    }
    public Data_Monster(string gameObjectName)
    {
        Name = gameObjectName;
        gameObj = GameObject.Find(gameObjectName);
        control = gameObj.GetComponent<Control_Monster>();
        playerInSight = false;
        playerDetected = false;
        playerPosLastSeen = 0.0f;
        isRandomTargetSet = false;
        randomTargetPos = 0.0f;
        isThinking = false;
        remainingThinkingTime = 0.0f;
    }

    public override string ToString() { return Name; }

	public void moveToRoom(Data_Room R) {
		isIn = R;
	}

    // Complete position specification
    public void updatePosition(Data_Room R, float xPos)
    {
        pos = xPos;
        isIn = R;
    }
    // Quicker update of horizontal position
    public void updatePosition(float Pos) {
		pos = Pos;
	}
}
