using System.Collections.Generic;
using UnityEngine;

public static class OptionalCorridorGenerator
{
    public static OptionalCorridorResult Generate(
        List<RoomData> rooms,
        MapGrid grid,
        OptionalCorridorConfig config,
        IRandomSource random,
        Dictionary<Vector2Int, int> cellOwnerCorridorId,
        Dictionary<int, OptionalCorridorData> corridorById)
    {
        var result = new OptionalCorridorResult();
        var pathfinder = new DungeonPathfinder2D(grid.Size);

        foreach (CorridorTerminationType t in System.Enum.GetValues(typeof(CorridorTerminationType)))
            result.TypeCounts[t] = 0;

        for (int attempt = 0; attempt < config.AttemptCount; attempt++)
        {
            var startCell = PickRandomCorridorCell(grid, random);
            if (startCell == null)
            {
                result.RejectedAttempts++;
                continue;
            }

            var type = RollTerminationType(config, random);
            OptionalCorridorData corridor = type switch
            {
                CorridorTerminationType.RoomConnect => TryRoomConnect(startCell.Value, rooms, grid, pathfinder, config),
                CorridorTerminationType.CorridorMerge => TryCorridorMerge(startCell.Value, grid, pathfinder, config, random),
                CorridorTerminationType.Loop => TryLoop(startCell.Value, grid, pathfinder, config, random),
                CorridorTerminationType.DeadEnd => TryDeadEnd(startCell.Value, grid, config, random),
                CorridorTerminationType.ExpandedDeadEnd => TryExpandedDeadEnd(startCell.Value, grid, config, random),
                _ => null
            };

            if (corridor == null)
            {
                result.RejectedAttempts++;
                continue;
            }

            int corridorId = result.Corridors.Count; // 간단한 순번 ID
            corridorById[corridorId] = corridor;
            StampCorridor(corridor, grid, corridorId, cellOwnerCorridorId); // 시그니처 변경

            result.Corridors.Add(corridor);
            result.TypeCounts[corridor.Type]++;
        }

        return result;
    }

    private static CorridorTerminationType RollTerminationType(OptionalCorridorConfig config, IRandomSource random)
    {
        float total = config.RoomConnectWeight + config.CorridorMergeWeight + config.LoopWeight
                    + config.DeadEndWeight + config.ExpandedDeadEndWeight;
        float roll = random.NextFloat(0f, total);

        if ((roll -= config.RoomConnectWeight) < 0) return CorridorTerminationType.RoomConnect;
        if ((roll -= config.CorridorMergeWeight) < 0) return CorridorTerminationType.CorridorMerge;
        if ((roll -= config.LoopWeight) < 0) return CorridorTerminationType.Loop;
        if ((roll -= config.DeadEndWeight) < 0) return CorridorTerminationType.DeadEnd;
        return CorridorTerminationType.ExpandedDeadEnd;
    }

    private static Vector2Int? PickRandomCorridorCell(MapGrid grid, IRandomSource random)
    {
        var candidates = new List<Vector2Int>();
        for (int x = 0; x < grid.Size.x; x++)
            for (int y = 0; y < grid.Size.y; y++)
                if (grid[new Vector2Int(x, y)] == MapCellType.Corridor)
                    candidates.Add(new Vector2Int(x, y));

        if (candidates.Count == 0) return null;
        return candidates[random.NextInt(0, candidates.Count)];
    }

    private static OptionalCorridorData TryRoomConnect(
     Vector2Int start, List<RoomData> rooms, MapGrid grid, DungeonPathfinder2D pathfinder, OptionalCorridorConfig config)
    {
        RoomData closest = null;
        float bestDist = float.MaxValue;
        Vector2Int target = default;

        // start 주변 1셀 범위와 겹치는 룸 = 이미 인접/연결된 룸으로 간주, 후보 제외
        var adjacentRect = new RectInt(start.x - 1, start.y - 1, 3, 3);

        foreach (var room in rooms)
        {
            if (room.Bounds.Overlaps(adjacentRect)) continue;

            var candidates = DoorCandidateFinder.FindCandidates(room, start, config.CorridorWidth, config.MinCornerMargin);
            if (candidates.Count == 0) continue;

            float dist = Vector2Int.Distance(start, candidates[0].RoomCellPosition);
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = room;
                target = candidates[0].RoomCellPosition;
            }
        }

        if (closest == null) return null;

        var path = pathfinder.FindPath(start, target, (a, b) => EvaluateCost(b, target, grid, config));
        if (path == null) return null;

