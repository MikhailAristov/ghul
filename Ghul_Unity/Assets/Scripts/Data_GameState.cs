using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Data_GameState {

    private Dictionary<string, float> SETTINGS;

    public SortedList<int, Data_Room> ROOMS;
    public SortedList<int, Data_Door> DOORS;

    private Data_Character PLAYER_CHARACTER;

    // Construct an empty game state
    public Data_GameState()
    {
        this.SETTINGS = new Dictionary<string, float>();
        this.ROOMS = new SortedList<int, Data_Room>();
        this.DOORS = new SortedList<int, Data_Door>();
        this.PLAYER_CHARACTER = null;
    }

    public void loadDefaultSetttings()
    {
        // Screen settings
        SETTINGS.Add("SCREEN_SIZE_HORIZONTAL", 6.4f);   // 640px
        SETTINGS.Add("SCREEN_SIZE_VERTICAL", 4.8f);     // 480px

        // Level generation setttings
        SETTINGS.Add("VERTICAL_ROOM_SPACING", -5.0f);    // must be bigger than SCREEN_SIZE_VERTICAL

        // Level layout setttings
        SETTINGS.Add("HORIZONTAL_ROOM_MARGIN", 0.9f);   // prevents movement to screen edge past the margin
        SETTINGS.Add("HORIZONTAL_DOOR_WIDTH", 1.35f);
        SETTINGS.Add("MARGIN_DOOR_ENTRANCE", 0.6f);     // How close a character's center of mass must be to the door's center to use it

        // Character movement settings
        SETTINGS.Add("CHARA_WALKING_SPEED", 5.0f);
        SETTINGS.Add("CHARA_RUNNING_SPEED", 8.0f);
        // Stamina range: 0.0 .. 1.0; increments are applied per second
        SETTINGS.Add("RUNNING_STAMINA_LOSS", -0.2f);   // Must be negative
        SETTINGS.Add("WALKING_STAMINA_GAIN", 0.1f);
        SETTINGS.Add("STANDING_STAMINA_GAIN", 0.4f);

        // Miscellaneous setttings
        SETTINGS.Add("DOOR_COOLDOWN_DURATION", 0.3f);
        SETTINGS.Add("CAMERA_PANNING_SPEED", 9.0f);
    }

    // Adds a room to the game state
    public void addRoom(GameObject R)
    {
        int INDEX = this.ROOMS.Count;
        this.ROOMS.Add(INDEX, new Data_Room(INDEX, R));
    }

    // Adds a door to the game state, as well as to its containing room
    public void addDoor(GameObject D, int RoomIndex)
    {
        int INDEX = this.DOORS.Count;
        this.DOORS.Add(INDEX, new Data_Door(INDEX, D));
        this.ROOMS[RoomIndex].addDoor(DOORS[INDEX], DOORS[INDEX].gameObj.transform.position.x);
    }

    // Connects two doors to each other
    public void connectTwoDoors(int fromIndex, int toIndex)
    {
        this.DOORS[fromIndex].connectTo(DOORS[toIndex]);
    }

    // Sets the player character object
    public void setPlayerCharacter(string name, GameObject O)
    {
        this.PLAYER_CHARACTER = new Data_Character(name, O);
    }

    // Returns the player character object
    public Data_Character getCHARA()
    {
        return this.PLAYER_CHARACTER;
    }

    // Update the player character's position
    public void updatePlayerCharacterPosition()
    {
        this.PLAYER_CHARACTER.updatePosition(this.PLAYER_CHARACTER.gameObj.transform.position.x);
        Debug.Log(PLAYER_CHARACTER + " is in room #" + PLAYER_CHARACTER.isIn + " at position " + PLAYER_CHARACTER.pos);
    }

    // Returns the value of a game setting
    public float getSetting(string Name)
    {
        if (this.SETTINGS.ContainsKey(Name)) {
            return SETTINGS[Name];
        } else {
            throw new System.ArgumentException("Setting " + Name + " is not defined", "original");
        }
    }

    // Returns a Room object to a given index, if it exists
    public Data_Room getRoomByIndex(int I)
    {
        if(ROOMS.ContainsKey(I)) {
            return ROOMS[I];
        } else {
            throw new System.ArgumentException("There is no room #" + I, "original");
        }
    }
}
