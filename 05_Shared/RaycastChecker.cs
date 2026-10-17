using FishNet.Object;
using System;
using UnityEngine;

public class RaycastChecker : NetworkBehaviour
{
    public enum CastPreset
    {
        Ray,        // 선 (단방향 레이)
        Circle,     // 원형 영역 (Overlap)
        Box         // 사각형 영역 (Overlap)
    }

    public event Action<Collider2D> OnCollEnter;

    [Header("Cast Settings")]
    [SerializeField] private CastPreset _preset = CastPreset.Ray;
    [SerializeField] private LayerMask _targetLayer;

    [Tooltip("Ray: Y=길이\nCircle: X=반지름\nBox: X,Y=크기")]
    [SerializeField] private Vector2 _size = Vector2.one;

    private CountdownTimer _activeTimer;
    private bool _initialized;
    private bool _isActive;

    // 가독성을 위한 프롭화
    private float RayDistance => _size.y;
    private float CircleRadius => _size.x;

    public void SetActive(bool isTrue)
    {
        EnsureInitialized();
        print(isTrue);
        _isActive = isTrue;
        if (!_isActive) _activeTimer?.Stop();
    }

    public void ActiveDuringTime(float duration)
    {
        EnsureInitialized();
        _activeTimer.Reset(duration);
        _activeTimer.Start();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _activeTimer = new CountdownTimer(0);
        _activeTimer.OnTimerStart += () => _isActive = true;
        _activeTimer.OnTimerStop += () => _isActive = false;
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized) return;
        if (!_isActive) return;

        Vector2 origin = transform.position;
        float currentAngle = transform.eulerAngles.z;
        Vector2 worldDirection = transform.right;

        switch (_preset)
        {
            case CastPreset.Ray:
                RaycastHit2D hit = Physics2D.Raycast(origin, worldDirection, RayDistance, _targetLayer);
                if (hit.collider != null) OnCollEnter?.Invoke(hit.collider);
                break;

            case CastPreset.Circle:
                Collider2D circleCol = Physics2D.OverlapCircle(origin, CircleRadius, _targetLayer);
                if (circleCol != null) OnCollEnter?.Invoke(circleCol);
                break;

            case CastPreset.Box:
                Collider2D boxCol = Physics2D.OverlapBox(origin, _size, currentAngle, _targetLayer);
                if (boxCol != null) OnCollEnter?.Invoke(boxCol);
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 origin = transform.position;
        Vector2 direction = transform.right;
        float currentAngle = transform.eulerAngles.z;

        switch (_preset)
        {
            case CastPreset.Ray:
                Gizmos.DrawLine(origin, origin + (direction * RayDistance));
                break;

            case CastPreset.Circle:
                Gizmos.DrawWireSphere(origin, CircleRadius);
                break;

            case CastPreset.Box:
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(origin, Quaternion.Euler(0, 0, currentAngle), Vector3.one);

                Gizmos.DrawWireCube(Vector3.zero, new Vector3(_size.x, _size.y, 0));
                Gizmos.matrix = oldMatrix;
                break;
        }
    }

    private void OnDestroy()
    {
        _activeTimer?.Dispose();
    }
}