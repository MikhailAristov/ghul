using UnityEngine;

public class Data_Character {

    public string Name { get; private set; }
    public GameObject gameObj { get; private set; }
    public Control_PlayerCharacter control { get; private set; }
    public Data_Room isIn { get; private set; }
    public float pos { get; private set; }
    // Gameplay parameters:
    public float stamina { get; private set; } // goes from 0.0 to 1.0
    public bool exhausted { get; private set; }

    public Data_Character(string N, GameObject O)
    {
        this.Name = N;
        this.gameObj = O;
        this.control = O.GetComponent<Control_PlayerCharacter>();

        this.stamina = 1.0f;
        this.exhausted = false;
    }

    public override string ToString() { return this.Name; }

    public void moveToRoom(Data_Room R) {
        this.isIn = R;
    }

    public void updatePosition(float Pos) {
        this.pos = Pos;
    }

    // Updates the stamina meter with the specified amount (positive or negative), within boundaries
    // Sets the exhausted flag as necessary and returns the final value of the meter
    public float modifyStamina(float Delta)
    {
        float tempStamina = this.stamina + Delta;
        if(tempStamina >= 1.0f) {
            this.stamina = 1.0f;
            this.exhausted = false;
        } else if(tempStamina <= 0.0f) {
            this.stamina = 0.0f;
            this.exhausted = true;
        } else {
            this.stamina = tempStamina;
        }
        return this.stamina;
    }
}
