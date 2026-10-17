using System.Collections.Generic;
using UnityEngine;

public static class RoomPlacer
{
    public static RoomPlacementResult PlaceRooms(RoomPlacementConfig config, IRandomSource random)
    {
        var result = new RoomPlacementResult();
        var rooms = new List<RoomData>();

        int attempts = 0;
        while (rooms.Count < config.TargetRoomCount && attempts < config.MaxPlacementAttempts)
        {
            attempts++;

            Vector2Int position = new Vector2Int(
                random.NextInt(0, config.MapSize.x),
                random.NextInt(0, config.MapSize.y)
            );

            Vector2Int roomSize = new Vector2Int(
                random.NextInt(config.RoomMinSize.x, config.RoomMaxSize.x + 1),
                random.NextInt(config.RoomMinSize.y, config.RoomMaxSize.y + 1)
            );

            var newRoom = new RoomData(position, roomSize);

            int b = config.RoomBuffer;
            var buffer = new RoomData(
                position + new Vector2Int(-b, -b),
                roomSize + new Vector2Int(b * 2, b * 2)
            );

            bool withinBounds =
                newRoom.Bounds.xMin >= 0 && newRoom.Bounds.xMax < config.MapSize.x &&
                newRoom.Bounds.yMin >= 0 && newRoom.Bounds.yMax < config.MapSize.y;

            if (!withinBounds)
                continue;

            bool overlaps = false;
            foreach (var existing in rooms)
            {
                if (RoomData.Intersect(existing, buffer))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
                continue;

            rooms.Add(newRoom);
        }

        result.Rooms = rooms;
        result.AttemptsUsed = attempts;
        result.Success = rooms.Count >= config.MinAcceptedRoomCount;

        if (!result.Success)
        {
            result.FailureReason =
                $"Placed {rooms.Count} rooms, required at least {config.MinAcceptedRoomCount} " +
                $"(used {attempts}/{config.MaxPlacementAttempts} attempts).";
        }

        return result;
    }
}