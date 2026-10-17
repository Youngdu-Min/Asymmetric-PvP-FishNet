using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WorldItem : NetworkBehaviour
{
    [SerializeField] private ItemType _itemType;

    public ItemType ItemType => _itemType;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerInitialized) return;

        // 콜라이더는 자식에 있고 Health는 루트에 있음 — 루트에서 찾지 않으면 사망 판정이 통째로 무시됨
        if (collision.transform.root.TryGetComponent(out SurvivorHealth health) && !health.IsAlive) return;

        if (collision.transform.root.TryGetComponent(out SurvivorInventory inventory)
            && inventory.AddItemByType(_itemType))
        {
            base.Despawn();
        }
    }
}