using System.Collections.Generic;
using UnityEngine;

public sealed class CorridorData
{
    public int RoomA;
    public int RoomB;
    public List<Vector2Int> Centerline;
    public HashSet<Vector2Int> ExpandedCells;
}