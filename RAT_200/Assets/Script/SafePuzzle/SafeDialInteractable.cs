using UnityEngine;
using DG.Tweening;
using TMPro;

public enum CardSuit { Spade, Heart, Diamond, Club }

[RequireComponent(typeof(Collider))]
public class SafeDialInteractable : BaseInteractable
{
    [Header("Dial")]
    public Transform dialRoot;                 // 회전할 부모(원통)
    [Range(0.01f, 1f)] public float tweenSec = 0.08f;
    public Ease ease = Ease.OutQuad;
    public CardSuit suit;

    [Header("Display (optional)")]
    public TMP_Text ledText;                   // LED 대신 TMP 텍스트 사용 시

    [Header("Micro Gate")]
    public bool requireMicroZoom = true;
    public MicroZoomSession micro;             // 비어있으면 부모에서 찾음

    public int CurrentValue { get; private set; } = 0;
    Tween _t;

    void Awake()
    {
        if (!dialRoot) dialRoot = transform;
        if (!micro) micro = GetComponentInParent<MicroZoomSession>();
        UpdateLed();
    }

    bool PassesMicroGate()
    {
        if (!requireMicroZoom) return true;
        if (!micro) return false;
        return micro.InMicro;
    }

    public override bool CanInteract(PlayerInteractor i) => PassesMicroGate();

    public override void Interact(PlayerInteractor i)
    {
        if (!PassesMicroGate()) return;
        Step(+1);
    }

    public void SetValue(int v, bool instant = false)
    {
        v = ((v % 10) + 10) % 10;
        CurrentValue = v;
        RotateTo(CurrentValue, instant);
        UpdateLed();
    }

    public void Step(int delta)
    {
        SetValue(CurrentValue + delta, instant: false);
        // 퍼즐 컨트롤러가 있으면 신호
        //var ctrl = GetComponentInParent<SafePuzzleController>();
        //if (ctrl) ctrl.OnDialChanged(this);
    }

    void RotateTo(int value, bool instant)
    {
        float targetX = 36f * value; // 시계 방향 회전을 X축 +방향으로
        _t?.Kill();
        if (instant)
            dialRoot.localRotation = Quaternion.Euler(targetX, 0, 90);
        else
            _t = dialRoot.DOLocalRotate(new Vector3(targetX, 0, 90), tweenSec).SetEase(ease);
    }

    void UpdateLed()
    {
        if (ledText) ledText.text = CurrentValue.ToString();
    }
}