        return BuildResult(CorridorTerminationType.RoomConnect, path, grid, config);
    }

    // 통로 합류: 다른 임의 통로 셀로 A*
    private static OptionalCorridorData TryCorridorMerge(
        Vector2Int start, MapGrid grid, DungeonPathfinder2D pathfinder, OptionalCorridorConfig config, IRandomSource random)
    {
        var targetCell = PickRandomCorridorCell(grid, random);
        if (targetCell == null || targetCell.Value == start) return null;

        var path = pathfinder.FindPath(start, targetCell.Value, (a, b) => EvaluateCost(b, targetCell.Value, grid, config));
        if (path == null) return null;

        return BuildResult(CorridorTerminationType.CorridorMerge, path, grid, config);
    }

    // 순환: 다른 통로 셀로 연결하되 최소 경로 길이를 강제해 짧은 순환 방지 (기획서 5.4)
    private static OptionalCorridorData TryLoop(
        Vector2Int start, MapGrid grid, DungeonPathfinder2D pathfinder, OptionalCorridorConfig config, IRandomSource random)
    {
        for (int i = 0; i < 5; i++)
        {
            var targetCell = PickRandomCorridorCell(grid, random);
            if (targetCell == null || targetCell.Value == start) continue;

            float straightDist = Vector2Int.Distance(start, targetCell.Value);
            if (straightDist < config.MinLoopPathLength) continue;

            var path = pathfinder.FindPath(start, targetCell.Value, (a, b) => EvaluateCost(b, targetCell.Value, grid, config));
            if (path == null || path.Count < config.MinLoopPathLength) continue;

            return BuildResult(CorridorTerminationType.Loop, path, grid, config);
        }
        return null;
    }

    // 막다른 길: 랜덤 워크로 직선 위주 성장, 길이 범위 강제
    private static OptionalCorridorData TryDeadEnd(
        Vector2Int start, MapGrid grid, OptionalCorridorConfig config, IRandomSource random)
    {
        int targetLength = random.NextInt(config.MinDeadEndLength, config.MaxDeadEndLength + 1);
        var path = RandomWalk(start, targetLength, grid, random);

        if (path == null || path.Count < config.MinDeadEndLength) return null;

        return BuildResult(CorridorTerminationType.DeadEnd, path, grid, config);
    }

    // 확장형 막다른 공간: 막다른 길 + 끝에 작은 빈 공간 (룸으로 분류하지 않음, 기획서 5.6)
    private static OptionalCorridorData TryExpandedDeadEnd(
        Vector2Int start, MapGrid grid, OptionalCorridorConfig config, IRandomSource random)
    {
        int targetLength = random.NextInt(config.MinDeadEndLength, config.MaxDeadEndLength + 1);
        var path = RandomWalk(start, targetLength, grid, random);

        if (path == null || path.Count < config.MinDeadEndLength) return null;

        var corridor = BuildResult(CorridorTerminationType.ExpandedDeadEnd, path, grid, config);

        var tip = path[path.Count - 1];
        int r = config.ExpandedDeadEndRadius;
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                var cell = tip + new Vector2Int(dx, dy);
                if (grid.InBounds(cell) && grid[cell] == MapCellType.None)
                    corridor.ExpandedCells.Add(cell);
            }
        }

        return corridor;
    }

    private static List<Vector2Int> RandomWalk(Vector2Int start, int targetLength, MapGrid grid, IRandomSource random)
    {
        var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var path = new List<Vector2Int> { start };
        var current = start;
        Vector2Int lastDir = directions[random.NextInt(0, 4)];

        for (int i = 0; i < targetLength; i++)
        {
            // 직선 위주로 가되 가끔 꺾이도록(최대 3회 꺾임 가이드라인은 4.3절, 여기서는 단순 확률로 근사)
            Vector2Int dir = random.Chance(0.75f) ? lastDir : directions[random.NextInt(0, 4)];
            var next = current + dir;

            if (!grid.InBounds(next) || grid[next] != MapCellType.None)
            {
                dir = directions[random.NextInt(0, 4)];
                next = current + dir;
                if (!grid.InBounds(next) || grid[next] != MapCellType.None)
                    break;
            }

            current = next;
            lastDir = dir;
            path.Add(current);
        }

        return path;
    }

    private static OptionalCorridorData BuildResult(
        CorridorTerminationType type, List<Vector2Int> path, MapGrid grid, OptionalCorridorConfig config)
    {
        var expanded = CorridorWidthExpander.ExpandCenterline(path, config.CorridorWidth, grid.Size);
        return new OptionalCorridorData { Type = type, Centerline = path, ExpandedCells = expanded };
    }

    private static void StampCorridor(
       OptionalCorridorData corridor, MapGrid grid, int corridorId, Dictionary<Vector2Int, int> cellOwnerCorridorId)
    {
        foreach (var cell in corridor.ExpandedCells)
        {
            if (grid[cell] == MapCellType.None)
            {
                grid[cell] = MapCellType.Corridor;
                cellOwnerCorridorId[cell] = corridorId;
            }
        }
    }

    private static DungeonPathfinder2D.PathCost EvaluateCost(
        Vector2Int next, Vector2Int end, MapGrid grid, OptionalCorridorConfig config)
    {
        var cost = new DungeonPathfinder2D.PathCost { Traversable = true };
        float heuristic = Vector2Int.Distance(next, end);

        switch (grid[next])
        {
            case MapCellType.Room:
                cost.Cost = heuristic + config.RoomInteriorCost;
                break;
            case MapCellType.Corridor:
                cost.Cost = heuristic + config.ExistingCorridorCost;
                break;
            default:
                cost.Cost = heuristic + config.EmptyCellCost;
                break;
        }

        return cost;
    }
}