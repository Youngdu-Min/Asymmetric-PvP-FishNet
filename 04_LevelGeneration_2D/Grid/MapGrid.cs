using UnityEngine;

public sealed class MapGrid
{
    private readonly MapCellType[,] _cells;
    public Vector2Int Size { get; }

    public MapGrid(Vector2Int size)
    {
        Size = size;
        _cells = new MapCellType[size.x, size.y];
    }

    public MapCellType this[Vector2Int pos]
    {
        get => InBounds(pos) ? _cells[pos.x, pos.y] : MapCellType.None;
        set { if (InBounds(pos)) _cells[pos.x, pos.y] = value; }
    }

    public bool InBounds(Vector2Int pos)
        => pos.x >= 0 && pos.x < Size.x && pos.y >= 0 && pos.y < Size.y;
}