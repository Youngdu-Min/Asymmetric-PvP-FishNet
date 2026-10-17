using UnityEngine;

public sealed class RoomPlacementConfig
{
    public Vector2Int MapSize;
    public Vector2Int RoomMinSize;
    public Vector2Int RoomMaxSize;
    public int TargetRoomCount;
    public int MaxPlacementAttempts;
    public int MinAcceptedRoomCount;
    public int RoomBuffer = 1;
}