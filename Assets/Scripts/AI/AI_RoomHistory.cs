using System;

[Serializable]
public class AI_RoomHistory {

	public int roomId;
	public float width;
	public int countDoors;
	public bool hasItemSpawns;

	public double effectiveWidth;
	public double meanExplorationDistance;
	public double meanItemFetchDistance;
	public double meanDoorToDoorDistance;

	public float cumulativeWalkedDistance;

	public AI_RoomHistory(Data_Room room) {
		roomId = room.INDEX;
		width = room.width;
		countDoors = room.DOORS.Count;
		hasItemSpawns = room.hasItemSpawns;

		effectiveWidth = room.effectiveWidth;
		meanExplorationDistance = room.meanExplorationDistance;
		meanItemFetchDistance = room.meanItemFetchDistance;
		meanDoorToDoorDistance = room.meanDoorToDoorDistance;

		cumulativeWalkedDistance = 0;
	}

	public void increaseWalkedDistance(float increment) {
		if(increment > 0) {
			cumulativeWalkedDistance += increment;
		}
	}
}
