using UnityEngine;

public class Data_Character {

    public string Name { get; private set; }
    public GameObject gameObj { get; private set; }
    public Control_PlayerCharacter control { get; private set; }
    public Data_Room isIn { get; private set; }
    public float pos { get; private set; }

    public Data_Character(string N, GameObject O)
    {
        this.Name = N;
        this.gameObj = O;
        this.control = O.GetComponent<Control_PlayerCharacter>();
    }

    public override string ToString() { return this.Name; }

    public void moveToRoom(Data_Room R)
    {
        this.isIn = R;
    }

    public void updatePosition(float Pos)
    {
        this.pos = Pos;
    }
}
