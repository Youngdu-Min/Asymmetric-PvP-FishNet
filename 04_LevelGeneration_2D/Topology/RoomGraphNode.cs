using UnityEngine;

public sealed class RoomGraphNode
{
    public int RoomIndex;
    public RoomData Room;
    public Vector2 Center;

    public RoomGraphNode(int roomIndex, RoomData room)
    {
        RoomIndex = roomIndex;
        Room = room;
        Center = (Vector2)room.Bounds.position + (Vector2)room.Bounds.size / 2f;
    }
}