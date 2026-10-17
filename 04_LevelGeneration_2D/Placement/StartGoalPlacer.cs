using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StartGoalPlacer
{
    public static StartGoalResult Place(
        List<RoomData> rooms,
        RoomTopologyResult topology,
        Vector2Int mapSize,
        StartGoalConfig config,
        IRandomSource random)
    {
        var result = new StartGoalResult();

        if (rooms.Count < 2)
        {
            result.Success = false;
            result.FailureReason = "Need at least 2 rooms.";
            return result;
        }

        var adjacency = RoomAdjacencyGraph.Build(topology, rooms.Count);

        // 더블 스윕 → Start
        int seedRoom = random.NextInt(0, rooms.Count);
        var firstSweep = RoomAdjacencyGraph.BfsDistances(adjacency, seedRoom);

        if (firstSweep.Count < rooms.Count)
        {
            result.Success = false;
            result.FailureReason = $"Graph disconnected: {firstSweep.Count}/{rooms.Count} reachable.";
            return result;
        }

        int roomA = firstSweep.OrderByDescending(kv => kv.Value).First().Key;
        var secondSweep = RoomAdjacencyGraph.BfsDistances(adjacency, roomA);

        // Goal 후보: roomA 제외, 그래프 거리 내림차순 상위 N개
        var goalCandidates = secondSweep
            .Where(kv => kv.Key != roomA)
            .OrderByDescending(kv => kv.Value)
            .Take(config.GoalCandidateCount)
            .ToList();

        float mapDiagonal = Vector2.Distance(Vector2.zero, (Vector2)mapSize);
        var startCenter = rooms[roomA].Bounds.position + rooms[roomA].Bounds.size / 2;

        result.StartRoomIndex = roomA;
        result.StartPosition = startCenter;

        foreach (var kv in goalCandidates)
        {
            var center = rooms[kv.Key].Bounds.position + rooms[kv.Key].Bounds.size / 2;
            result.GoalRoomIndices.Add(kv.Key);
            result.GoalPositions.Add(center);
            result.GoalGraphDistances.Add(kv.Value);
        }

        // 랜덤 선택
        int selectedIdx = random.NextInt(0, result.GoalRoomIndices.Count);
        result.SelectedGoalRoomIndex = result.GoalRoomIndices[selectedIdx];
        result.SelectedGoalPosition = result.GoalPositions[selectedIdx];

        int bestGraphDist = result.GoalGraphDistances[0];
        float straightDist = Vector2Int.Distance(startCenter, result.GoalPositions[selectedIdx]);
        result.StraightLineRatio = straightDist / mapDiagonal;
        result.MetMinGraphDistance = bestGraphDist >= config.MinGraphDistance;
        result.MetMinStraightLineRatio = result.StraightLineRatio >= config.MinStraightLineRatio;
        result.Success = result.MetMinGraphDistance;

        if (!result.Success)
            result.FailureReason = $"Best graph distance {bestGraphDist} < MinGraphDistance {config.MinGraphDistance}.";

        return result;
    }
}