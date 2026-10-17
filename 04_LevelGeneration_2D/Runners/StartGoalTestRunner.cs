using System.Collections.Generic;
using UnityEngine;

public class StartGoalTestRunner : MonoBehaviour
{
    [Header("── Reference ─────────────────")]
    [Tooltip("4단계 결과 참조. 먼저 실행 필요")]
    [SerializeField] private MandatoryCorridorTestRunner mandatoryRef;

    [Header("── Constraints ───────────────")]
    [Tooltip("Start-Goal 간 최소 그래프 거리 (룸 홉 수)")]
    [Range(1, 10)]
    [SerializeField] private int minGraphDistance = 4;

    [Tooltip("Start-Goal 직선거리가 맵 대각선 대비 최소 비율 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float minStraightLineRatio = 0.40f;

    private StartGoalResult _result;
    private List<RoomData> _rooms;

    public StartGoalResult LastResult => _result;

    [ContextMenu("Run Start/Goal Placement")]
    public void Run()
    {
        if (mandatoryRef == null || mandatoryRef.Rooms == null)
        {
            Debug.LogWarning("[StartGoal] Run MandatoryCorridorTestRunner first.");
            return;
        }

        _rooms = mandatoryRef.Rooms;

        var config = new StartGoalConfig
        {
            MinGraphDistance = minGraphDistance,
            MinStraightLineRatio = minStraightLineRatio,
            GoalCandidateCount = 3
        };

        var random = mandatoryRef.SeedContext.GetSource(MapGenerationPass.StartGoalPlacement);
        _result = StartGoalPlacer.Place(_rooms, mandatoryRef.Topology, mandatoryRef.MapSizeValue, config, random);

        Debug.Log($"[StartGoal] Success={_result.Success} Start=Room{_result.StartRoomIndex} " +
                  $"SelectedGoal=Room{_result.SelectedGoalRoomIndex} " +
                  $"GraphDist={_result.GoalGraphDistances[0]}(met={_result.MetMinGraphDistance}) " +
                  $"StraightRatio={_result.StraightLineRatio:P1}(met={_result.MetMinStraightLineRatio})");

        for (int i = 0; i < _result.GoalRoomIndices.Count; i++)
        {
            bool selected = _result.GoalRoomIndices[i] == _result.SelectedGoalRoomIndex;
            Debug.Log($"[StartGoal] Goal[{i}] Room{_result.GoalRoomIndices[i]} " +
                      $"GraphDist={_result.GoalGraphDistances[i]} Selected={selected}");
        }

        if (!_result.Success)
            Debug.LogWarning($"[StartGoal] {_result.FailureReason}");
    }

    void OnDrawGizmos()
    {
        if (_result == null || _rooms == null) return;

        // Start
        Gizmos.color = Color.green;
        var startPos = new Vector3(_result.StartPosition.x, _result.StartPosition.y, -0.2f);
        Gizmos.DrawSphere(startPos, 0.6f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(startPos + Vector3.up * 1.2f, "Start");
#endif

        // Goal 후보 3개
        for (int i = 0; i < _result.GoalRoomIndices.Count; i++)
        {
            bool selected = _result.GoalRoomIndices[i] == _result.SelectedGoalRoomIndex;
            Gizmos.color = selected ? Color.red : Color.yellow;

            var pos = new Vector3(_result.GoalPositions[i].x, _result.GoalPositions[i].y, -0.2f);
            Gizmos.DrawSphere(pos, selected ? 0.6f : 0.4f);

#if UNITY_EDITOR
            string label = $"Goal[{i}]\nDist={_result.GoalGraphDistances[i]}\n{(selected ? "★선택" : "미선택")}";
            UnityEditor.Handles.Label(pos + Vector3.up * 1.2f, label);
#endif
        }
    }
}