using UnityEngine;

public class ValidationTestRunner : MonoBehaviour
{
    [Header("── References ────────────────")]
    [Tooltip("4단계 Runner. 룸/그리드 데이터 소스")]
    [SerializeField] private MandatoryCorridorTestRunner mandatoryRef;

    [Tooltip("5단계 Runner. 추가 통로 결과")]
    [SerializeField] private OptionalCorridorTestRunner optionalRef;

    [Tooltip("6단계 Runner. 막다른 길 정리 결과")]
    [SerializeField] private DeadEndCleanupTestRunner cleanupRef;

    [Tooltip("7단계 Runner. Start/Goal 배치 결과")]
    [SerializeField] private StartGoalTestRunner startGoalRef;

    public StartGoalResult StartGoalResultValue => startGoalRef?.LastResult;
    public MapGrid Grid => mandatoryRef?.Grid;

    private ValidationResult _result;

    [ContextMenu("Run Validation")]
    public void Run()
    {
        if (mandatoryRef == null || mandatoryRef.Rooms == null)
        {
            Debug.LogWarning("[Validation] Run MandatoryCorridorTestRunner first.");
            return;
        }

        var diagnostics = new GenerationDiagnostics();
        diagnostics.Begin(0, "0.1", "test");

        _result = MapValidator.Validate(
            mandatoryRef.Rooms,
            mandatoryRef.Grid,
            mandatoryRef.GetComponent<MandatoryCorridorTestRunner>() != null
                ? GetMandatoryResult() : null,
            optionalRef?.LastResult,
            cleanupRef?.LastResult,
            startGoalRef?.LastResult,
            diagnostics);

        Debug.Log(
            $"[Validation] Success={_result.Success} " +
            $"AllRoomsReachable={_result.AllRoomsReachable} " +
            $"Rooms={_result.RoomCount} " +
            $"MandatoryCorridors={_result.MandatoryCorridorCount} " +
            $"OptionalCorridors={_result.OptionalCorridorCount} " +
            $"RoomCells={_result.TotalRoomCells} " +
            $"CorridorCells={_result.TotalCorridorCells} " +
            $"Ratio={_result.CorridorToRoomRatio:F2} " +
            $"KeptDeadEnds={_result.KeptDeadEndCount} " +
            $"RemovedDeadEnds={_result.RemovedDeadEndCount} " +
            $"SGGraphDist={_result.StartGoalGraphDistance} " +
            $"SGStraightRatio={_result.StartGoalStraightLineRatio:P1}");

        foreach (var reason in _result.FailureReasons)
            Debug.LogWarning($"[Validation] FAIL: {reason}");

        // Diagnostics 전체 Stats 출력
        foreach (var kv in diagnostics.Stats)
            Debug.Log($"[Diagnostics] {kv.Key}={kv.Value}");
    }

    private MandatoryCorridorResult GetMandatoryResult()
        => mandatoryRef.CorridorResult;

    void OnDrawGizmos()
    {
        if (_result == null || mandatoryRef?.Rooms == null) return;

        // 미도달 룸 빨간 강조
        Gizmos.color = Color.red;
        foreach (var idx in _result.UnreachableRoomIndices)
        {
            var room = mandatoryRef.Rooms[idx];
            Vector3 center = new Vector3(room.Bounds.center.x, room.Bounds.center.y, 0);
            Vector3 size = new Vector3(room.Bounds.size.x, room.Bounds.size.y, 0.2f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}