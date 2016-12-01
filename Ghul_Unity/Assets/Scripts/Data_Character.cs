using UnityEngine;
using System;

[Serializable]
public class Data_Character {

    public string Name;
    public GameObject gameObj;
    public Control_PlayerCharacter control;
    public Data_Room isIn;
    public float pos;
    // Gameplay parameters:
    public float stamina; // goes from 0.0 to 1.0
    public bool exhausted;

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
