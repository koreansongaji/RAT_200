using UnityEngine;
using DG.Tweening;
using TMPro;

public enum CardSuit { Spade, Heart, Diamond, Club }

[RequireComponent(typeof(Collider))]
public class SafeDialInteractable : BaseInteractable
{
    [Header("Dial")]
    public Transform dialRoot;                 // ȸ���� �θ�(����)
    [Range(0.01f, 1f)] public float tweenSec = 0.08f;
    public Ease ease = Ease.OutQuad;
    public CardSuit suit;

    [Header("Display (optional)")]
    public TMP_Text ledText;                   // LED ��� TMP �ؽ�Ʈ ��� ��
    
    [Header("Micro Gate")]
    public bool requireMicroZoom = true;
    public MicroZoomSession micro;             // ��������� �θ𿡼� ã��

    public int CurrentValue { get; private set; } = 0;
    Tween _t;
    Quaternion _baseLocalRotation;

    [SerializeField] private AudioClip _safeDial;
    
    void Awake()
    {
        if (!dialRoot) dialRoot = transform;
        if (!micro) micro = GetComponentInParent<MicroZoomSession>();
        _baseLocalRotation = dialRoot.localRotation;
        UpdateLed();
        
        if(_safeDial != null) _safeDial = Resources.Load<AudioClip>("Sounds/Effect/Safe/safe_dial");
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
        AudioManager.Instance.Play(_safeDial, AudioManager.Sound.Effect, Random.Range(0.9f, 1.1f));
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
        // ���� ��Ʈ�ѷ��� ������ ��ȣ
        //var ctrl = GetComponentInParent<SafePuzzleController>();
        //if (ctrl) ctrl.OnDialChanged(this);
    }

    void RotateTo(int value, bool instant)
    {
        float targetX = 36f * value;

        _t?.Kill();

        // ���� ���� ȸ��
        Quaternion baseRot = _baseLocalRotation;

        // ���� X���� �������� ȸ�� (�� transform�� localAxis ���)
        Quaternion xRot = Quaternion.AngleAxis(targetX, Vector3.down);

        Quaternion finalRot = baseRot * xRot;

        if (instant)
        {
            dialRoot.localRotation = finalRot;
        }
        else
        {
            _t = dialRoot.DOLocalRotateQuaternion(finalRot, tweenSec)
                         .SetEase(ease);
        }
    }





    void UpdateLed()
    {
        if (ledText) ledText.text = CurrentValue.ToString();
    }
}
