using UnityEngine;

public class DeadEndCleanupTestRunner : MonoBehaviour
{
    public DeadEndCleanupResult LastResult => _result;

    [Header("── Reference ─────────────────")]
    [Tooltip("5단계 결과 참조. 먼저 실행 필요")]
    [SerializeField] private OptionalCorridorTestRunner optionalRef;

    [Header("── Length Filter ──────────────")]
    [Tooltip("유지할 막다른 길 최소 길이")]
    [Range(1, 20)]
    [SerializeField] private int minDeadEndLength = 5;

    [Tooltip("유지할 막다른 길 최대 길이")]
    [Range(5, 50)]
    [SerializeField] private int maxDeadEndLength = 18;

    [Header("── Depth Filter ──────────────")]
    [Tooltip("허용할 연속 막다른 분기 최대 깊이")]
    [Range(1, 5)]
    [SerializeField] private int maxConsecutiveDepth = 2;

    [Header("── Area Filter ───────────────")]
    [Tooltip("전체 통로 면적 대비 막다른 길 최대 비율 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float maxDeadEndAreaRatio = 0.30f;

    private DeadEndCleanupResult _result;

    [ContextMenu("Run Dead-End Cleanup")]
    public void Run()
    {
        if (optionalRef == null || optionalRef.LastResult == null)
        {
            Debug.LogWarning("[Cleanup] Run OptionalCorridorTestRunner first.");
            return;
        }

        var config = new DeadEndCleanupConfig
        {
            MinDeadEndLength = minDeadEndLength,
            MaxDeadEndLength = maxDeadEndLength,
            MaxConsecutiveDeadEndDepth = maxConsecutiveDepth,
            MaxDeadEndAreaRatio = maxDeadEndAreaRatio
        };

        _result = DeadEndCleanup.Run(
            optionalRef.LastResult.Corridors,
            optionalRef.CellOwnerCorridorId,
            optionalRef.CorridorById,
            optionalRef.Grid,
            config);

        Debug.Log($"[Cleanup] Kept={_result.KeptDeadEnds.Count} Removed={_result.RemovedDeadEnds.Count} " +
                  $"FinalAreaRatio={_result.FinalDeadEndAreaRatio:P1}");

        for (int i = 0; i < _result.RemovedDeadEnds.Count; i++)
            Debug.Log($"[Cleanup] Removed reason: {_result.RemovalReasons[i]}");
    }

    void OnDrawGizmos()
    {
        if (_result == null) return;

        Gizmos.color = Color.green;
        foreach (var kept in _result.KeptDeadEnds)
            foreach (var cell in kept.ExpandedCells)
                Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), new Vector3(0.8f, 0.8f, 0.05f));

        Gizmos.color = Color.red;
        foreach (var removed in _result.RemovedDeadEnds)
            foreach (var cell in removed.ExpandedCells)
                Gizmos.DrawCube(new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0), new Vector3(0.8f, 0.8f, 0.05f));
    }
}