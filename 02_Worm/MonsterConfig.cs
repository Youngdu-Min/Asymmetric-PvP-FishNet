using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterConfig", menuName = "Config/MonsterConfig")]
public class MonsterConfig : CharacterConfig
{
    [BoxGroup("부활")]
    [LabelText("부활 대기 시간 (초)"), MinValue(0.1f)]
    [SerializeField] private float _respawnTime = 10.0f;

    [BoxGroup("탄환 피격 감속")]
    [LabelText("이동 속도 감소율 (0~1)"), Range(0f, 1f)]
    [SerializeField] private float _bulletSlowRate = 0.30f;

    [BoxGroup("탄환 피격 감속")]
    [LabelText("이동 속도 감소 시간 (초)"), MinValue(0f)]
    [SerializeField] private float _bulletSlowDuration = 1.50f;

    [BoxGroup("접촉 피해")]
    [LabelText("접촉 피해량"), MinValue(0)]
    [SerializeField] private int _contactDamage = 30;

    [BoxGroup("스킬 - 돌진")]
    [BoxGroup("스킬 - 돌진/피해")]
    [LabelText("돌진 피해량")]
    [SerializeField] private int _dashDamage = 50;

    [BoxGroup("스킬 - 돌진/이동")]
    [LabelText("돌진 속도")]
    [SerializeField] private float _dashSpeed = 12.0f;

    [BoxGroup("스킬 - 돌진/이동")]
    [LabelText("돌진 최대 거리")]
    [SerializeField] private float _dashDistance = 4.5f;

    [BoxGroup("스킬 - 돌진/타이밍")]
    [LabelText("돌진 준비 시간 (초)")]
    [SerializeField] private float _dashPrepareDuration = 0.20f;

    [BoxGroup("스킬 - 돌진/타이밍")]
    [LabelText("종료 후 경직 시간 (초)")]
    [SerializeField] private float _dashStunDuration = 0.15f;

    [BoxGroup("스킬 - 돌진/타이밍")]
    [LabelText("재사용 대기 시간 (초)")]
    [SerializeField] private float _dashCooldown = 4.00f;

    [BoxGroup("감지 및 추적")]
    [LabelText("생존자 레이어")]
    [SerializeField] private LayerMask m_survivorLayer;
    [BoxGroup("감지 및 추적/근접 감지")]
    [LabelText("근접 감지 반지름 (타일)"), MinValue(0f)]
    [SerializeField] private float m_proximityRadius = 6.0f;

    [BoxGroup("감지 및 추적/근접 감지")]
    [LabelText("근접 감지 갱신 주기 (초)"), MinValue(0.01f)]
    [SerializeField] private float m_proximityUpdateInterval = 0.25f;

    [BoxGroup("감지 및 추적/근접 감지")]
    [LabelText("근접 감지 해제 지연 (초)"), MinValue(0f)]
    [SerializeField] private float m_proximityReleaseDelay = 0.50f;

    [BoxGroup("감지 및 추적/위치 추적")]
    [LabelText("위치 추적 지속 시간 (초)"), MinValue(0f)]
    [SerializeField] private float m_pathDuration = 4.00f;

    [BoxGroup("감지 및 추적/위치 추적")]
    [LabelText("경로 갱신 주기 (초)"), MinValue(0.01f)]
    [SerializeField] private float m_pathUpdateInterval = 0.25f;

    [BoxGroup("감지 및 추적/위치 추적")]
    [LabelText("최대 동시 추적 경로 수"), MinValue(1)]
    [SerializeField] private int m_maxTrackCount = 3;

    public float RespawnTime => _respawnTime;

    public int ContactDamage => _contactDamage;

    public float BulletSlowRate => _bulletSlowRate;
    public float BulletSlowDuration => _bulletSlowDuration;

    public int DashDamage => _dashDamage;
    public float DashSpeed => _dashSpeed;
    public float DashDistance => _dashDistance;
    public float DashPrepareDuration => _dashPrepareDuration;
    public float DashStunDuration => _dashStunDuration;
    public float DashCooldown => _dashCooldown;

    public LayerMask SurvivorLayer => m_survivorLayer;
    public float ProximityRadius => m_proximityRadius;
    public float ProximityUpdateInterval => m_proximityUpdateInterval;
    public float ProximityReleaseDelay => m_proximityReleaseDelay;

    public float PathDuration => m_pathDuration;
    public float PathUpdateInterval => m_pathUpdateInterval;
    public int MaxTrackCount => m_maxTrackCount;
}