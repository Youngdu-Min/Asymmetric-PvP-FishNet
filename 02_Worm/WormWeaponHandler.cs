using FishNet.Object;
using UnityEngine;

public class WormWeaponHandler : NetworkBehaviour, IFixedUpdateObserver
{
    [SerializeField] private RaycastChecker _dashRaycastHit;
    [SerializeField] private RaycastChecker _contactRaycastHit;

    private MonsterConfig _config;
    private WormFSM _fsm;
    private WormLocomotion _locomotion;
    private EntityHealth _health;
    private WormContactDamage _contactDamage;

    // 타이머 관리
    private CountdownTimer _dashPrepareTimer;
    private CountdownTimer _dashProgressTimer;
    private CountdownTimer _dashStunTimer;
    private CountdownTimer _dashCooldownTimer;

    private bool _isDashCooldown;
    private Vector2 _fixedDashDirection;
    private FixedUpdateDisposer _dashFixedUpdateDisposer;

    private MonsterStatOverride _override;

    private int ContactDamage => _override.Enabled ? _override.ContactDamage : _config.ContactDamage;
    public float BulletSlowRate => _override.Enabled ? _override.BulletSlowRate : _config.BulletSlowRate;
    public float BulletSlowDuration => _override.Enabled ? _override.BulletSlowDuration : _config.BulletSlowDuration;
    private int DashDamage => _override.Enabled ? _override.DashDamage : _config.DashDamage;
    private float DashSpeed => _override.Enabled ? _override.DashSpeed : _config.DashSpeed;
    private float DashDistance => _override.Enabled ? _override.DashDistance : _config.DashDistance;
    private float DashPrepareDuration => _override.Enabled ? _override.DashPrepareDuration : _config.DashPrepareDuration;
    private float DashStunDuration => _override.Enabled ? _override.DashStunDuration : _config.DashStunDuration;
    private float DashCooldown => _override.Enabled ? _override.DashCooldown : _config.DashCooldown;

    public void ApplyOverride(MonsterStatOverride statOverride) => _override = statOverride; // 디버깅 전용

    public bool IsDashReady => !_isDashCooldown;
    public float DashCooldownProgress => _isDashCooldown ? 1f - _dashCooldownTimer.Remain : 1f;

    // 디버깅 전용: 현재 유효 스탯(오버라이드 반영) 조회
    public MonsterStatOverride CurrentStats => new MonsterStatOverride
    {
        Enabled = true,
        ContactDamage = ContactDamage,
        BulletSlowRate = BulletSlowRate,
        BulletSlowDuration = BulletSlowDuration,
        DashDamage = DashDamage,
        DashSpeed = DashSpeed,
        DashDistance = DashDistance,
        DashPrepareDuration = DashPrepareDuration,
        DashStunDuration = DashStunDuration,
        DashCooldown = DashCooldown,
    };

    public void Initialize(MonsterConfig config, WormFSM fsm, WormLocomotion locomotion, EntityHealth health)
    {
        _config = config;
        _fsm = fsm;
        _locomotion = locomotion;
        _health = health;

        // 접촉 피해도 웜의 공격 수단 → 돌진과 함께 무기 핸들러가 소유. 피해량은 오버라이드 반영값을 실시간 조회
        _contactDamage = new WormContactDamage();
        _contactDamage.Initialize(_contactRaycastHit, _health, _fsm, () => ContactDamage);

        // 돌진 타이머 시퀀스 설정 (준비 0.2s -> 진행 -> 경직 0.15s -> 쿨다운 4.0s)
        _dashPrepareTimer = new CountdownTimer(config.DashPrepareDuration);
        _dashPrepareTimer.OnTimerStop += StartDashProgress;

        // 돌진 진행 최대 지속 시간 계산 (돌진거리 / 속도)
        float maxDashDuration = config.DashDistance / config.DashSpeed;
        _dashProgressTimer = new CountdownTimer(maxDashDuration);
        _dashProgressTimer.OnTimerStop += EndDashProgress;

        _dashStunTimer = new CountdownTimer(config.DashStunDuration);
        _dashStunTimer.OnTimerStop += ReleaseDashStun;

        _dashCooldownTimer = new CountdownTimer(config.DashCooldown);
        _dashCooldownTimer.OnTimerStop += () => _isDashCooldown = false;

        _dashRaycastHit.OnCollEnter += OnDashColliderEnter;
        _dashRaycastHit.SetActive(false);
    }

    public bool TryExecuteDash()
    {
        if (_isDashCooldown) return false;
        if (!IsOwner) return false;

        _locomotion.UpdateAim();
        _isDashCooldown = true;
        _dashCooldownTimer.Reset(DashCooldown);
        _dashCooldownTimer.Start();

        // 입력 시점 방향 고정
        _fixedDashDirection = _locomotion.AimWorldDir;

        _fsm.TransitionTo(WormState.DashPrepare);
        _dashPrepareTimer.Reset(DashPrepareDuration);
        _dashPrepareTimer.Start();
        CmdNotifyDash(_fixedDashDirection);
        return true;
    }

    [ServerRpc]
    private void CmdNotifyDash(Vector2 aimDir)
    {
        if (IsOwner) return; // 호스트 플레이어라면 중복 실행 방지

        _fixedDashDirection = aimDir;
        _fsm.TransitionTo(WormState.DashPrepare);
        _dashPrepareTimer.Reset(_config.DashPrepareDuration);
        _dashPrepareTimer.Start();
    }

    private void StartDashProgress()
    {
        _fsm.TransitionTo(WormState.DashProgress);

        if (IsServerInitialized) SetActiveDashAttack(true);

        // 이동은 오너(다이내믹 바디)만 구동 — 물리 스텝 동안만 구독
        if (IsOwner) _dashFixedUpdateDisposer = new FixedUpdateDisposer(this);

        _dashProgressTimer.Reset(DashDistance / Mathf.Max(0.1f, DashSpeed)); // 거리/속도 반영, 0 division 가드
        _dashProgressTimer.Start();
    }

    public void SetActiveDashAttack(bool isTrue)
    {
        _dashRaycastHit?.SetActive(isTrue);
    }

    private void OnDashColliderEnter(Collider2D coll)
    {
        print("dash");
        if (!coll.transform.root.TryGetComponent(out ITakeDamage takeDamage)) { return; }
        takeDamage.TakeDamage(DashDamage);
    }

    private void EndDashProgress()
    {
        if (IsServerInitialized) SetActiveDashAttack(false);

        _dashFixedUpdateDisposer?.Dispose();
        _dashFixedUpdateDisposer = null;

        _locomotion.Stop();
        _fsm.TransitionTo(WormState.DashStun);
        _dashStunTimer.Reset(DashStunDuration);
        _dashStunTimer.Start();
    }

    private void ReleaseDashStun()
    {
        _fsm.TransitionTo(WormState.Idle);
    }

    public void ObserverFixedUpdate(float fixedDt)
    {
        if (_fsm.Current != WormState.DashProgress) return;

        // 고정 방향으로 물리 이동 직접 제어 (속도 12.0)
        _locomotion.SetVelocity(_fixedDashDirection * DashSpeed);
    }

    public void Cleanup()
    {
        _contactDamage?.Cleanup();

        if (_dashRaycastHit != null) _dashRaycastHit.OnCollEnter -= OnDashColliderEnter;

        _dashFixedUpdateDisposer?.Dispose();
        _dashFixedUpdateDisposer = null;

        _dashPrepareTimer?.Dispose();
        _dashProgressTimer?.Dispose();
        _dashStunTimer?.Dispose();
        _dashCooldownTimer?.Dispose();
    }
}