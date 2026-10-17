using System.Collections.Generic;

public sealed class RoomPlacementResult
{
    public List<RoomData> Rooms = new List<RoomData>();
    public int AttemptsUsed;
    public bool Success;
    public string FailureReason;
}