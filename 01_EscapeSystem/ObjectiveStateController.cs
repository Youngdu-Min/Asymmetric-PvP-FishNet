using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectiveStateController : NetworkBehaviour
{
    [Header("Tilemap")]
    [SerializeField] private Tilemap _tilemap;

    [Header("Exit Door")]
    [SerializeField] private EscapeRevealChannel _escapeRevealChannel;
    [SerializeField] private RPCActivator _escapeDoorRoot;
    [SerializeField] private int _minDoorItemDistance = 15;

    [Header("Chest")]
    [SerializeField] private EscapeChest _escapeChest;
    [SerializeField] private int _chestCount = 3;

    [Header("Item")]
    [SerializeField] private ItemData _itemAData;
    [SerializeField] private int _itemSpawnCount = 5;
    [SerializeField] private ItemData _itemBData;

    [SerializeField] private Transform _itemARoot;
    [SerializeField] private Transform _itemBRoot;
    [SerializeField] private Transform _chestRoot;
    [SerializeField] private Transform _doorRoot;

    [Header("Spawn Overlap")]
    [SerializeField] private float _minSpawnRadius = 2f;

    public IReadOnlyList<Vector3> OccupiedPositions => _occupiedPositions;
    private readonly List<Vector3> _occupiedPositions = new();
    private MapGenerationResult _mapResult;
    private RPCActivator _spawnedDoorRoot;

    // --- 계층 동기화용 SyncList 구조체 ---
    public struct PropParentData
    {
        public int ObjectId;
        public int RootIndex;
    }

    private readonly SyncList<PropParentData> _parentDataList = new();

    void OnEnable()
    {
        MapGenerationMasterRunner.OnMapGenerated += OnMapGenerated;
        if (MapGenerationMasterRunner.LastResult != null)
            OnMapGenerated(MapGenerationMasterRunner.LastResult);
    }

    void OnDisable() => MapGenerationMasterRunner.OnMapGenerated -= OnMapGenerated;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _parentDataList.OnChange += OnParentDataListChanged;
        if (InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Objects != null)
        {
            InstanceFinder.ClientManager.Objects.OnSpawnedAdd += OnSpawnedAdd;
        }

        ApplyAllExistingParents();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _parentDataList.OnChange -= OnParentDataListChanged;
        if (InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Objects != null)
        {
            InstanceFinder.ClientManager.Objects.OnSpawnedAdd -= OnSpawnedAdd;
        }
    }

    void OnMapGenerated(MapGenerationResult result)
    {
        _mapResult = result;
        if (IsServerStarted) ServerSetup();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_mapResult != null) ServerSetup();
    }

    // --- 클라이언트 부모 정돈 핵심 로직 ---

    private void OnParentDataListChanged(SyncListOperation op, int index, PropParentData oldItem, PropParentData newItem, bool asServer)
    {
        if (asServer) return;

        if (op == SyncListOperation.Add || op == SyncListOperation.Set)
        {
            TrySetParentByData(newItem);
        }
    }

    private void OnSpawnedAdd(int objectId, NetworkObject nob)
    {
        foreach (var data in _parentDataList)
        {
            if (data.ObjectId != objectId) continue;
            Transform targetRoot = GetRootByIndex(data.RootIndex);
            if (targetRoot != null && nob != null)
            {
                nob.transform.SetParent(targetRoot);
            }
            break;
        }
    }

    private void ApplyAllExistingParents()
    {
        foreach (var data in _parentDataList)
        {
            TrySetParentByData(data);
        }
    }

    private void TrySetParentByData(PropParentData data)
    {
        if (!InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(data.ObjectId, out NetworkObject nob))
        {
            return;
        }
        Transform targetRoot = GetRootByIndex(data.RootIndex);
        if (targetRoot != null && nob != null)
        {
            nob.transform.SetParent(targetRoot);
        }
    }

    // --- 서버 전용 헬퍼 ---

    [Server]
    private void SetParentAndTrack(NetworkObject nob, Transform root)
    {
        if (nob == null || root == null) return;

        nob.transform.SetParent(root);

        int rootIndex = GetRootIndex(root);
        if (rootIndex != -1)
        {
            _parentDataList.Add(new PropParentData
            {
                ObjectId = nob.ObjectId,
                RootIndex = rootIndex
            });
        }
    }

    private int GetRootIndex(Transform root)
    {
        if (root == _itemARoot) return 0;
        if (root == _itemBRoot) return 1;
        if (root == _chestRoot) return 2;
        if (root == _doorRoot) return 3;
        return -1;
    }

    private Transform GetRootByIndex(int index) => index switch
    {
        0 => _itemARoot,
        1 => _itemBRoot,
        2 => _chestRoot,
        3 => _doorRoot,
        _ => null
    };

    // --- 스폰 및 설정 구현부 ---

    [Server]
    private void ServerSetup()
    {
        _occupiedPositions.Clear();
        _parentDataList.Clear();

        SpawnDoorInactive();

        EscapeChest[] chests = SpawnChests();
        if (chests == null || chests.Length == 0) return;

        GameObject[] itemAs = SpawnItemAs();

        foreach (var chest in chests)
            chest.OnOpened += () => OnChestCompleted(chests, itemAs);
    }

    [Server]
    private void SpawnDoorInactive()
    {
        var goalResult = _mapResult.StartGoalResult;
        int idx = Random.Range(0, goalResult.GoalRoomIndices.Count);
        var goalRoom = _mapResult.Rooms[goalResult.GoalRoomIndices[idx]];
        Vector3 doorWorld = RoomCenterToWorld(goalRoom);

        _occupiedPositions.Add(doorWorld);
        _spawnedDoorRoot = Instantiate(_escapeDoorRoot, doorWorld, Quaternion.identity);

        ServerManager.Spawn(_spawnedDoorRoot.gameObject);
        SetParentAndTrack(_spawnedDoorRoot.NetworkObject, _doorRoot);

        _spawnedDoorRoot.RpcSetActive(false);
    }

    [Server]
    private void OnChestCompleted(EscapeChest[] chests, GameObject[] itemAs)
    {
        foreach (var c in chests) c.ResetCount();
        foreach (var item in itemAs) if (item != null) Despawn(item);

        // 접속한 전 클라이언트를 순회 — 로컬 커넥션만 보면 호스트 1명만 초기화됨
        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            var playerObj = conn.FirstObject;
            if (playerObj != null && playerObj.TryGetComponent(out SurvivorInventory inv))
                inv.ClearItems();
        }

        SpawnFinalPhaseProps();
    }

    [Server]
    private void SpawnFinalPhaseProps()
    {
        if (_mapResult == null || _spawnedDoorRoot == null) return;

        Vector3? itemBWorld = GetRandomFloorCellFarFrom(_spawnedDoorRoot.transform.position, _minDoorItemDistance);
        if (itemBWorld == null)
        {
            Debug.LogWarning("[EscapePropGenerator] 아이템 B 거리 조건 만족 위치 없음, 임의 배치.");
            itemBWorld = GetRandomFloorCell();
        }

        var itemB = Instantiate(_itemBData.ItemPrefab, itemBWorld.Value, Quaternion.identity);

        ServerManager.Spawn(itemB);
        if (itemB.TryGetComponent(out NetworkObject nob))
        {
            SetParentAndTrack(nob, _itemBRoot);
        }

        _spawnedDoorRoot.RpcSetActive(true);
        RpcNotifyExitRevealed(_spawnedDoorRoot.NetworkObject);
    }

    // 방향 UI가 탈출구 위치를 알아야 하므로 NetworkObject로 전달 — BufferLast로 늦게 접속한 클라도 수신
    [ObserversRpc(BufferLast = true, RunLocally = true)]
    private void RpcNotifyExitRevealed(NetworkObject doorRoot)
    {
        if (_escapeRevealChannel != null && doorRoot != null)
            _escapeRevealChannel.Raise(doorRoot.transform);
    }

    [Server]
    private EscapeChest[] SpawnChests()
    {
        var rooms = _mapResult.Rooms;
        if (rooms == null || rooms.Count == 0) return null;

        var shuffled = Shuffled(rooms);
        int count = Mathf.Min(_chestCount, shuffled.Count);
        var result = new EscapeChest[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = RandomRoomCellToWorld(shuffled[i]);
            var chest = Instantiate(_escapeChest, pos, Quaternion.identity);

            ServerManager.Spawn(chest.gameObject);
            SetParentAndTrack(chest.NetworkObject, _chestRoot);

            result[i] = chest;
        }
        return result;
    }

    [Server]
    private GameObject[] SpawnItemAs()
    {
        var items = new GameObject[_itemSpawnCount];
        for (int i = 0; i < _itemSpawnCount; i++)
        {
            Vector3 pos = GetRandomFloorCell() ?? Vector3.zero;
            var obj = Instantiate(_itemAData.ItemPrefab, pos, Quaternion.identity);

            ServerManager.Spawn(obj);
            if (obj.TryGetComponent(out NetworkObject nob))
            {
                SetParentAndTrack(nob, _itemARoot);
            }

            items[i] = obj;
        }
        return items;
    }

    private Vector3 RoomCenterToWorld(RoomData room)
    {
        var center = room.Bounds.position + room.Bounds.size / 2;
        return _tilemap.CellToWorld(new Vector3Int(center.x, center.y, 0));
    }

    private Vector3 RandomRoomCellToWorld(RoomData room)
    {
        var cells = new List<Vector2Int>();
        for (int x = room.Bounds.xMin + 1; x < room.Bounds.xMax - 1; x++)
            for (int y = room.Bounds.yMin + 1; y < room.Bounds.yMax - 1; y++)
                cells.Add(new Vector2Int(x, y));

        cells = cells.FindAll(c =>
        {
            Vector3 w = _tilemap.CellToWorld(new Vector3Int(c.x, c.y, 0));
            return MapSpawnHelper.IsFarEnoughFromAll(w, _occupiedPositions, _minSpawnRadius);
        });

        if (cells.Count == 0)
        {
            int x = Random.Range(room.Bounds.xMin + 1, room.Bounds.xMax - 1);
            int y = Random.Range(room.Bounds.yMin + 1, room.Bounds.yMax - 1);
            Vector3 fallback = _tilemap.CellToWorld(new Vector3Int(x, y, 0));
            _occupiedPositions.Add(fallback);
            return fallback;
        }

        var chosen = cells[Random.Range(0, cells.Count)];
        Vector3 world = _tilemap.CellToWorld(new Vector3Int(chosen.x, chosen.y, 0));
        _occupiedPositions.Add(world);
        return world;
    }

    private List<Vector2Int> GetFloorCells()
        => MapSpawnHelper.GetFloorCellsAwayFromWalls(_mapResult.Grid);

    private Vector3? GetRandomFloorCell()
    {
        var cells = GetFloorCells();
        cells = cells.FindAll(c =>
        {
            Vector3 w = _tilemap.CellToWorld(new Vector3Int(c.x, c.y, 0));
            return MapSpawnHelper.IsFarEnoughFromAll(w, _occupiedPositions, _minSpawnRadius);
        });
        if (cells.Count == 0) return null;
        var chosen = cells[Random.Range(0, cells.Count)];
        Vector3 world = _tilemap.CellToWorld(new Vector3Int(chosen.x, chosen.y, 0));
        _occupiedPositions.Add(world);
        return world;
    }

    private Vector3? GetRandomFloorCellFarFrom(Vector3 origin, float minDist)
    {
        var cells = GetFloorCells();
        cells = cells.FindAll(c =>
        {
            Vector3 w = _tilemap.CellToWorld(new Vector3Int(c.x, c.y, 0));
            return Vector3.Distance(w, origin) >= minDist
                && MapSpawnHelper.IsFarEnoughFromAll(w, _occupiedPositions, _minSpawnRadius);
        });
        if (cells.Count == 0) return null;
        var chosen = cells[Random.Range(0, cells.Count)];
        Vector3 world = _tilemap.CellToWorld(new Vector3Int(chosen.x, chosen.y, 0));
        _occupiedPositions.Add(world);
        return world;
    }

    private List<T> Shuffled<T>(List<T> source)
    {
        var list = new List<T>(source);
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
        return list;
    }
}