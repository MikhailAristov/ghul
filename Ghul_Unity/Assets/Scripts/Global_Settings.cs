using UnityEngine;
using System.Collections.Generic;

public class Global_Settings {

	private static Dictionary<string, float> LIST;

	private static void loadSettings() {
		LIST = new Dictionary<string, float> {
			// Screen settings
			{ "SCREEN_SIZE_HORIZONTAL",			6.4f },		// 640px
			{ "SCREEN_SIZE_VERTICAL",			4.8f },		// 480px

			// Level generation setttings
			{ "VERTICAL_ROOM_SPACING",			-5.0f },	// Must be bigger than SCREEN_SIZE_VERTICAL
			{ "TOTAL_NUMBER_OF_ROOMS",			17.0f },	// How many rooms are to there to be in the game, including the Ritual Room; must be more than 5!
			{ "TOTAL_NUMBER_OF_ITEMS_PLACED",	10.0f },	// needs casting to int when used

			// Level layout setttings
			{ "HORIZONTAL_ROOM_MARGIN",			0.5625f },	// Prevents movement to screen edge past the margin
			{ "HORIZONTAL_DOOR_WIDTH",			1.6875f },

			// Ritual room settings
			{ "RITUAL_ROOM_INDEX",				0.0f },		// The index of the room with the ritual pentagram (has to be cast to int)
			{ "RITUAL_PENTAGRAM_CENTER",		2.7375f },	// The center pentagram space (relative to room center)
			{ "RITUAL_PENTAGRAM_RADIUS",		0.9f },		// The distance between pentagram center and edge in either direction
			{ "RITUAL_ITEMS_REQUIRED",			5.0f },		// How many items are needed for the ritual (has to be cast to int)
			{ "RITUAL_ITEM_PLACEMENT",			1.0f },		// How long, in seconds, it takes to place an item on the pentagram

			// Door settings
			{ "MARGIN_DOOR_ENTRANCE",			0.8625f },	// How close a character's center of mass must be to the door's center to use it
			{ "DOOR_TRANSITION_DURATION",		0.4f },		// How long it takes to go through a door
			{ "DOOR_OPEN_DURATION",				1.0f },		// How long a door stays open after someone goes through it

			// Character movement settings
			{ "CHARA_WALKING_SPEED",			1.95f },
			{ "CHARA_RUNNING_SPEED",			4.275f },
			{ "CHARA_SINGLE_STEP_LENGTH",		0.8f },		// this is how many (virtual) meters chara can walk before making a noise
			{ "SUICIDLE_DURATION",				30.0f },	// How long the suicidle animation takes

			// Monster settings
			{ "MONSTER_WALKING_SPEED",			2.75f },
			{ "MONSTER_SLOW_WALKING_SPEED",		1.25f },	// when the monster randomly walks around
			{ "MONSTER_ATTACK_RANGE",			2.55f },	// the distance from the monster that it main attack hits
			{ "MONSTER_ATTACK_MARGIN",			0.5f },		// the size of the "hitbox" for the monster's attack
			{ "MONSTER_ATTACK_DURATION",		0.5f },		// how long, in seconds, does an attack take from start to hit
			{ "MONSTER_ATTACK_COOLDOWN",		0.5f },		// how long, in seconds, does an attack take from hit to finish
			{ "MONSTER_WAIT_FOR_TONI_MOVE",		2.0f },		// how long, in seconds, does the monster wait for Toni to move in a face-off

			{ "MONSTER_HOLDS_DOORS_AFTER_ITEM",	2.0f },		// after how many placed items the monster learns to hold doors shut
			{ "MONSTER_INVISIBLE_AFTER_ITEM",	3.0f },		// after how many placed items the monster learns to turn invisible
			{ "MONSTER_INVISIBILIY_TRANSITION",	0.25f },	// how long it takes, in seconds, to turn invisible and back

			{ "TONI_ATTACK_MARGIN",				1.0f },		// the size of the "hitbox" for the monster Toni's attack
			{ "TONI_ATTACK_DURATION",			0.5f },		// how long, in seconds, does an attack take from start to hit
			{ "TONI_ATTACK_COOLDOWN",			0.5f },		// how long, in seconds, does an attack take from hit to finish

			// Stamina range: 0.0 .. 1.0; increments are applied per second
			{ "RUNNING_STAMINA_LOSS",			-0.2f },	// Must be negative
			{ "WALKING_STAMINA_GAIN",			0.1f },
			{ "STANDING_STAMINA_GAIN",			0.4f },

			// Item settings
			{ "MARGIN_ITEM_COLLECT",			0.6f },		// How close a character's center must be to an item to be able to collect it
			{ "ITEM_CARRY_ELEVATION",			-1.2f },	// Distance from the horizontal center of the room at which items are carried by chara
			{ "ITEM_FLOOR_LEVEL",				-1.7625f },	// Distance from the horizontal center of the room at which items are lying on the floor
			{ "INVENTORY_DISPLAY_DURATION",		2.0f },		// How long the inventory overlay is shown when invoked
			{ "ITEM_PICKUP_DURATION",			1.3f },		// How long Toni needs to pick up an item
			{ "ITEM_ZAP_DURATION",				1.6f },		// How long Toni is disabled after trying to pick up a wrong item

			// Artificial intelligence and modelling settings
			{ "TIME_STEP",		Time.fixedDeltaTime },		// ...

			// Miscellaneous setttings
			{ "AUTOSAVE_FREQUENCY",				10.0f },	// In seconds
			{ "CAMERA_PANNING_SPEED",			9.0f },
			{ "TOTAL_DEATH_DURATION",			4.5f },		// When deathDuration of Data_Character reaches this value the player resets to the starting room
			{ "ENCOUNTER_JINGLE_DURATION",		3.0f },
			{ "ROOMS_UNLOCKED_AT_ZERO_ITEMS",	5.0f },		// How many rooms are accessible at the start of the game
			{ "ROOMS_UNLOCKED_AFTER_ONE_ITEM",	10.0f },	// How many rooms are accessible after placing the first item on the pengram
			// (Placing the second item unlocks all remaining rooms!)

			// Comma after the last pair of values is okay, the compiler doesn't care
		};
	}

	public static float read(string Name) {
		// Initialize on first read
		if(LIST == null) { loadSettings(); }

		// Check if setting exists and return it, otherwise throw exception
		if (LIST.ContainsKey(Name)) {
			return LIST[Name];
		} else {
			throw new System.ArgumentException("Setting " + Name + " is not defined"); //Nabil was here
		}
	} 
}
