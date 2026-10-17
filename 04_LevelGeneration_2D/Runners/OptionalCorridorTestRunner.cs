using System.Collections.Generic;
using UnityEngine;

public class OptionalCorridorTestRunner : MonoBehaviour
{
    [Header("── Reference ─────────────────")]
    [Tooltip("4단계 결과 참조. 먼저 실행 필요")]
    [SerializeField] private MandatoryCorridorTestRunner mandatoryRef;

    [Header("── Attempt ───────────────────")]
    [Tooltip("추가 통로 생성 시도 횟수")]
    [Range(1, 100)]
    [SerializeField] private int attemptCount = 20;

    [Header("── Type Weights ──────────────")]
    [Tooltip("룸 연결 통로 생성 가중치")]
    [Range(0f, 1f)]
    [SerializeField] private float roomConnectWeight = 0.35f;

    [Tooltip("기존 통로 합류 가중치")]
    [Range(0f, 1f)]
    [SerializeField] private float corridorMergeWeight = 0.25f;

    [Tooltip("순환 통로 가중치")]
    [Range(0f, 1f)]
    [SerializeField] private float loopWeight = 0.15f;

    [Tooltip("막다른 길 가중치")]
    [Range(0f, 1f)]
    [SerializeField] private float deadEndWeight = 0.20f;

    [Tooltip("확장형 막다른 공간 가중치")]
    [Range(0f, 1f)]
    [SerializeField] private float expandedDeadEndWeight = 0.05f;

    [Header("── Dead End ──────────────────")]
    [Tooltip("막다른 길 최소 길이 (타일)")]
    [Range(1, 20)]
    [SerializeField] private int minDeadEndLength = 5;

    [Tooltip("막다른 길 최대 길이 (타일)")]
    [Range(5, 50)]
    [SerializeField] private int maxDeadEndLength = 18;

    private Dictionary<Vector2Int, int> _cellOwnerCorridorId = new Dictionary<Vector2Int, int>();
    private Dictionary<int, OptionalCorridorData> _corridorById = new Dictionary<int, OptionalCorridorData>();
    private OptionalCorridorResult _result;

    public OptionalCorridorResult LastResult => _result;
    public Dictionary<Vector2Int, int> CellOwnerCorridorId => _cellOwnerCorridorId;
    public Dictionary<int, OptionalCorridorData> CorridorById => _corridorById;
    public MapGrid Grid => mandatoryRef != null ? mandatoryRef.Grid : null;


    [ContextMenu("Run Optional Corridors")]
    public void Run()
    {
        if (mandatoryRef == null || mandatoryRef.Grid == null)
        {
            Debug.LogWarning("[Optional] Run MandatoryCorridorTestRunner first.");
            return;
        }

        var config = new OptionalCorridorConfig
        {
            AttemptCount = attemptCount,
            RoomConnectWeight = roomConnectWeight,
            CorridorMergeWeight = corridorMergeWeight,
            LoopWeight = loopWeight,
            DeadEndWeight = deadEndWeight,
            ExpandedDeadEndWeight = expandedDeadEndWeight,
            MinDeadEndLength = minDeadEndLength,
            MaxDeadEndLength = maxDeadEndLength
        };

        _cellOwnerCorridorId.Clear();
        _corridorById.Clear();

        var random = mandatoryRef.SeedContext.GetSource(MapGenerationPass.OptionalCorridor);
        _result = OptionalCorridorGenerator.Generate(
            mandatoryRef.Rooms, mandatoryRef.Grid, config, random,
            _cellOwnerCorridorId, _corridorById);

        Debug.Log($"[Optional] Total={_result.Corridors.Count} Rejected={_result.RejectedAttempts} " +
                  $"RoomConnect={_result.TypeCounts[CorridorTerminationType.RoomConnect]} " +
                  $"Merge={_result.TypeCounts[CorridorTerminationType.CorridorMerge]} " +
                  $"Loop={_result.TypeCounts[CorridorTerminationType.Loop]} " +
                  $"DeadEnd={_result.TypeCounts[CorridorTerminationType.DeadEnd]} " +
                  $"ExpandedDeadEnd={_result.TypeCounts[CorridorTerminationType.ExpandedDeadEnd]}");
    }

    void OnDrawGizmos()
    {
        if (_result == null) return;

        foreach (var corridor in _result.Corridors)
        {
            Gizmos.color = corridor.Type switch
            {
                CorridorTerminationType.RoomConnect => Color.cyan,
                CorridorTerminationType.CorridorMerge => Color.blue,
                CorridorTerminationType.Loop => new Color(1f, 0.5f, 0f),
                CorridorTerminationType.DeadEnd => Color.gray,
                CorridorTerminationType.ExpandedDeadEnd => Color.magenta,
                _ => Color.white
            };

            foreach (var cell in corridor.ExpandedCells)
                Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), new Vector3(0.85f, 0.85f, 0.05f));
        }
    }
}