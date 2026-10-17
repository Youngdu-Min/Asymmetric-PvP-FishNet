using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

public class Worm : NetworkBehaviour, IUpdateObserver
{
    [Title("Managers")]
    [SerializeField] private InputHandler _inputHandler; // 공통 InputHandler 사용 가정
    [SerializeField] private CameraTargetChannel _cameraChannel;

    [Title("Configs")]
    [SerializeField] private MonsterConfig _monsterConfig; // 기획서의 수치 컨피그 구조체
    [SerializeField] private float _cameraYOffset = 0.2f;

    [Title("Components")]
    [SerializeField] private NetworkAnimator _animator;
    [SerializeField] private WormFSM _fsm;
    [SerializeField] private WormNetworkHandler _networkHandler;
    [SerializeField] private EntityHealth _health;
    [SerializeField] private WormWeaponHandler _weapon;
    [SerializeField] private EntityPathRevealController _pathTracker;
    [SerializeField] private EntityProximitySensor _proximityDetector;
    [SerializeField] private EntityPathRenderer _pathRenderer;
    [SerializeField] private Transform _bodyRootTransform;
    [SerializeField] private SpriteBlink _blink;

    private WormLocomotion _locomotion;
    private WormAnimationHandler _animationHandler; // 8방향 스프라이트 제어용
    private UpdateDisposer _updateDisposer;

    // 몸체 회전(바라보는 방향)을 서버·전 클라에 동기화 → 서버 접촉/돌진 Box 판정 각도 정합
    private readonly SyncVar<DirectionResolver.EightDir> _facing = new SyncVar<DirectionResolver.EightDir>(DirectionResolver.EightDir.Down);

