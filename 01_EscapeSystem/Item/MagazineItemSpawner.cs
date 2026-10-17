using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MagazineItemSpawner : NetworkBehaviour
{
    [SerializeField] private MagazineItem _prefab;
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private int _spawnCount = 5;

    private List<Vector2Int> _floorCells = new();
    private List<MagazineItem> _items = new();
    private MapGenerationResult _mapResult;
    private bool _mapReady;

    public int InitialCount => _spawnCount;
    public List<Vector2Int> FloorCells => _floorCells;

    void OnEnable()
    {
        MapGenerationMasterRunner.OnMapGenerated += OnMapGenerated;
        if (MapGenerationMasterRunner.LastResult != null)
            OnMapGenerated(MapGenerationMasterRunner.LastResult);
    }

    void OnDisable() => MapGenerationMasterRunner.OnMapGenerated -= OnMapGenerated;

    void OnMapGenerated(MapGenerationResult result)
    {
        _mapResult = result;
        CacheFloorCells();
        _mapReady = true;
        if (IsServerStarted) ServerSpawnAll();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_mapReady) ServerSpawnAll();
    }
    private void CacheFloorCells()
    {
        _floorCells = MapSpawnHelper.GetFloorCellsAwayFromWalls(_mapResult.Grid);
    }

    [Server]
    private void ServerSpawnAll()
    {
        print("스폰");


        for (int i = 0; i < _spawnCount; i++)
        {
            Vector3 pos = GetRandomFloorWorld();
            var item = Instantiate(_prefab, pos, Quaternion.identity);
            item.Initialize(this);
            ServerManager.Spawn(item.gameObject);
            if (item.TryGetComponent(out NetworkObject nob))
            {
                nob.SetParent(this);
            }
            _items.Add(item);
        }
    }

    public Vector3 GetRandomFloorWorld()
    {
        var cell = _floorCells[Random.Range(0, _floorCells.Count)];
        return _tilemap.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
    }

    public int GetActiveCount()
    {
        int count = 0;
        foreach (var item in _items)
            if (item != null && item.IsActive) count++;
        return count;
    }
}