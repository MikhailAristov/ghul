using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;

[Serializable]
public class Data_GameState {

    [NonSerialized]
    private Dictionary<string, float> SETTINGS;

    [SerializeField]
    public SortedList<int, Data_Room> ROOMS;
    [SerializeField]
    public SortedList<int, Data_Door> DOORS;

    [SerializeField]
    private Data_Character PLAYER_CHARACTER;

    private static string FILENAME_SAVE_RESETTABLE = "save1.dat";
    //private static string FILENAME_SAVE_PERMANENT  = "save2.dat";

    // Construct an empty game state
    public Data_GameState()
    {
        SETTINGS = new Dictionary<string, float>();
        ROOMS = new SortedList<int, Data_Room>();
        DOORS = new SortedList<int, Data_Door>();
        PLAYER_CHARACTER = null;
    }

    public void loadDefaultSetttings()
    {
        // Screen settings
        SETTINGS.Add("SCREEN_SIZE_HORIZONTAL", 6.4f);   // 640px
        SETTINGS.Add("SCREEN_SIZE_VERTICAL", 4.8f);     // 480px

        // Level generation setttings
        SETTINGS.Add("VERTICAL_ROOM_SPACING", -5.0f);   // Must be bigger than SCREEN_SIZE_VERTICAL

        // Level layout setttings
        SETTINGS.Add("HORIZONTAL_ROOM_MARGIN", 0.9f);   // Prevents movement to screen edge past the margin
        SETTINGS.Add("HORIZONTAL_DOOR_WIDTH", 1.35f);

        // Door settings
        SETTINGS.Add("MARGIN_DOOR_ENTRANCE", 0.6f);     // How close a character's center of mass must be to the door's center to use it
        SETTINGS.Add("DOOR_COOLDOWN_DURATION", 0.4f);

        // Character movement settings
        SETTINGS.Add("CHARA_WALKING_SPEED", 5.0f);
        SETTINGS.Add("CHARA_RUNNING_SPEED", 8.0f);
        // Stamina range: 0.0 .. 1.0; increments are applied per second
        SETTINGS.Add("RUNNING_STAMINA_LOSS", -0.2f);    // Must be negative
        SETTINGS.Add("WALKING_STAMINA_GAIN", 0.1f);
        SETTINGS.Add("STANDING_STAMINA_GAIN", 0.4f);

        // Miscellaneous setttings
        SETTINGS.Add("AUTOSAVE_FREQUENCY", 10.0f);       // In seconds
        SETTINGS.Add("CAMERA_PANNING_SPEED", 9.0f);
    }

    // Adds a room to the game state
    public void addRoom(string gameObjectName)
    {
        int INDEX = ROOMS.Count;
        ROOMS.Add(INDEX, new Data_Room(INDEX, gameObjectName));
    }

    // Adds a door to the game state, as well as to its containing room
    public void addDoor(string gameObjectName, int RoomIndex)
    {
        int INDEX = DOORS.Count;
        DOORS.Add(INDEX, new Data_Door(INDEX, gameObjectName));
        ROOMS[RoomIndex].addDoor(DOORS[INDEX], DOORS[INDEX].gameObj.transform.position.x);
    }

    // Connects two doors to each other
    public void connectTwoDoors(int fromIndex, int toIndex)
    {
        DOORS[fromIndex].connectTo(DOORS[toIndex]);
    }

    // Sets the player character object
    public void setPlayerCharacter(string gameObjectName)
    {
        PLAYER_CHARACTER = new Data_Character(gameObjectName);
    }

    // Returns the player character object
    public Data_Character getCHARA()
    {
        return PLAYER_CHARACTER;
    }

    // Returns the value of a game setting
    public float getSetting(string Name)
    {
        if (SETTINGS.ContainsKey(Name)) {
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
            throw new System.ArgumentException("There is no room #" + I);
        }
    }

    // Returns a Door object to a given index, if it exists
    public Data_Door getDoorByIndex(int I)
    {
        if (DOORS.ContainsKey(I)) {
            return DOORS[I];
        } else {
            throw new System.ArgumentException("There is no door #" + I);
        }
    }

    // Saves the current game state to disk
    [MethodImpl(MethodImplOptions.Synchronized)] // Synchronized to avoid simultaneous calls from parallel threads
    public static void saveToDisk(Data_GameState GS)
    {
        // Set the save file paths
        string resettableFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_RESETTABLE;
        //string permanentFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_PERMANENT;

        Debug.Log("Saving game to " + resettableFilePath);

        // Prepare writing file
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(resettableFilePath);

        // Write the game state to file and close it
        bf.Serialize(file, GS);
        file.Close();
    }
}
