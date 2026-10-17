using System.Collections.Generic;
using UnityEngine;

public sealed class ValidationResult
{
    // 접근성
    public bool AllRoomsReachable;
    public List<int> UnreachableRoomIndices = new List<int>();

    // 셀 통계
    public int TotalRoomCells;
    public int TotalCorridorCells;
    public float CorridorToRoomRatio;

    // 단계별 요약
    public int RoomCount;
    public int MandatoryCorridorCount;
    public int OptionalCorridorCount;
    public int RemovedDeadEndCount;
    public int KeptDeadEndCount;
    public int StartGoalGraphDistance;
    public float StartGoalStraightLineRatio;

    // 최종 판정
    public bool Success;
    public List<string> FailureReasons = new List<string>();
}

public static class MapValidator
{
    private static readonly Vector2Int[] Dirs4 = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static ValidationResult Validate(
        List<RoomData> rooms,
        MapGrid grid,
        MandatoryCorridorResult mandatoryResult,
        OptionalCorridorResult optionalResult,
        DeadEndCleanupResult cleanupResult,
        StartGoalResult startGoalResult,
        GenerationDiagnostics diagnostics)
    {
        var result = new ValidationResult();

        // ── 셀 카운트 ──────────────────────────────────────────
        for (int x = 0; x < grid.Size.x; x++)
        {
            for (int y = 0; y < grid.Size.y; y++)
            {
                var cell = new Vector2Int(x, y);
                switch (grid[cell])
                {
                    case MapCellType.Room: result.TotalRoomCells++; break;
                    case MapCellType.Corridor: result.TotalCorridorCells++; break;
                }
            }
        }

        result.CorridorToRoomRatio = result.TotalRoomCells > 0
            ? (float)result.TotalCorridorCells / result.TotalRoomCells
            : 0f;

        // ── 전 룸 도달 가능성 (Flood Fill) ────────────────────
        result.UnreachableRoomIndices = CheckRoomReachability(rooms, grid);
        result.AllRoomsReachable = result.UnreachableRoomIndices.Count == 0;

        if (!result.AllRoomsReachable)
            result.FailureReasons.Add(
                $"Unreachable rooms: [{string.Join(", ", result.UnreachableRoomIndices)}]");

        // ── 단계별 요약 ────────────────────────────────────────
        result.RoomCount = rooms?.Count ?? 0;
        result.MandatoryCorridorCount = mandatoryResult?.Corridors.Count ?? 0;
        result.OptionalCorridorCount = optionalResult?.Corridors.Count ?? 0;
        result.RemovedDeadEndCount = cleanupResult?.RemovedDeadEnds.Count ?? 0;
        result.KeptDeadEndCount = cleanupResult?.KeptDeadEnds.Count ?? 0;
        result.StartGoalGraphDistance = startGoalResult?.GraphDistance ?? 0;
        result.StartGoalStraightLineRatio = startGoalResult?.StraightLineRatio ?? 0f;

        // ── 필수 통로 실패 전파 ────────────────────────────────
        if (mandatoryResult != null && !mandatoryResult.Success)
        {
            foreach (var r in mandatoryResult.FailureReasons)
                result.FailureReasons.Add($"[MandatoryCorridor] {r}");
        }

        // ── StartGoal 미달 전파 ────────────────────────────────
        if (startGoalResult != null && !startGoalResult.Success)
            result.FailureReasons.Add($"[StartGoal] {startGoalResult.FailureReason}");

        // ── GenerationDiagnostics 기록 ─────────────────────────
        if (diagnostics != null)
        {
            diagnostics.RecordStat("RoomCount", result.RoomCount);
            diagnostics.RecordStat("MandatoryCorridors", result.MandatoryCorridorCount);
            diagnostics.RecordStat("OptionalCorridors", result.OptionalCorridorCount);
            diagnostics.RecordStat("RemovedDeadEnds", result.RemovedDeadEndCount);
            diagnostics.RecordStat("KeptDeadEnds", result.KeptDeadEndCount);
            diagnostics.RecordStat("TotalRoomCells", result.TotalRoomCells);
            diagnostics.RecordStat("TotalCorridorCells", result.TotalCorridorCells);
            diagnostics.RecordStat("CorridorToRoomRatio", result.CorridorToRoomRatio);
            diagnostics.RecordStat("AllRoomsReachable", result.AllRoomsReachable);
            diagnostics.RecordStat("StartGoalGraphDistance", result.StartGoalGraphDistance);
            diagnostics.RecordStat("StartGoalStraightRatio", result.StartGoalStraightLineRatio);

            if (result.FailureReasons.Count > 0)
                foreach (var r in result.FailureReasons)
                    diagnostics.RecordFailure(r);
        }

        result.Success = result.FailureReasons.Count == 0;
        return result;
    }

    // 각 룸의 내부 임의 셀에서 BFS — 룸 또는 코리도를 타고 다른 모든 룸에 도달 가능한지 확인
    private static List<int> CheckRoomReachability(List<RoomData> rooms, MapGrid grid)
    {
        if (rooms == null || rooms.Count == 0) return new List<int>();

        // 전체 walkable 셀 BFS (룸 + 코리도)
        var origin = rooms[0].Bounds.position;
        var reachable = FloodFill(origin, grid);

        var unreachable = new List<int>();
        for (int i = 1; i < rooms.Count; i++)
        {
            var sample = rooms[i].Bounds.position;
            if (!reachable.Contains(sample))
                unreachable.Add(i);
        }

        return unreachable;
    }

    private static HashSet<Vector2Int> FloodFill(Vector2Int start, MapGrid grid)
    {
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();

        // 시작점이 None이면 가장 가까운 walkable 셀로 이동
        if (grid[start] == MapCellType.None)
            return visited;

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in Dirs4)
            {
                var next = current + dir;
                if (!grid.InBounds(next)) continue;
                if (visited.Contains(next)) continue;
                if (grid[next] == MapCellType.None) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return visited;
    }
}