using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;
using System.Linq;

// This class manages the saving and loading of persistent data for the game
public abstract class Control_Persistence {

	private static bool SAVING_DISABLED = false; // For debugging purposes
	private static string FILENAME_SAVE_RESETTABLE = "save1.dat";
	private static string FILENAME_SAVE_PERMANENT  = "save2.dat";

	// Saves the current game state to disk
	public static void saveToDisk(Data_GameState GS) {
		// The second clause is just to avoid saving weird in-between states:
		if(!SAVING_DISABLED && GS.getToni().etherialCooldown < 0.1f) {
			writeToFile(Application.persistentDataPath + "/" + FILENAME_SAVE_RESETTABLE, GS);
		}
	}

	// Saves the current game state to disk
	public static void saveToDisk(AI_PlayerParameters PP) {
		// The second clause is just to avoid saving weird in-between states:
		if(!SAVING_DISABLED) {
			writeToFile(Application.persistentDataPath + "/" + FILENAME_SAVE_PERMANENT, PP);
		}
	}

	// Synchronized to avoid simultaneous calls from parallel threads
	[MethodImpl(MethodImplOptions.Synchronized)] 
	private static void writeToFile(string filePath, object dataObject) {
		// Prepare writing file
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(filePath);

		// Write the game state to file and close it
		bf.Serialize(file, dataObject);
		file.Close();
	}

	// Returns a game state from disk; returns null if no saved state is found
	public static Data_GameState loadFromDisk()
	{
		// Set the save file paths
		string resettableFilePath = Application.persistentDataPath + "/" + FILENAME_SAVE_RESETTABLE;
		if(!File.Exists(resettableFilePath))
		{
			Debug.Log("No game state found in: " + resettableFilePath);
			return null;
		}

		try {
			Debug.Log("Loading game from " + resettableFilePath);

			// Prepare opening the file
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(resettableFilePath, FileMode.Open);

			// Read the file to memory and close it
			Data_GameState result = (Data_GameState)bf.Deserialize(file);
			file.Close();

			return result;
		} catch(SerializationException) {
			Debug.LogWarning("The saved game " + resettableFilePath + " is corrupted, starting a new game instead");
			return null;
		}  
	}
}
