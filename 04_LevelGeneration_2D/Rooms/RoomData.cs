using UnityEngine;

public sealed class RoomData
{
    public RectInt Bounds;

    public RoomData(Vector2Int position, Vector2Int size)
    {
        Bounds = new RectInt(position, size);
    }

    public static bool Intersect(RoomData a, RoomData b)
    {
        return !((a.Bounds.position.x >= b.Bounds.position.x + b.Bounds.size.x)
            || (a.Bounds.position.x + a.Bounds.size.x <= b.Bounds.position.x)
            || (a.Bounds.position.y >= b.Bounds.position.y + b.Bounds.size.y)
            || (a.Bounds.position.y + a.Bounds.size.y <= b.Bounds.position.y));
    }
}