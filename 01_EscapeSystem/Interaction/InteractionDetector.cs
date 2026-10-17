using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    [SerializeField] private float _detectRange = 2.0f; // 조작 및 튜닝 기준 반영
    [SerializeField] private LayerMask _interactableLayer;

    private IInteractable _nearestTarget;

    public IInteractable GetNearestTarget()
    {
        // 주변 IInteractable 레이어를 가진 콜라이더 검색
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _detectRange, _interactableLayer);

        float minDistance = float.MaxValue;
        IInteractable bestTarget = null;

        foreach (var col in hitColliders)
        {
            if (col.TryGetComponent(out IInteractable interactable))
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestTarget = interactable;
                }
            }
        }
        return bestTarget;
    }
}