    public MonsterConfig Config => _monsterConfig;
    public float CurrentMoveSpeed => _locomotion != null ? _locomotion.MoveSpeed : _monsterConfig.MoveSpeed;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _health.ServerInitializeHp(_monsterConfig.MaxHealth);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Initialize();
        if (!IsOwner) return;
        InitializeClient();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (!IsOwner) return;
        Cleanup();
    }

    private void Initialize()
    {
        _locomotion = new WormLocomotion();
        _animationHandler = new WormAnimationHandler(_animator);
        _animationHandler.OnDirChanged += OnOwnerDirChanged;
        _facing.OnChange += OnFacingSynced;
        SetColliderRotation(_facing.Value); // 스폰 시 이미 동기화된 방향 반영

        var rb = GetComponent<Rigidbody2D>();
        if (!IsOwner) rb.bodyType = RigidbodyType2D.Kinematic;

        // 기획 수치 반영 초기화 (최대 체력 250)
        _health.Initialize(_networkHandler, _monsterConfig.MaxHealth, _monsterConfig.RespawnTime);
        _weapon.Initialize(_monsterConfig, _fsm, _locomotion, _health);
        _locomotion.Initialize(rb, _fsm, _monsterConfig.MoveSpeed, transform);
        _fsm.Initialize(_networkHandler, _locomotion);

        // PathTracker 및 ProximityDetector 의존성 주입
        _pathRenderer?.Initialize(_monsterConfig);
        _pathTracker?.Initialize(_monsterConfig, _fsm, _pathRenderer);
        _proximityDetector?.Initialize(_monsterConfig, _fsm);

        _health.OnHit += OnHitReceived;
        _health.OnDead += OnDeadReceived;
        _health.OnRespawned += OnRespawnedReceived;
    }

    // 오너가 방향 전환 시: 즉시 로컬 반영 + 서버로 전파(서버가 SyncVar로 전 클라 동기화)
    private void OnOwnerDirChanged(DirectionResolver.EightDir dir)
    {
        SetColliderRotation(dir);
        if (IsServerInitialized) _facing.Value = dir;
        else CmdSetFacing(dir);
    }

    [ServerRpc]
    private void CmdSetFacing(DirectionResolver.EightDir dir) => _facing.Value = dir;

    private void OnFacingSynced(DirectionResolver.EightDir prev, DirectionResolver.EightDir next, bool asServer)
        => SetColliderRotation(next);

    private void SetColliderRotation(DirectionResolver.EightDir dir)
    {
        // 8방향에 따른 콜라이더 회전값 설정
        float angle = dir switch
        {
            DirectionResolver.EightDir.Up => 0f,
            DirectionResolver.EightDir.UpRight => 315f,
            DirectionResolver.EightDir.Right => 270f,
            DirectionResolver.EightDir.DownRight => 225f,
            DirectionResolver.EightDir.Down => 180f,
            DirectionResolver.EightDir.DownLeft => 135f,
            DirectionResolver.EightDir.Left => 90f,
            DirectionResolver.EightDir.UpLeft => 45f,
            _ => 0f,
        };
        _bodyRootTransform.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void InitializeClient()
    {
        _inputHandler.Init();
        _inputHandler.CurrentInputState = InputHandler.InputState.Monster; // 웜 조작계로 전환

        // 입력 이벤트 구독
        _inputHandler.onMoveInput += _locomotion.OnMoveInput;
        _inputHandler.onAimInput += _locomotion.OnAimInput;
        _inputHandler.onSpecialInput += OnDashRequested;   // 마우스 우클릭 돌진

        _cameraChannel.Raise(transform, _cameraYOffset);
        _updateDisposer = new UpdateDisposer(this);
    }

    public void ObserverUpdate(float dt)
    {
        if (!_networkHandler.IsLocalOwner) return;

        _fsm.Tick(_locomotion.MoveInput);

        _locomotion.ApplyMovement();

        bool isMoving = _fsm.Current == WormState.Move;
        _animationHandler.SetMoving(isMoving);
        // 돌진 중일 때는 시작 시점의 방향 유지 규칙 반영
        if (_fsm.Current != WormState.DashProgress && _fsm.Current != WormState.DashStun)
            _animationHandler.SetDirection(_locomotion.MoveEightDir);
    }

    private void OnHitReceived()
    {
        _blink.Blink();
        // 생존자 탄환 피해 적용 시 기본 이동 속도 일시 감소 (이동 구동 주체인 오너에만 적용, 오버라이드 반영)
        if (IsOwner)
            _locomotion.ApplySlow(_weapon.BulletSlowRate, _weapon.BulletSlowDuration);
    }

    private void OnDashRequested()
    {
        if (_fsm.IsDashBlocked) return;

        if (_weapon.TryExecuteDash())
        {
            _animationHandler.AnimateOneShot("Special");
        }
        print("Dash Requested");
    }

    private void OnDeadReceived()
    {
        _animationHandler.AnimateOneShot("Knock");
        _fsm.ForceTransitionTo(WormState.Dead);
        _locomotion.Stop();
        _locomotion.ClearSlow(); // 무력화 시 감속 효과 해제
    }

    private void OnRespawnedReceived()
    {
        _animationHandler.AnimateOneShot("KnockEnd");
        _fsm.ForceTransitionTo(WormState.Idle);
        _locomotion.ClearSlow(); // 부활 시 감속 효과 해제
    }

    private void Cleanup()
    {
        _cameraChannel?.Unregister();
        _animationHandler.OnDirChanged -= OnOwnerDirChanged;
        _facing.OnChange -= OnFacingSynced;
        _inputHandler.onMoveInput -= _locomotion.OnMoveInput;
        _inputHandler.onAimInput -= _locomotion.OnAimInput;
        _inputHandler.onSpecialInput -= OnDashRequested;

        _health.OnHit -= OnHitReceived;
        _health.OnDead -= OnDeadReceived;
        _health.OnRespawned -= OnRespawnedReceived;

        _pathTracker?.Cleanup();
        _proximityDetector?.Cleanup();
        _weapon.Cleanup();
        _locomotion.Dispose();
        _updateDisposer?.Dispose();
    }

    // 디버깅 전용 훅
    public void ApplyMoveSpeed(float moveSpeed) => _locomotion.SetMoveSpeed(moveSpeed);
}