using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DeadEndCleanup
{
    public static DeadEndCleanupResult Run(
        List<OptionalCorridorData> optionalCorridors,
        Dictionary<Vector2Int, int> cellOwnerCorridorId, // 셀 -> 소유 통로 ID (Stamp 시 기록 필요)
        Dictionary<int, OptionalCorridorData> corridorById,
        MapGrid grid,
        DeadEndCleanupConfig config)
    {
        var result = new DeadEndCleanupResult();

        var deadEnds = optionalCorridors
            .Where(c => c.Type == CorridorTerminationType.DeadEnd || c.Type == CorridorTerminationType.ExpandedDeadEnd)
            .ToList();

        int totalCorridorArea = cellOwnerCorridorId.Count;
        if (totalCorridorArea == 0) totalCorridorArea = 1; // 0 나눗셈 방지

        var survivors = new List<OptionalCorridorData>(deadEnds);

        // 1차: 길이 범위 위반 제거
        foreach (var deadEnd in deadEnds)
        {
            int length = deadEnd.Centerline.Count;
            if (length < config.MinDeadEndLength)
            {
                Remove(deadEnd, survivors, result, $"Length {length} below MinDeadEndLength {config.MinDeadEndLength}");
            }
            else if (length > config.MaxDeadEndLength)
            {
                Remove(deadEnd, survivors, result, $"Length {length} exceeds MaxDeadEndLength {config.MaxDeadEndLength}");
            }
        }

        // 2차: 연속 분기 깊이 위반 제거 (다른 막다른 길의 시작점에서 갈라진 막다른 길)
        foreach (var deadEnd in deadEnds)
        {
            if (!survivors.Contains(deadEnd)) continue;

            int depth = ComputeBranchDepth(deadEnd, deadEnds, cellOwnerCorridorId, corridorById);
            if (depth > config.MaxConsecutiveDeadEndDepth)
            {
                Remove(deadEnd, survivors, result, $"Consecutive dead-end depth {depth} exceeds max {config.MaxConsecutiveDeadEndDepth}");
            }
        }

        // 3차: 전체 면적 비율 초과 시, 가장 면적이 큰 것부터 제거
        var sortedByArea = survivors.OrderByDescending(d => d.ExpandedCells.Count).ToList();
        int currentDeadEndArea = survivors.Sum(d => d.ExpandedCells.Count);

        int index = 0;
        while ((float)currentDeadEndArea / totalCorridorArea > config.MaxDeadEndAreaRatio && index < sortedByArea.Count)
        {
            var candidate = sortedByArea[index];
            if (survivors.Contains(candidate))
            {
                currentDeadEndArea -= candidate.ExpandedCells.Count;
                Remove(candidate, survivors, result, $"Total dead-end area ratio exceeded {config.MaxDeadEndAreaRatio:P0}");
            }
            index++;
        }

        // 실제 그리드에서 제거 (다른 통로/룸과 공유되지 않는 셀만 클리어)
        foreach (var removed in result.RemovedDeadEnds)
        {
            UnstampExclusiveCells(removed, cellOwnerCorridorId, corridorById, grid);
        }

        result.KeptDeadEnds = survivors;
        result.FinalDeadEndAreaRatio = (float)currentDeadEndArea / totalCorridorArea;

        return result;
    }

    private static void Remove(
        OptionalCorridorData target, List<OptionalCorridorData> survivors,
        DeadEndCleanupResult result, string reason)
    {
        if (!survivors.Remove(target)) return;
        result.RemovedDeadEnds.Add(target);
        result.RemovalReasons.Add(reason);
    }

    // 시작점이 다른 막다른 길의 영역 안에 있으면 그 위에서 분기한 것으로 간주, 재귀적으로 깊이 계산
    private static int ComputeBranchDepth(
        OptionalCorridorData deadEnd, List<OptionalCorridorData> allDeadEnds,
        Dictionary<Vector2Int, int> cellOwnerCorridorId, Dictionary<int, OptionalCorridorData> corridorById,
        int currentDepth = 1, HashSet<OptionalCorridorData> visited = null)
    {
        visited ??= new HashSet<OptionalCorridorData>();
        if (!visited.Add(deadEnd)) return currentDepth; // 순환 방지

        var start = deadEnd.Centerline[0];

        if (!cellOwnerCorridorId.TryGetValue(start, out int ownerId))
            return currentDepth; // 룸이나 필수 통로에서 시작 = 1단계

        if (!corridorById.TryGetValue(ownerId, out var owner))
            return currentDepth;

        bool ownerIsDeadEnd = owner.Type == CorridorTerminationType.DeadEnd || owner.Type == CorridorTerminationType.ExpandedDeadEnd;
        if (!ownerIsDeadEnd || owner == deadEnd)
            return currentDepth; // 필수 통로/추가 통로(막다른 길 아님) 위에서 분기 = 1단계로 처리

        return ComputeBranchDepth(owner, allDeadEnds, cellOwnerCorridorId, corridorById, currentDepth + 1, visited);
    }

    private static void UnstampExclusiveCells(
        OptionalCorridorData removed, Dictionary<Vector2Int, int> cellOwnerCorridorId,
        Dictionary<int, OptionalCorridorData> corridorById, MapGrid grid)
    {
        foreach (var cell in removed.ExpandedCells)
        {
            // 이 셀의 소유 통로 ID가 정확히 이 dead-end 하나뿐일 때만 비움
            // (다른 통로와 겹쳐 stamp된 셀은 보존)
            if (cellOwnerCorridorId.TryGetValue(cell, out int ownerId) && corridorById[ownerId] == removed)
            {
                grid[cell] = MapCellType.None;
                cellOwnerCorridorId.Remove(cell);
            }
        }
    }
}