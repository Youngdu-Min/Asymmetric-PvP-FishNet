using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TilemapBakeConfig
{
    public TileBase FloorTile;
    public TileBase WallTile;
    public Vector3Int Offset = Vector3Int.zero;
    public bool BakeWalls = true;
}

public static class TilemapBaker
{
    private static readonly Vector2Int[] Dirs4 = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private static readonly Vector2Int[] Dirs8 = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
    };

    public static void Bake(MapGrid grid, TilemapBakeConfig config, Tilemap floorMap, Tilemap wallMap)
    {
        floorMap.ClearAllTiles();
        wallMap?.ClearAllTiles();

        // 1패스: floor 전체 먼저 stamp
        for (int x = 0; x < grid.Size.x; x++)
        {
            for (int y = 0; y < grid.Size.y; y++)
            {
                var gridPos = new Vector2Int(x, y);
                var tilePos = new Vector3Int(x, y, 0) + config.Offset;
                var cell = grid[gridPos];

                if ((cell == MapCellType.Room || cell == MapCellType.Corridor) && config.FloorTile != null)
                    floorMap.SetTile(tilePos, config.FloorTile);
            }
        }

        // 2패스: wall stamp (floor 전부 깔린 뒤에 배치해야 Rule Tile 이웃 인식 정확)
        if (config.BakeWalls && wallMap != null && config.WallTile != null)
        {
            for (int x = 0; x < grid.Size.x; x++)
            {
                for (int y = 0; y < grid.Size.y; y++)
                {
                    var gridPos = new Vector2Int(x, y);
                    if (grid[gridPos] != MapCellType.None) continue;

                    var tilePos = new Vector3Int(x, y, 0) + config.Offset;
                    wallMap.SetTile(tilePos, config.WallTile);
                }
            }
        }

        // 3패스: 모든 stamp 완료 후 양쪽 refresh — Rule Tile 이웃 재계산
        floorMap.RefreshAllTiles();
        wallMap?.RefreshAllTiles();
    }
}