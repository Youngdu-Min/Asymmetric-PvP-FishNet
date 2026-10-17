using System.Collections.Generic;

public sealed class MapGenerationResult
{
    public List<RoomData> Rooms;
    public MapGrid Grid;
    public StartGoalResult StartGoalResult;
    public int PropBakeSeed;
}