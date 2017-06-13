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

	private const bool SAVING_DISABLED = false; // For debugging purposes
	private const string FILENAME_SAVE_RESETTABLE = "save1.dat";
	private const string FILENAME_SAVE_PERMANENT  = "save2.dat";

	// Utility class for convenience
	private static BinaryFormatter _binFormatter;
	private static BinaryFormatter binFormatter {
		get { 
			if(_binFormatter == null) {
				_binFormatter = new BinaryFormatter();
			}
			return _binFormatter;
		}
		set { return; }
	}

	// Saves the current game state to disk
	public static void saveToDisk(Data_GameState GS) {
		// The second clause is just to avoid saving weird in-between states:
		if(!SAVING_DISABLED && GS.getToni().cooldown < 0.1f) {
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
		FileStream file = File.Create(filePath);
		binFormatter.Serialize(file, dataObject);
		file.Close();
	}

	// Returns a game state from disk; returns null if no saved state is found
	public static T loadFromDisk<T>()
	{
		// Set the save file paths
		string filePath = Application.persistentDataPath + "/";
		if(typeof(T) == typeof(Data_GameState)) {
			filePath += FILENAME_SAVE_RESETTABLE;
		} else if(typeof(T) == typeof(AI_PlayerParameters)) {
			filePath += FILENAME_SAVE_PERMANENT;
		} else {
			throw new ArgumentException(typeof(T).ToString() + " is not a valid persistence class!");
		}

		if(!File.Exists(filePath)) {
			Debug.Log("No data found in: " + filePath);
			return default(T);
		}

		try {
			Debug.Log("Loading data from " + filePath);

			FileStream file = File.Open(filePath, FileMode.Open);
			T result = (T)binFormatter.Deserialize(file);
			file.Close();

			return result;
		} catch(SerializationException) {
			Debug.LogWarning("The file " + filePath + " is corrupted, create a new data object instead!");
			return default(T);
		}  
	}
}
