public sealed class OptionalCorridorConfig
{
    public int CorridorWidth = 3;
    public int MinCornerMargin = 1;

    public int AttemptCount = 20;

    // 기획서 8.2 초기 비율
    public float RoomConnectWeight = 0.35f;
    public float CorridorMergeWeight = 0.25f;
    public float LoopWeight = 0.15f;
    public float DeadEndWeight = 0.20f;
    public float ExpandedDeadEndWeight = 0.05f;

    public int MinDeadEndLength = 5;
    public int MaxDeadEndLength = 18;
    public int ExpandedDeadEndRadius = 2;

    public int MinLoopPathLength = 6; // 너무 가까운 순환 방지 (기획서 5.4)

    public float EmptyCellCost = 5f;
    public float ExistingCorridorCost = 1f;
    public float RoomInteriorCost = 10f;
    public float RoomBufferCost = 7f;
}