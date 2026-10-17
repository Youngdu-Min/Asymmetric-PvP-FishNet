using System.Collections.Generic;
using UnityEngine;

public sealed class StartGoalResult
{
    public int StartRoomIndex;
    public Vector2Int StartPosition;

    // Goal 후보 3개 (그래프 거리 내림차순)
    public List<int> GoalRoomIndices = new List<int>();
    public List<Vector2Int> GoalPositions = new List<Vector2Int>();
    public List<int> GoalGraphDistances = new List<int>();

    // 랜덤 선택된 1개
    public int SelectedGoalRoomIndex;
    public Vector2Int SelectedGoalPosition;

    public int GoalRoomIndex => SelectedGoalRoomIndex;
    public Vector2Int GoalPosition => SelectedGoalPosition;
    public int GraphDistance => GoalGraphDistances.Count > 0 ? GoalGraphDistances[0] : 0;

    public float StraightLineRatio;
    public bool MetMinGraphDistance;
    public bool MetMinStraightLineRatio;
    public bool Success;
    public string FailureReason;
}