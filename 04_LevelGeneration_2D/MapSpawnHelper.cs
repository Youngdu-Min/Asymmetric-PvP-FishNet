using System.Collections.Generic;
using UnityEngine;

public static class MapSpawnHelper
{
    // 벽(None 셀)에서 minWallDistance 이상 떨어진 floor 셀만 반환
    public static List<Vector2Int> GetFloorCellsAwayFromWalls(MapGrid grid, int minWallDistance = 1)
    {
        var result = new List<Vector2Int>();

        for (int x = 0; x < grid.Size.x; x++)
        {
            for (int y = 0; y < grid.Size.y; y++)
            {
                var pos = new Vector2Int(x, y);
                if (grid[pos] != MapCellType.Room && grid[pos] != MapCellType.Corridor) continue;
                if (IsAwayFromWalls(pos, grid, minWallDistance))
                    result.Add(pos);
            }
        }

        return result;
    }

    public static bool IsFarEnoughFromAll(Vector3 candidate, List<Vector3> occupied, float minRadius)
    {
        foreach (var pos in occupied)
            if (Vector3.Distance(candidate, pos) < minRadius)
                return false;
        return true;
    }

    private static bool IsAwayFromWalls(Vector2Int pos, MapGrid grid, int minDist)
    {
        for (int dx = -minDist; dx <= minDist; dx++)
            for (int dy = -minDist; dy <= minDist; dy++)
            {
                var neighbor = new Vector2Int(pos.x + dx, pos.y + dy);
                if (!grid.InBounds(neighbor)) return false;
                if (grid[neighbor] == MapCellType.None) return false;
            }
        return true;
    }
}