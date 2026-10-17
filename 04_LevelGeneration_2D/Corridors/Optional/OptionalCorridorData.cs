using System.Collections.Generic;
using UnityEngine;

public sealed class OptionalCorridorData
{
    public CorridorTerminationType Type;
    public List<Vector2Int> Centerline;
    public HashSet<Vector2Int> ExpandedCells;
}