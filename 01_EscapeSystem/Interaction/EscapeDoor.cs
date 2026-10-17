using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EscapeDoor : LockedInteractable
{
    [SerializeField] private Sprite _openSprite;
    private SpriteRenderer _sprite;

    public override string ActionGuideText => "탈출구 열기";
    public override string ProgressLabelText => "탈출구 개방 중";
    public override void OnStartClient()
    {
        base.OnStartClient();
        _sprite = GetComponent<SpriteRenderer>();
        OnOpened += () =>
        {
            if (_sprite != null)
            {
                _sprite.sprite = _openSprite; // 탈출구가 열렸음을 시각적으로 표시
            }
        };
    }

    [ObserversRpc(RunLocally = true)]
    public void RpcSetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerInitialized || !_isOpened.Value) return;

        if (collision.transform.root.TryGetComponent(out Survivor survivor))
        {
            // 접촉 탈출에 성공한 생존자는 즉시 필드에서 제거
            survivor.OnEscapeSuccess(); // 필드 제거 및 생존 판정 로직 포함 권장
        }
    }
}