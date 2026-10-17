using System.Collections.Generic;
using UnityEngine;

public static class SpikeValidator
{
    public static bool IsFullyConnected(HashSet<Vector2Int> corridorCells)
    {
        if (corridorCells.Count == 0) return false;

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();

        var start = new List<Vector2Int>(corridorCells)[0];
        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] dirs4 = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in dirs4)
            {
                var next = current + dir;
                if (corridorCells.Contains(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return visited.Count == corridorCells.Count;
    }

    public static bool HasOverlapWithRooms(HashSet<Vector2Int> corridorCells, List<RoomData> rooms, RoomData excludeStart, RoomData excludeEnd)
    {
        foreach (var room in rooms)
        {
            if (room == excludeStart || room == excludeEnd) continue;

            foreach (var pos in room.Bounds.allPositionsWithin)
            {
                if (corridorCells.Contains(pos))
                    return true;
            }
        }
        return false;
    }

    public static bool IsWithinMapBounds(HashSet<Vector2Int> corridorCells, Vector2Int mapSize)
    {
        foreach (var cell in corridorCells)
        {
            if (cell.x < 0 || cell.x >= mapSize.x || cell.y < 0 || cell.y >= mapSize.y)
                return false;
        }
        return true;
    }

    // 신규: Door Candidate가 0개로 떨어졌는데도 fallback으로 넘어가는 걸 잡기 위한 체크
    public static bool HasValidDoorCandidates(List<DoorCandidate> candidatesA, List<DoorCandidate> candidatesB)
    {
        return candidatesA != null && candidatesB != null
            && candidatesA.Count > 0 && candidatesB.Count > 0;
    }
}