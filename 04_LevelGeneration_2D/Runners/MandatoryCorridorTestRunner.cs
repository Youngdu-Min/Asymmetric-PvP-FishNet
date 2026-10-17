using System.Collections.Generic;
using UnityEngine;

public class MandatoryCorridorTestRunner : MonoBehaviour
{
    [Header("── Seed ──────────────────────")]
    [Tooltip("맵 생성 기본 시드.")]
    [SerializeField] private int baseSeed = 0;

    [Header("── Map ───────────────────────")]
    [Tooltip("맵 전체 크기 (타일 단위)")]
    [SerializeField] private Vector2Int mapSize = new Vector2Int(40, 30);

    [Header("── Room ──────────────────────")]
    [Tooltip("목표 룸 생성 개수")]
    [Range(1, 30)]
    [SerializeField] private int targetRoomCount = 10;

    [Tooltip("룸 최소 크기")]
    [SerializeField] private Vector2Int roomMinSize = new Vector2Int(5, 5);

    [Tooltip("룸 최대 크기")]
    [SerializeField] private Vector2Int roomMaxSize = new Vector2Int(8, 8);

    [Tooltip("룸 배치 최대 시도 횟수")]
    [Range(50, 500)]
    [SerializeField] private int maxPlacementAttempts = 200;

    [Tooltip("파이프라인 진행에 필요한 최소 룸 개수")]
    [Range(1, 20)]
    [SerializeField] private int minAcceptedRoomCount = 6;

    [Header("── Topology ──────────────────")]
    [Tooltip("MST 외 추가 엣지 생성 확률 (0=없음, 1=전부)")]
    [Range(0f, 1f)]
    [SerializeField] private float extraEdgeChance = 0.125f;

    [Header("── Corridor ──────────────────")]
    [Tooltip("통로 폭 (타일 단위)")]
    [Range(1, 6)]
    [SerializeField] private int corridorWidth = 3;

    [Tooltip("통로 끝점이 룸 코너에서 최소 떨어져야 할 거리")]
    [Range(0, 4)]
    [SerializeField] private int minCornerMargin = 1;

    public MandatoryCorridorResult CorridorResult => _corridorResult;
    public RoomTopologyResult Topology => _topologyResult;
    public Vector2Int MapSizeValue => mapSize;
    public MapSeedContext SeedContext => _seedContext;
    public List<RoomData> Rooms => _roomResult?.Rooms;
    public MapGrid Grid => _grid;

    private MapSeedContext _seedContext;
    private RoomPlacementResult _roomResult;
    private RoomTopologyResult _topologyResult;
    private MandatoryCorridorResult _corridorResult;
    private MapGrid _grid;

    public void RunPipeline(int seed)
    {
        _seedContext = new MapSeedContext(seed, "0.1", "test");

        // 1단계
        var roomRandom = _seedContext.GetSource(MapGenerationPass.RoomPlacement);

        // 2단계
        var roomConfig = new RoomPlacementConfig
        {
            MapSize = mapSize,
            RoomMinSize = roomMinSize,
            RoomMaxSize = roomMaxSize,
            TargetRoomCount = targetRoomCount,
            MaxPlacementAttempts = maxPlacementAttempts,
            MinAcceptedRoomCount = minAcceptedRoomCount,
            RoomBuffer = 1
        };
        _roomResult = RoomPlacer.PlaceRooms(roomConfig, roomRandom);

        if (!_roomResult.Success)
        {
            Debug.LogWarning($"[Pipeline] Room placement failed: {_roomResult.FailureReason}");
            return;
        }

        // 3단계
        var topologyRandom = _seedContext.GetSource(MapGenerationPass.Topology);
        var topologyConfig = new RoomTopologyConfig { ExtraEdgeChance = extraEdgeChance };
        _topologyResult = RoomTopologyBuilder.Build(_roomResult.Rooms, topologyConfig, topologyRandom);

        if (!_topologyResult.Success)
        {
            Debug.LogWarning($"[Pipeline] Topology failed: {_topologyResult.FailureReason}");
            return;
        }

        // 4단계
        _grid = new MapGrid(mapSize);
        var corridorConfig = new MandatoryCorridorConfig
        {
            CorridorWidth = corridorWidth,
            MinCornerMargin = minCornerMargin
        };
        _corridorResult = MandatoryCorridorGenerator.Generate(_roomResult.Rooms, _topologyResult, _grid, corridorConfig);

        Debug.Log($"[Pipeline] Seed={seed} Rooms={_roomResult.Rooms.Count} " +
                  $"TopologyEdges={_topologyResult.SelectedEdges.Count} " +
                  $"Corridors={_corridorResult.Corridors.Count} " +
                  $"CorridorSuccess={_corridorResult.Success}");

        if (!_corridorResult.Success)
        {
            foreach (var reason in _corridorResult.FailureReasons)
                Debug.LogWarning($"[Pipeline] Corridor failure: {reason}");
        }
    }

    [ContextMenu("Run Pipeline (1~4)")]
    public void RunPipelineInEditor()
    {
        RunPipeline(baseSeed);
    }

    void OnDrawGizmos()
    {
        if (_roomResult == null || !_roomResult.Success) return;

        // 맵 경계
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(new Vector3(mapSize.x / 2f, mapSize.y / 2f, 0), new Vector3(mapSize.x, mapSize.y, 0.1f));

        // 룸
        Gizmos.color = Color.red;
        for (int i = 0; i < _roomResult.Rooms.Count; i++)
        {
            var room = _roomResult.Rooms[i];
            Vector3 center = new Vector3(room.Bounds.center.x, room.Bounds.center.y, 0);
            Vector3 size = new Vector3(room.Bounds.size.x, room.Bounds.size.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }

        // 토폴로지 그래프
        if (_topologyResult != null && _topologyResult.Success)
        {
            var mstSet = new HashSet<RoomGraphEdge>(_topologyResult.MstEdges);

            foreach (var edge in _topologyResult.SelectedEdges)
            {
                Gizmos.color = mstSet.Contains(edge) ? Color.white : Color.yellow;
                var a = _topologyResult.Nodes[edge.A].Center;
                var b = _topologyResult.Nodes[edge.B].Center;
                Gizmos.DrawLine(new Vector3(a.x, a.y, -0.05f), new Vector3(b.x, b.y, -0.05f));
            }
        }

        // 통로
        if (_corridorResult != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var corridor in _corridorResult.Corridors)
            {
                foreach (var cell in corridor.ExpandedCells)
                {
                    Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), new Vector3(0.9f, 0.9f, 0.05f));
                }
            }

            Gizmos.color = Color.green;
            foreach (var corridor in _corridorResult.Corridors)
            {
                if (corridor.Centerline.Count == 0) continue;
                var start = corridor.Centerline[0];
                var end = corridor.Centerline[corridor.Centerline.Count - 1];
                Gizmos.DrawSphere(new Vector3(start.x + 0.5f, start.y + 0.5f, -0.1f), 0.2f);
                Gizmos.DrawSphere(new Vector3(end.x + 0.5f, end.y + 0.5f, -0.1f), 0.2f);
            }
        }
    }
}