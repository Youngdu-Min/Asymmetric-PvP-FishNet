using System.Collections.Generic;
using UnityEngine;

public static class TestRoomFixtures
{
    public static List<RoomData> StraightAligned()
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(6, 6)),
            new RoomData(new Vector2Int(12, 0), new Vector2Int(6, 6)),
        };
    }

    public static List<RoomData> SlightlyOffset()
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(6, 6)),
            new RoomData(new Vector2Int(14, 3), new Vector2Int(6, 8)),
        };
    }

    public static List<RoomData> DiagonalCorner()
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(5, 5)),
            new RoomData(new Vector2Int(16, 12), new Vector2Int(7, 5)),
        };
    }

    public static List<RoomData> NearBoundary(Vector2Int mapSize)
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(5, 5)),
            new RoomData(new Vector2Int(mapSize.x - 8, mapSize.y - 6), new Vector2Int(6, 5)),
        };
    }

    // 케이스 2: 룸 너비(3) < corridorWidth(4) 후보 — 후보 0개로 떨어지는지 검증
    public static List<RoomData> NarrowRoom()
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(3, 8)),
            new RoomData(new Vector2Int(14, 0), new Vector2Int(3, 8)),
        };
    }

    // 케이스 3: 정확히 45도 대각선, 거리 동일 — facing 필터 경계값 검증
    public static List<RoomData> DiagonalSymmetric()
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(5, 5)),
            new RoomData(new Vector2Int(15, 15), new Vector2Int(5, 5)),
        };
    }

    // 케이스 4: A-B 직선 경로 옆에 C룸을 최소 간격(2타일)으로 근접 배치
    public static List<RoomData> ThreeRoomsObstacle()
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(5, 5)),
            new RoomData(new Vector2Int(20, 0), new Vector2Int(5, 5)),
            new RoomData(new Vector2Int(9, -1), new Vector2Int(6, 7)), // A-B 사이, 간격 2타일로 근접
        };
    }

    // 케이스 5: 맵 양쪽 정반대 모서리
    public static List<RoomData> OppositeCorners(Vector2Int mapSize)
    {
        return new List<RoomData> {
            new RoomData(new Vector2Int(0, 0), new Vector2Int(5, 5)),
            new RoomData(new Vector2Int(mapSize.x - 5, mapSize.y - 5), new Vector2Int(5, 5)),
        };
    }
}