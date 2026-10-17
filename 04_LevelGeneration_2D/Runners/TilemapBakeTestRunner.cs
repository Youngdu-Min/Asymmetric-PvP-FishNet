// TilemapBakeTestRunner.cs
using NavMeshPlus.Components;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBakeTestRunner : MonoBehaviour
{
    [System.Serializable]
    public struct TileInfo
    {
        [Tooltip("벽지 타일 에셋")]
        public TileBase Tile;

        [Tooltip("타일 배치 오프셋")]
        public Vector3 Offset;
    }

    [Header("── Reference ─────────────────")]
    [Tooltip("8단계 Validation 결과 기준 Grid 사용")]
    [SerializeField] private ValidationTestRunner m_validationRef;

    [Tooltip("4단계 필수 통로 결과 참조")]
    [SerializeField] private MandatoryCorridorTestRunner m_mandatoryRef;

    [Tooltip("5단계 추가 통로 결과 참조")]
    [SerializeField] private OptionalCorridorTestRunner m_optionalRef;

    [Tooltip("스폰된 오브젝트 위치 참조 (프롭 타일 겹침 방지)")]
    [SerializeField] private ObjectiveStateController m_objectiveStateControllerRef;

    [Tooltip("타일맵에 맞게 NavMesh Bake용 참조")]
    [SerializeField] private NavMeshSurface m_navMeshSurface;

    [Header("── Tilemaps ──────────────────")]
    [Tooltip("바닥 타일맵 (Room + Corridor)")]
    [SerializeField] private Tilemap m_floorTilemap;

    [Tooltip("벽 타일맵 (None 셀 전체)")]
    [SerializeField] private Tilemap m_wallTilemap;

    [Tooltip("벽지 타일맵 (벽지 교체용 전용 레이어)")]
    [SerializeField] private Tilemap m_wallpaperTilemap;

    [Tooltip("프롭 타일맵 (방 안 장식 전용 레이어)")]
    [SerializeField] private Tilemap m_propTilemap;

    [Header("── Tiles ─────────────────────")]
    [Tooltip("바닥 타일 에셋")]
    [SerializeField] private TileBase m_floorTile;

    [Tooltip("벽 타일 에셋 (Rule Tile 호환)")]
    [SerializeField] private TileBase m_wallTile;

    [Tooltip("전체 맵 외곽 테두리 확장 두께 (타일 단위)")]
    [Range(0, 10)]
    [SerializeField] private int m_mapBorderPadding = 1;

    [Tooltip("룸 인접 벽 그룹 교체 확률 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float m_roomWallpaperChance = 0.3f;

    [Tooltip("통로 인접 벽 그룹 교체 확률 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float m_corridorWallpaperChance = 0.15f;

    [Tooltip("벽지 타일 후보 목록 (타일 + 오프셋)")]
    [SerializeField] private TileInfo[] m_wallpaperTiles;

    [Header("── Props ─────────────────────")]
    [Tooltip("방 안에 배치할 프롭 타일 후보 목록")]
    [SerializeField] private TileBase[] m_propTiles;

    [Tooltip("방 1개당 프롭 타일 최대 배치 개수")]
    [Range(0, 20)]
    [SerializeField] private int m_maxPropsPerRoom = 3;

    [Tooltip("프롭 간 최소 거리 (타일 단위)")]
    [Range(1, 5)]
    [SerializeField] private int m_minPropDistance = 2;

    [Header("── Config ────────────────────")]
    [Tooltip("벽 타일 굽기 여부. wallTilemap 미할당 시 자동 스킵")]
    [SerializeField] private bool m_bakeWalls = true;

    [Tooltip("Tilemap 원점 오프셋")]
    [SerializeField] private Vector3Int m_offset = Vector3Int.zero;

    public MapGrid Grid => m_validationRef?.Grid ?? null;

    [ContextMenu("Bake Tilemap")]
    public void Bake()
    {
        if (m_validationRef == null || m_validationRef.Grid == null)
        {
            Debug.LogWarning("[TilemapBake] Run ValidationTestRunner first.");
            return;
        }

        if (m_floorTilemap == null)
        {
            Debug.LogWarning("[TilemapBake] floorTilemap not assigned.");
            return;
        }

        TilemapBakeConfig config = new TilemapBakeConfig
        {
            FloorTile = m_floorTile,
            WallTile = m_wallTile,
            BakeWalls = m_bakeWalls && m_wallTilemap != null,
            Offset = m_offset
        };

        MapGenerationResult mapResult = MapGenerationMasterRunner.LastResult;
        if (mapResult == null) return;

        Random.State prevState = Random.state;
        Random.InitState(mapResult.PropBakeSeed);

        MapGrid grid = m_validationRef.Grid;

        TilemapBaker.Bake(grid, config, m_floorTilemap, m_wallTilemap);
        BakeOuterMapBorder(grid);
        BakeWallpaper(grid);
        BakeProps(grid);

        m_floorTilemap.RefreshAllTiles();
        m_wallTilemap?.RefreshAllTiles();
        m_wallpaperTilemap?.RefreshAllTiles();
        m_propTilemap?.RefreshAllTiles();

        int floorCells = Count(grid, MapCellType.Room) + Count(grid, MapCellType.Corridor);
        Debug.Log($"[TilemapBake] Done. FloorCells={floorCells} WallsBaked={config.BakeWalls}");
        Random.state = prevState;

        m_navMeshSurface?.BuildNavMesh();
    }

    private void BakeOuterMapBorder(MapGrid grid)
    {
        if (!m_bakeWalls || m_wallTilemap == null || m_wallTile == null || m_mapBorderPadding <= 0) return;

        int minX = -m_mapBorderPadding;
        int maxX = grid.Size.x + m_mapBorderPadding - 1;
        int minY = -m_mapBorderPadding;
        int maxY = grid.Size.y + m_mapBorderPadding - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (grid.InBounds(pos) && grid[pos] != MapCellType.None) continue;

                Vector3Int tilePos = new Vector3Int(x, y, 0) + m_offset;
                m_wallTilemap.SetTile(tilePos, m_wallTile);
            }
        }
    }

    private void BakeWallpaper(MapGrid grid)
    {
        if (m_wallpaperTilemap == null || m_wallpaperTiles == null || m_wallpaperTiles.Length == 0) return;

        m_wallpaperTilemap.ClearAllTiles();

        MapGenerationResult mapResult = MapGenerationMasterRunner.LastResult;
        if (mapResult == null) return;

        foreach (RoomData room in mapResult.Rooms)
        {
            if (Random.value > m_roomWallpaperChance) continue;
            ApplyWallpaperToGroup(GetRoomAdjacentWalls(room, grid), grid);
        }

        if (m_mandatoryRef?.CorridorResult != null)
        {
            foreach (CorridorData corridor in m_mandatoryRef.CorridorResult.Corridors)
            {
                if (Random.value > m_corridorWallpaperChance) continue;
                ApplyWallpaperToGroup(GetCorridorAdjacentWallsFromCells(corridor.ExpandedCells, grid), grid);
            }
        }

        if (m_optionalRef?.LastResult != null)
        {
            foreach (OptionalCorridorData corridor in m_optionalRef.LastResult.Corridors)
            {
                if (Random.value > m_corridorWallpaperChance) continue;
                ApplyWallpaperToGroup(GetCorridorAdjacentWallsFromCells(corridor.ExpandedCells, grid), grid);
            }
        }
    }

    private List<Vector2Int> GetCorridorAdjacentWallsFromCells(HashSet<Vector2Int> corridorCells, MapGrid grid)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        Vector2Int[] dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int cell in corridorCells)
        {
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int neighbor = cell + dir;
                if (!grid.InBounds(neighbor)) continue;
                if (grid[neighbor] == MapCellType.None)
                    result.Add(neighbor);
            }
        }
        return new List<Vector2Int>(result);
    }

    private void ApplyWallpaperToGroup(List<Vector2Int> wallCells, MapGrid grid)
    {
        if (m_wallpaperTiles == null || m_wallpaperTiles.Length == 0) return;

        foreach (Vector2Int cell in wallCells)
        {
            Vector2Int south = new Vector2Int(cell.x, cell.y - 1);
            if (!grid.InBounds(south)) continue;
            MapCellType southType = grid[south];
            if (southType != MapCellType.Room && southType != MapCellType.Corridor) continue;

            Vector2Int left = new Vector2Int(cell.x - 1, cell.y);
            Vector2Int right = new Vector2Int(cell.x + 1, cell.y);
            bool leftIsWall = grid.InBounds(left) && grid[left] == MapCellType.None;
            bool rightIsWall = grid.InBounds(right) && grid[right] == MapCellType.None;
            if (!leftIsWall && !rightIsWall) continue;

            TileInfo entry = m_wallpaperTiles[Random.Range(0, m_wallpaperTiles.Length)];
            if (entry.Tile == null) continue;

            Vector3Int tilePos = new Vector3Int(cell.x, cell.y, 0) + m_offset;
            m_wallpaperTilemap.SetTile(tilePos, entry.Tile);
            m_wallpaperTilemap.SetTransformMatrix(tilePos, Matrix4x4.Translate(entry.Offset));
        }
    }

    private List<Vector2Int> GetRoomAdjacentWalls(RoomData room, MapGrid grid)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        RectInt bounds = room.Bounds;

        for (int x = bounds.xMin - 1; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin - 1; y <= bounds.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!grid.InBounds(pos)) continue;
                if (grid[pos] != MapCellType.None) continue;
                result.Add(pos);
            }
        }
        return result;
    }

    private bool IsAdjacentTo(Vector2Int pos, MapCellType type, MapGrid grid)
    {
        Vector2Int[] dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int dir in dirs)
        {
            Vector2Int neighbor = pos + dir;
            if (grid.InBounds(neighbor) && grid[neighbor] == type)
                return true;
        }
        return false;
    }

    private void BakeProps(MapGrid grid)
    {
        if (m_propTilemap == null || m_propTiles == null || m_propTiles.Length == 0) return;

        m_propTilemap.ClearAllTiles();

        MapGenerationResult mapResult = MapGenerationMasterRunner.LastResult;
        if (mapResult == null) return;

        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        if (m_objectiveStateControllerRef != null)
        {
            foreach (Vector3 worldPos in m_objectiveStateControllerRef.OccupiedPositions)
            {
                Vector2Int cell = (Vector2Int)(Vector3Int)m_floorTilemap.WorldToCell(worldPos);
                occupiedCells.Add(cell);
            }
        }

        foreach (RoomData room in mapResult.Rooms)
        {
            List<Vector2Int> candidates = GetRoomInnerCells(room, occupiedCells);
            int count = Mathf.Min(m_maxPropsPerRoom, candidates.Count);

            for (int i = 0; i < count; i++)
            {
                if (candidates.Count == 0) break;

                int idx = Random.Range(0, candidates.Count);
                Vector2Int chosen = candidates[idx];
                candidates.RemoveAt(idx);

                TileBase prop = m_propTiles[Random.Range(0, m_propTiles.Length)];
                m_propTilemap.SetTile(new Vector3Int(chosen.x, chosen.y, 0) + m_offset, prop);
                occupiedCells.Add(chosen);

                candidates.RemoveAll(c =>
                    Mathf.Abs(c.x - chosen.x) < m_minPropDistance &&
                    Mathf.Abs(c.y - chosen.y) < m_minPropDistance);
            }
        }
    }

    private List<Vector2Int> GetRoomInnerCells(RoomData room, HashSet<Vector2Int> occupied)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int x = room.Bounds.xMin + 1; x < room.Bounds.xMax - 1; x++)
        {
            for (int y = room.Bounds.yMin + 1; y < room.Bounds.yMax - 1; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!occupied.Contains(pos))
                    result.Add(pos);
            }
        }
        return result;
    }

    [ContextMenu("Clear Tilemaps")]
    private void Clear()
    {
        m_floorTilemap?.ClearAllTiles();
        m_wallTilemap?.ClearAllTiles();
        m_wallpaperTilemap?.ClearAllTiles();
        m_propTilemap?.ClearAllTiles();
        Debug.Log("[TilemapBake] Cleared.");
    }

    private int Count(MapGrid grid, MapCellType type)
    {
        int n = 0;
        for (int x = 0; x < grid.Size.x; x++)
        {
            for (int y = 0; y < grid.Size.y; y++)
            {
                if (grid[new Vector2Int(x, y)] == type) n++;
            }
        }
        return n;
    }
}