using System.Collections.Generic;
using UnityEngine;

public class DoorCandidateSpikeRunner : MonoBehaviour
{
    public enum FixtureCase
    {
        StraightAligned,
        SlightlyOffset,
        DiagonalCorner,
        NearBoundary,
        NarrowRoom,
        DiagonalSymmetric,
        ThreeRoomsObstacle,
        OppositeCorners
    }

    [SerializeField] private FixtureCase fixtureCase;
    [SerializeField] private Vector2Int mapSize = new Vector2Int(40, 30);
    [SerializeField] private int corridorWidth = 3;
    [SerializeField] private int minCornerMargin = 1;

    private List<RoomData> _rooms;
    private HashSet<Vector2Int> _expandedCorridor;
    private List<DoorCandidate> _candidatesA;
    private List<DoorCandidate> _candidatesB;
    private List<Vector2Int> _centerline;
    private bool _connected;
    private bool _overlapsRoom;
    private bool _withinBounds;
    private bool _hasValidCandidates;

    void Start()
    {
        RunSpike();
    }

    [ContextMenu("Run Spike")]
    void RunSpike()
    {
        _rooms = fixtureCase switch
        {
            FixtureCase.StraightAligned => TestRoomFixtures.StraightAligned(),
            FixtureCase.SlightlyOffset => TestRoomFixtures.SlightlyOffset(),
            FixtureCase.DiagonalCorner => TestRoomFixtures.DiagonalCorner(),
            FixtureCase.NearBoundary => TestRoomFixtures.NearBoundary(mapSize),
            FixtureCase.NarrowRoom => TestRoomFixtures.NarrowRoom(),
            FixtureCase.DiagonalSymmetric => TestRoomFixtures.DiagonalSymmetric(),
            FixtureCase.ThreeRoomsObstacle => TestRoomFixtures.ThreeRoomsObstacle(),
            FixtureCase.OppositeCorners => TestRoomFixtures.OppositeCorners(mapSize),
            _ => TestRoomFixtures.StraightAligned()
        };

        // ThreeRoomsObstacle은 A(0)-B(1) 연결, C(2)는 장애물 역할로 충돌 검사에만 사용
        var roomA = _rooms[0];
        var roomB = _rooms[1];
        var centerA = roomA.Bounds.position + roomA.Bounds.size / 2;
        var centerB = roomB.Bounds.position + roomB.Bounds.size / 2;

        _candidatesA = DoorCandidateFinder.FindCandidates(roomA, centerB, corridorWidth, minCornerMargin);
        _candidatesB = DoorCandidateFinder.FindCandidates(roomB, centerA, corridorWidth, minCornerMargin);

        _hasValidCandidates = SpikeValidator.HasValidDoorCandidates(_candidatesA, _candidatesB);

        Vector2Int start = _candidatesA.Count > 0 ? _candidatesA[0].RoomCellPosition : centerA;
        Vector2Int end = _candidatesB.Count > 0 ? _candidatesB[0].RoomCellPosition : centerB;

        _centerline = BuildLShapedCenterline(start, end);
        _expandedCorridor = CorridorWidthExpander.ExpandCenterline(_centerline, corridorWidth, mapSize);

        _connected = SpikeValidator.IsFullyConnected(_expandedCorridor);
        _overlapsRoom = SpikeValidator.HasOverlapWithRooms(_expandedCorridor, _rooms, roomA, roomB);
        _withinBounds = SpikeValidator.IsWithinMapBounds(_expandedCorridor, mapSize);

        bool pass = _connected && !_overlapsRoom && _withinBounds && _hasValidCandidates;

        Debug.Log($"[Spike:{fixtureCase}] PASS={pass} Connected={_connected} OverlapsRoom={_overlapsRoom} " +
                  $"WithinBounds={_withinBounds} HasValidCandidates={_hasValidCandidates} " +
                  $"DoorCandidatesA={_candidatesA.Count} DoorCandidatesB={_candidatesB.Count}");
    }

    private List<Vector2Int> BuildLShapedCenterline(Vector2Int start, Vector2Int end)
    {
        var path = new List<Vector2Int>();
        Vector2Int current = start;
        path.Add(current);

        while (current.x != end.x)
        {
            current.x += current.x < end.x ? 1 : -1;
            path.Add(current);
        }
        while (current.y != end.y)
        {
            current.y += current.y < end.y ? 1 : -1;
            path.Add(current);
        }

        return path;
    }

    void OnDrawGizmos()
    {
        if (_rooms == null) return;

        Gizmos.color = Color.red;
        foreach (var room in _rooms)
        {
            Vector3 center = new Vector3(room.Bounds.center.x, room.Bounds.center.y, 0);
            Vector3 size = new Vector3(room.Bounds.size.x, room.Bounds.size.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }

        if (_expandedCorridor != null)
        {
            Gizmos.color = (_connected && !_overlapsRoom && _withinBounds && _hasValidCandidates) ? Color.cyan : Color.magenta;
            foreach (var cell in _expandedCorridor)
            {
                Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), new Vector3(0.9f, 0.9f, 0.05f));
            }
        }

        Gizmos.color = Color.yellow;
        DrawCandidates(_candidatesA);
        DrawCandidates(_candidatesB);
    }

    private void DrawCandidates(List<DoorCandidate> candidates)
    {
        if (candidates == null) return;
        foreach (var c in candidates)
        {
            Vector3 pos = new Vector3(c.RoomCellPosition.x + 0.5f, c.RoomCellPosition.y + 0.5f, -0.1f);
            Gizmos.DrawSphere(pos, 0.15f);
        }
    }
}