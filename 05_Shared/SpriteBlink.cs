using UnityEngine;

public class SpriteBlink : MonoBehaviour
{
    [SerializeField] private float blinkDecaySpeed = 10f; // 깜빡임이 원래대로 돌아오는 속도

    private SpriteRenderer[] spriteRenderers;
    private MaterialPropertyBlock propertyBlock;
    private float blinkFactor = 0f;

    private void Start()
    {
        // 자식 오브젝트를 포함하여 모든 SpriteRenderer를 가져옵니다.
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        // 깜빡임 계수가 0 이하라면 아무것도 하지 않습니다.
        if (blinkFactor <= 0f) return;

        // 시간에 따라 blinkFactor를 0으로 감쇠시킵니다.
        blinkFactor = Mathf.Lerp(blinkFactor, 0f, Time.deltaTime * blinkDecaySpeed);

        // 계수가 매우 작아지면 정확히 0으로 설정하여 연산을 멈춥니다 (최적화).
        if (blinkFactor < 0.01f)
        {
            blinkFactor = 0f;
        }

        ApplyBlinkFactor();
    }

    // 피격 시 외부(예: 데미지/넉백 스크립트)에서 호출할 퍼블릭 메서드
    public void Blink()
    {
        blinkFactor = 1f;
        ApplyBlinkFactor();
    }

    // 각 SpriteRenderer의 MaterialPropertyBlock에 blinkFactor를 반영하는 메서드
    private void ApplyBlinkFactor()
    {
        foreach (var renderer in spriteRenderers)
        {
            // 기존 프로퍼티 블록 값을 가져옴
            renderer.GetPropertyBlock(propertyBlock);

            // 셰이더 그래프의 reference 이름(_BlinkFactor)에 값 세팅
            propertyBlock.SetFloat("_BlinkFactor", blinkFactor);

            // 변경된 블록을 렌더러에 다시 적용
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}