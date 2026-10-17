
using System.Collections.Generic;
using UnityEngine;

public static class DoorCandidateFinder
{
    private static readonly Vector2Int[] Directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static List<DoorCandidate> FindCandidates(
        RoomData room,
        Vector2Int targetCenter,
        int corridorWidth,
        int minCornerMargin)
    {
        var candidates = new List<DoorCandidate>();
        var bounds = room.Bounds;

        foreach (var dir in Directions)
        {
            bool facingTarget = Vector2.Dot(dir, (targetCenter - bounds.position - bounds.size / 2)) > 0;
            if (!facingTarget) continue;

            List<Vector2Int> wallCells = GetWallCells(bounds, dir);

            for (int i = 0; i <= wallCells.Count - corridorWidth; i++)
            {
                bool segmentValid = true;
                for (int w = 0; w < corridorWidth; w++)
                {
                    if (!IsFarEnoughFromCorner(wallCells[i + w], bounds, minCornerMargin))
                    {
                        segmentValid = false;
                        break;
                    }
                }

                if (segmentValid)
                {
                    candidates.Add(new DoorCandidate
                    {
                        RoomCellPosition = wallCells[i + corridorWidth / 2],
                        OutwardDirection = dir,
                        Clearance = corridorWidth
                    });
                }
            }
        }

        return candidates;
    }

    private static List<Vector2Int> GetWallCells(RectInt bounds, Vector2Int dir)
    {
        var cells = new List<Vector2Int>();

        if (dir == Vector2Int.up || dir == Vector2Int.down)
        {
            int y = dir == Vector2Int.up ? bounds.yMax - 1 : bounds.yMin;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
                cells.Add(new Vector2Int(x, y));
        }
        else
        {
            int x = dir == Vector2Int.right ? bounds.xMax - 1 : bounds.xMin;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                cells.Add(new Vector2Int(x, y));
        }

        return cells;
    }

    private static bool IsFarEnoughFromCorner(Vector2Int cell, RectInt bounds, int minMargin)
    {
        int distFromMinX = cell.x - bounds.xMin;
        int distFromMaxX = bounds.xMax - 1 - cell.x;
        int distFromMinY = cell.y - bounds.yMin;
        int distFromMaxY = bounds.yMax - 1 - cell.y;

        bool isHorizontalWall = distFromMinY == 0 || distFromMaxY == 0;
        int alongWallMargin = isHorizontalWall
            ? Mathf.Min(distFromMinX, distFromMaxX)
            : Mathf.Min(distFromMinY, distFromMaxY);

        return alongWallMargin >= minMargin;
    }
}