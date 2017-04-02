using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class AI_Util {

	public static void displayMatrix(string preface, double[,] input) {
		int height = input.GetLength(0);
		int width = input.GetLength(1);

		string output = "" + preface;
		for(int i = 0; i < height; i++) {
			output += "\n";
			for(int j = 0; j < width; j++) {
				output += string.Format(" {0:F6}", input[i, j]);
			}
		}

		Debug.Log(output);
	}

	public static void displayMatrix(string preface, float[,] input) {
		int height = input.GetLength(0);
		int width = input.GetLength(1);

		string output = "" + preface;
		for(int i = 0; i < height; i++) {
			output += "\n";
			for(int j = 0; j < width; j++) {
				output += string.Format(" {0:F6}", input[i, j]);
			}
		}

		Debug.Log(output);
	}
		
	public static void displayVector(string preface, double[] input) {
		int length = input.GetLength(0);

		string output = "" + preface + "\n";
		for(int i = 0; i < length; i++) {
			output += string.Format(" {0:F6}", input[i]);
		}
		output += ("\nSUM: " + input.Sum().ToString());

		Debug.Log(output);
	}

	public static float[,] subtractMatrices(float[,] minuend, float[,] subtrahend) {
		int height = minuend.GetLength(0);
		int width = minuend.GetLength(1);

		if(height != subtrahend.GetLength(0) || width != subtrahend.GetLength(1)) {
			throw new ArgumentException();
		}

		float[,] result = new float[height, width];
		for(int i = 0; i < height; i++) {
			for(int j = 0; j < width; j++) {
				result[i, j] = minuend[i, j] - subtrahend[i, j];
			}
		}

		return result;
	}

	public static void shuffleList<T>(List<T> input) {
		int count = input.Count;
		int last = count - 1;
		for(int i = 0; i < last; i++) {
			int r = UnityEngine.Random.Range(i, count);
			T tmp = input[i];
			input[i] = input[r];
			input[r] = tmp;
		}
	}
}
