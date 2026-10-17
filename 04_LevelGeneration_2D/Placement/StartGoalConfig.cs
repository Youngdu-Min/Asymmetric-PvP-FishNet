public sealed class StartGoalConfig
{
    public int MinGraphDistance = 4;        // 기획서 11장: 그래프 기준 최소 4개 룸
    public float MinStraightLineRatio = 0.40f; // 맵 대각선 길이의 40% 이상 우선
    public int GoalCandidateCount = 3;
}