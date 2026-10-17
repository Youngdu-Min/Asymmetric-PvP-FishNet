public sealed class DeadEndCleanupConfig
{
    public int MinDeadEndLength = 5;
    public int MaxDeadEndLength = 18;
    public int MaxConsecutiveDeadEndDepth = 2; // 기획서 10장: 연속 막다른 분기 깊이 최대 2단계
    public float MaxDeadEndAreaRatio = 0.30f;  // 기획서 10장: 전체 통로 면적의 30% 초과 금지
}