using UnityEngine;
using DG.Tweening; // DOTween을 쓰고 계시니 부드럽게 바뀌도록 만들었습니다!

public class SimpleColorChanger : MonoBehaviour
{
    [Header("대상")]
    public SpriteRenderer targetRenderer;

    [Header("색상 설정")]
    public Color normalColor = Color.white; // 원래 색 (보통 흰색)
    public Color activeColor = Color.red;   // 바꿀 색 (예: 빨강)

    [Header("설정")]
    public float duration = 0.3f; // 바뀌는 시간 (0이면 즉시 변경)

    private void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<SpriteRenderer>();
    }

    // ★ UnityEvent에 등록할 함수 1: "활성 색(빨강)으로 변경해라"
    public void ChangeToActive()
    {
        if (!targetRenderer) return;
        targetRenderer.DOKill();
        // duration이 0보다 크면 부드럽게, 아니면 즉시 변경
        if (duration > 0) targetRenderer.DOColor(activeColor, duration);
        else targetRenderer.color = activeColor;
    }

    // ★ UnityEvent에 등록할 함수 2: "원래 색(흰색)으로 돌아와라"
    public void ChangeToNormal()
    {
        if (!targetRenderer) return;
        targetRenderer.DOKill();
        if (duration > 0) targetRenderer.DOColor(normalColor, duration);
        else targetRenderer.color = normalColor;
    }
}