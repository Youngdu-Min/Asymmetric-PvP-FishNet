using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

public abstract class LockedInteractable : InteractableBase
{
    [SerializeField] private ItemData _requiredItem;
    private SurvivorInventory _cachedInventory;
    private EntityPathRevealController _entityPathRevealController;
    private bool _consumeItemOnUse = true;
    public event Action OnOpened;

    protected readonly SyncVar<bool> _isOpened = new SyncVar<bool>(new SyncTypeSettings
    {
        WritePermission = WritePermission.ServerOnly,
        ReadPermission = ReadPermission.Observers
    });

    public override void OnStartClient()
    {
        base.OnStartClient();
        _isOpened.OnChange += OnOpenedChanged;
    }

    // 이미 열렸거나 필요 아이템 미보유 시 안내/시도 차단 (서버 RequestStart 검사와 동일 기준)
    public override bool IsInteractable(SurvivorInventory inventory)
    {
        if (_isOpened.Value) return false;
        if (_requiredItem == null) return true;
        return inventory != null && inventory.HasItem(_requiredItem);
    }

    [Server]
    public override void RequestStart(int playerId)
    {
        if (_isOpened.Value) return;
        if (!CheckHasItem(playerId)) return;

        // 상호작용이 성공적으로 점유되기 전 상태 확인
        bool isAlreadyOccupied = OccupantId != -1;

        base.RequestStart(playerId);

        // 점유 성공 시 엔티티 위치 추적 경로 발동
        if (!isAlreadyOccupied && OccupantId == playerId)
        {
            NotifyEntityTracker(playerId);
        }
    }

    [Server]
    private void NotifyEntityTracker(int playerId)
    {
        if (!InstanceFinder.ServerManager.Clients.TryGetValue(playerId, out var conn))
            return;

        var playerInstance = conn.FirstObject;
        if (playerInstance != null && playerInstance.TryGetComponent(out Survivor survivor))
        {
            _entityPathRevealController ??= FindAnyObjectByType<EntityPathRevealController>();
            _entityPathRevealController?.StartTracking(survivor);
        }
    }

    protected override void OnComplete()
    {
        _isOpened.Value = true;
        ResetInteraction();
        ConsumeItem();
    }

    protected void ConsumeItem()
    {
        if (_consumeItemOnUse && _cachedInventory != null)
        {
            _cachedInventory.RemoveItem(_requiredItem);
            _cachedInventory = null;
        }
    }

    protected void OnOpenedChanged(bool prev, bool next, bool asServer)
    {
        if (asServer) return;

        if (next)
        {
            OnOpened?.Invoke();
        }
    }

    [Server]
    private bool CheckHasItem(int playerId)
    {
        if (_requiredItem == null) return true;

        if (!InstanceFinder.ServerManager.Clients.TryGetValue(playerId, out var conn))
            return false;

        var playerInstance = conn.FirstObject;
        if (playerInstance != null && playerInstance.TryGetComponent(out _cachedInventory))
            return _cachedInventory.HasItem(_requiredItem);

        return false;
    }
}
