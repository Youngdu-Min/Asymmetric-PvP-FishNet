public sealed class MandatoryCorridorConfig
{
    public int CorridorWidth = 3;
    public int MinCornerMargin = 1;

    // 스파이크에서 확정한 A* 비용 가중치
    public float EmptyCellCost = 5f;
    public float ExistingCorridorCost = 1f;
    public float RoomInteriorCost = 10f;
    public float RoomBufferCost = 7f;
}