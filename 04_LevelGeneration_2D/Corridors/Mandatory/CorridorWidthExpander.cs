using System.Collections.Generic;
using UnityEngine;

public static class CorridorWidthExpander
{
    public static HashSet<Vector2Int> ExpandCenterline(List<Vector2Int> centerline, int width, Vector2Int mapSize)
    {
        var expanded = new HashSet<Vector2Int>();
        int half = width / 2;

        for (int i = 0; i < centerline.Count; i++)
        {
            Vector2Int current = centerline[i];
            Vector2Int direction = Vector2Int.zero;

            if (i < centerline.Count - 1)
                direction = centerline[i + 1] - current;
            else if (i > 0)
                direction = current - centerline[i - 1];

            Vector2Int perpendicular = new Vector2Int(-direction.y, direction.x);
            perpendicular = ClampToAxis(perpendicular);

            for (int o = -half; o < width - half; o++)
            {
                Vector2Int cell = current + perpendicular * o;
                if (IsInBounds(cell, mapSize))
                    expanded.Add(cell);
            }

            expanded.Add(current);
        }

        return expanded;
    }

    private static Vector2Int ClampToAxis(Vector2Int v)
    {
        if (v.x != 0 && v.y != 0)
            return new Vector2Int(0, v.y != 0 ? v.y : 1);
        if (v == Vector2Int.zero)
            return Vector2Int.right;
        return v;
    }

    private static bool IsInBounds(Vector2Int cell, Vector2Int mapSize)
    {
        return cell.x >= 0 && cell.x < mapSize.x && cell.y >= 0 && cell.y < mapSize.y;
    }
}