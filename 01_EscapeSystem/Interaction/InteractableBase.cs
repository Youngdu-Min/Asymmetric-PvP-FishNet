using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public abstract class InteractableBase : NetworkBehaviour, IInteractable, IUpdateObserver
{
    protected readonly SyncVar<int> _occupantId = new SyncVar<int>(-1);
    protected readonly SyncVar<float> _currentProgress = new SyncVar<float>(0f);

    [SerializeField] protected float _interactionDuration = 7.0f; // N3 초기값

    public float InteractionDuration => _interactionDuration;
    public int OccupantId => _occupantId.Value;

    private float _syncedProgress;
    private float _syncedTime;

    // 서버는 매 프레임 갱신하지만 클라는 SendRate(0.1s) 간격으로만 수신 → 마지막 수신값 기준으로 외삽해 부드럽게 표시
    public float CurrentProgress
    {
        get
        {
            if (IsServerInitialized) return Mathf.Min(1f, _currentProgress.Value);
            if (_occupantId.Value == -1) return 0f;

            float elapsed = Time.time - _syncedTime;
            return Mathf.Clamp01(_syncedProgress + elapsed / _interactionDuration);
        }
    }

    public virtual string ActionGuideText => "상호작용";
    public virtual string ProgressLabelText => "상호작용 중";

    public virtual bool IsInteractable(SurvivorInventory inventory) => true;

    private UpdateDisposer _updateDisposer;

    public override void OnStartClient()
    {
        _updateDisposer = new UpdateDisposer(this);
        _currentProgress.OnChange += OnProgressSynced;
        _occupantId.OnChange += OnOccupantSynced;
    }

    public override void OnStopClient()
    {
        _currentProgress.OnChange -= OnProgressSynced;
        _occupantId.OnChange -= OnOccupantSynced;
    }

    private void OnProgressSynced(float prev, float next, bool asServer)
    {
        _syncedProgress = next;
        _syncedTime = Time.time;
    }

    // 시작 시 진행도가 0→0이면 OnChange가 없어 앵커가 낡은 채로 남음 → 점유자 변경 시점에 앵커를 다시 잡음
    private void OnOccupantSynced(int prev, int next, bool asServer)
    {
        _syncedProgress = 0f;
        _syncedTime = Time.time;
    }

    public void ObserverUpdate(float dt)
    {
        if (!IsServerInitialized || _occupantId.Value == -1) return;

        _currentProgress.Value += dt / _interactionDuration;

        if (_currentProgress.Value >= 1f)
        {
            OnComplete();
        }
    }

    [Server]
    public virtual void RequestStart(int playerId)
    {
        print("RequestStart");
        if (_occupantId.Value != -1) return; // [방지] 동시 시도 충돌
        _occupantId.Value = playerId;
        _currentProgress.Value = 0f;
    }

    [Server]
    public virtual void RequestStop(int playerId)
    {
        if (_occupantId.Value == playerId) ResetInteraction();
    }

    [Server]
    public virtual void OnHitOccupant(int playerId)
    {
        if (_occupantId.Value == playerId) ResetInteraction(); // 피격 시 초기화
    }

    protected virtual void ResetInteraction()
    {
        _occupantId.Value = -1;
        _currentProgress.Value = 0f;
    }

    public Transform GetTransform() => transform;

    protected abstract void OnComplete(); // 상자 A, 탈출구 등에서 개별 구현

    private void OnDestroy()
    {
        _updateDisposer?.Dispose();
    }
}