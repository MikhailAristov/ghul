﻿using UnityEngine;
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

	public static void displayMatrix(string preface, int[,] input) {
		int height = input.GetLength(0);
		int width = input.GetLength(1);

		string output = "" + preface;
		for(int i = 0; i < height; i++) {
			output += "\n";
			for(int j = 0; j < width; j++) {
				output += string.Format(" {0:D}", input[i, j]);
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

	public static int pickRandomWeightedElement(float[] weights) {
		// Normalize and sum up the weights
		float sum = 0;
		for(int i = 0; i < weights.Length; i++) {
			if(weights[i] < 0) {
				weights[i] = 0;
			}
			sum += weights[i];
		}
		// Check the sum is great than zero
		if(sum <= 0) {
			throw new ArgumentOutOfRangeException("sum = " + sum + ", but must be a non-zero positive number!");
		}
		// Pick a random number and pick a random corresponding element
		float random = UnityEngine.Random.Range(0, sum);
		int result = -1;
		do {
			result += 1;
			random -= weights[result];
		} while(random > 0 || weights[result] == 0);
		return result;
	}

	// Clears or creates an empty vector
	public static void initializeVector<T>(ref T[] vector, int size) {
		if(vector == null || vector.Length != size) {
			vector = new T[size];
		} else {
			Array.Clear(vector, 0, size);
		}
	}

	// Clears or creates an empty 2D matrix
	public static void initializeMatrix<T>(ref T[,] matrix, int size0, int size1) {
		if(matrix == null || matrix.GetLength(0) != size0 || matrix.GetLength(1) != size1) {
			matrix = new T[size0, size1];
		} else {
			Array.Clear(matrix, 0, size0 * size1);
		}
	}

	// Copies a matrix from a reference matrix
	public static void copyMatrix<T>(ref T[,] copyFrom, ref T[,] copyTo) {
		if(copyFrom == null) {
			throw new InvalidOperationException();
		}
		if(copyTo == null || copyFrom.GetLength(0) != copyTo.GetLength(0) || copyFrom.GetLength(1) != copyTo.GetLength(1)) {
			copyTo = new T[copyFrom.GetLength(0), copyFrom.GetLength(1)];
		}
		Array.Copy(copyFrom, copyTo, copyFrom.GetLength(0) * copyFrom.GetLength(1));
	}

	// Clears or creates an empty 3D matrix
	public static void initializeMatrix<T>(ref T[,,] matrix, int size0, int size1, int size2) {
		if(matrix == null || matrix.GetLength(0) != size0 || matrix.GetLength(1) != size1 || matrix.GetLength(2) != size2) {
			matrix = new T[size0, size1, size2];
		} else {
			Array.Clear(matrix, 0, size0 * size1 * size2);
		}
	}
}
