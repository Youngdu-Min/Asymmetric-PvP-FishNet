using System.Collections.Generic;
using UnityEngine;

public static class MandatoryCorridorGenerator
{
    public static MandatoryCorridorResult Generate(
        List<RoomData> rooms,
        RoomTopologyResult topology,
        MapGrid grid,
        MandatoryCorridorConfig config)
    {
        var result = new MandatoryCorridorResult();
        var pathfinder = new DungeonPathfinder2D(grid.Size);

        // 룸 내부를 먼저 그리드에 마킹 (비용 함수가 룸 내부/통로를 구분할 수 있도록)
        foreach (var room in rooms)
        {
            foreach (var pos in room.Bounds.allPositionsWithin)
                grid[pos] = MapCellType.Room;
        }

        foreach (var edge in topology.SelectedEdges)
        {
            var roomA = rooms[edge.A];
            var roomB = rooms[edge.B];
            var centerA = roomA.Bounds.position + roomA.Bounds.size / 2;
            var centerB = roomB.Bounds.position + roomB.Bounds.size / 2;

            var candidatesA = DoorCandidateFinder.FindCandidates(roomA, centerB, config.CorridorWidth, config.MinCornerMargin);
            var candidatesB = DoorCandidateFinder.FindCandidates(roomB, centerA, config.CorridorWidth, config.MinCornerMargin);

            if (candidatesA.Count == 0 || candidatesB.Count == 0)
            {
                result.FailureReasons.Add(
                    $"No valid door candidates between room {edge.A} and {edge.B} " +
                    $"(A={candidatesA.Count}, B={candidatesB.Count}).");
                continue;
            }

            Vector2Int start = candidatesA[0].RoomCellPosition;
            Vector2Int end = candidatesB[0].RoomCellPosition;

            var path = pathfinder.FindPath(start, end, (a, b) => EvaluateCost(b, end, grid, config));

            if (path == null)
            {
                result.FailureReasons.Add($"No path found between room {edge.A} and {edge.B}.");
                continue;
            }

            var expanded = CorridorWidthExpander.ExpandCenterline(path, config.CorridorWidth, grid.Size);

            foreach (var cell in expanded)
            {
                if (grid[cell] == MapCellType.None)
                    grid[cell] = MapCellType.Corridor;
            }

            result.Corridors.Add(new CorridorData
            {
                RoomA = edge.A,
                RoomB = edge.B,
                Centerline = path,
                ExpandedCells = expanded
            });
        }

        result.Success = result.FailureReasons.Count == 0;
        return result;
    }

    private static DungeonPathfinder2D.PathCost EvaluateCost(
        Vector2Int next, Vector2Int end, MapGrid grid, MandatoryCorridorConfig config)
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
            case MapCellType.None:
            default:
                cost.Cost = heuristic + config.EmptyCellCost;
                break;
        }

        return cost;
    }
}