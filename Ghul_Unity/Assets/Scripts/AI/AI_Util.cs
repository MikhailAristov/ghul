using UnityEngine;
using System.Linq;
using System.Collections;

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
		
	public static void displayVector(string preface, double[] input) {
		int length = input.GetLength(0);

		string output = "" + preface + "\n";
		for(int i = 0; i < length; i++) {
			output += string.Format(" {0:F6}", input[i]);
		}
		output += ("\nSUM: " + input.Sum().ToString());

		Debug.Log(output);
	}
}
