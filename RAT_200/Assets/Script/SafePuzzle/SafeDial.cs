using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(PressableButton3D))]
public class SafeDial : MonoBehaviour
{
    [Header("Identity")]
    public CardSuit suit = CardSuit.Spade;

    [Header("Visual Refs")]
    public Transform dialTransform;   // 회전할 캡(없으면 자기 자신)
    public TMP_Text ledText;          // LED 대용 텍스트

    [Header("Motion")]
    public float stepDegrees = 36f;   // 360/10
    public float rotateDuration = 0.08f;
    public Ease rotateEase = Ease.OutCubic;

    [Header("Runtime")]
    [Range(0, 9)] public int value;

    public UnityEvent<int> OnValueChanged;

    Tween _t;
    PressableButton3D _press;

    void Awake()
    {
        if (!dialTransform) dialTransform = transform;
        _press = GetComponent<PressableButton3D>();
        _press.OnPressed.AddListener(Increment);

        // 시작값 0으로 정렬
        ApplyVisual(true);
    }

    public void SetValue(int v, bool instant = false)
    {
        value = Mathf.Clamp(v, 0, 9);
        ApplyVisual(instant);
    }

    public void Increment()
    {
        value = (value + 1) % 10;
        ApplyVisual(false);
        OnValueChanged?.Invoke(value);
    }

    void ApplyVisual(bool instant)
    {
        float targetZ = -stepDegrees * value; // 시계방향(오른쪽으로) 회전
        _t?.Kill();
        if (instant) dialTransform.localRotation = Quaternion.Euler(0, 0, targetZ);
        else _t = dialTransform.DOLocalRotate(new Vector3(0, 0, targetZ), rotateDuration).SetEase(rotateEase);

        if (ledText) ledText.text = value.ToString(); // 나중에 LED로 교체 가능
    }
}
