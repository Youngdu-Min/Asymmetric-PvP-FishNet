using UnityEngine;

[CreateAssetMenu(fileName = "EntityDetectionConfig", menuName = "Config/EntityDetection")]
public class EntityDetectionConfig : ScriptableObject
{
    [Header("근접 감지 수치")]
    public float ProximityRadius = 6.0f;       // 감지 반지름 (6 타일)
    public float ProximityUpdateInterval = 0.25f; // 감지 갱신 주기
    public float ProximityReleaseDelay = 0.50f;   // 감지 해제 지연

    [Header("위치 추적 경로 수치")]
    public float PathDuration = 4.00f;         // 추적 지속 시간
    public float PathUpdateInterval = 0.25f;   // 경로 갱신 주기
    public int MaxTrackCount = 3;              // 동시 표시 가능 경로 수
}