using UnityEngine;

public enum ItemType
{
    ItemA,
    ItemB,
    Ammo,
    Consumable  // 소모품 대비
}

[CreateAssetMenu(fileName = "ItemData", menuName = "SO/Data/ItemData")]
public class ItemData : ScriptableObject
{
    [SerializeField] private ItemType _itemType;
    [SerializeField] private GameObject _itemPrefab; // 필드에 떨어질 때 생성될 프리팹
    [SerializeField] private Sprite _icon; // HUD 아이템 칸에 표시될 아이콘
    [SerializeField] private bool _showCountInHud; // 아이템 칸에 (보유/최대) 개수를 표시할지
    [SerializeField] private int _itemLimit = int.MaxValue;

    public ItemType ItemType => _itemType;
    public GameObject ItemPrefab => _itemPrefab;
    public Sprite Icon => _icon;
    public bool ShowCountInHud => _showCountInHud && _itemLimit != int.MaxValue; // 최대치가 없으면 표시 불가
    public float DecreaseSpeedMultiplier = 0f; // 아이템을 들었을 때 이동 속도 감소 배율
    public int ItemLimit => _itemLimit;
}