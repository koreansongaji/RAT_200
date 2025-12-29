using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class PressableButton3D : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("��ư ĸ(���� ������ ��Ʈ). ���� ������ �ڱ� �ڽ�.")]
    public Transform cap;
    [Tooltip("�Ʒ��� ������ ������(����).")]
    public Vector3 downLocalOffset = new(0f, -0.012f, 0f);
    public float tweenDuration = 0.06f;
    public Ease tweenEase = Ease.OutQuad;

    [Header("FX (����)")]
    public AudioSource sfxSource;
    public AudioClip sfxClick;
    public Renderer indicatorRenderer; // �� ��ȭ��(����)
    public Color normalColor = Color.white;
    public Color disabledColor = Color.gray;

    [Header("Interactable")]
    public bool interactable = true;

    [Header("Micro Gate")]
    public bool requireMicroZoom = true;                // �� �⺻ true�� �θ� Micro������ �۵�
    public MicroZoomSession micro;                      // ����θ� �θ𿡼� �ڵ� Ž��
    [SerializeField] bool autoToggleCollider = true;    // Micro �ƴϸ� �ݶ��̴� �ڵ� Off
    Collider _col;

    public UnityEvent OnPressed;

    Vector3 _startLocalPos;
    Tween _t;
    bool _isDown;

    void Awake()
    {
        if (!cap) cap = transform;
        _startLocalPos = cap.localPosition;
        ApplyVisual();
        _col = GetComponent<Collider>();
        if (!micro) micro = GetComponentInParent<MicroZoomSession>();
    }
    void OnEnable()
    {
        if (micro)
        {
            micro.OnEnterMicro.AddListener(OnEnterMicro);
            micro.OnExitMicro.AddListener(OnExitMicro);
        }
        UpdateColliderByMicro();
    }

    void OnDisable()
    {
        _t?.Kill();
        if (cap) cap.localPosition = _startLocalPos;
        _isDown = false;
        if (micro)
        {
            micro.OnEnterMicro.RemoveListener(OnEnterMicro);
            micro.OnExitMicro.RemoveListener(OnExitMicro);
        }
    }
    bool PassesMicroGate()
    {
        if (!requireMicroZoom) return true;
        if (!micro) return false;
        return micro.InMicro;
    }

    void UpdateColliderByMicro()
    {
        if (!autoToggleCollider || _col == null) return;
        _col.enabled = PassesMicroGate();
    }

    void OnEnterMicro() => UpdateColliderByMicro();
    void OnExitMicro() => UpdateColliderByMicro();

    public void SetInteractable(bool on)
    {
        interactable = on;
        ApplyVisual();
    }

    void ApplyVisual()
    {
        if (indicatorRenderer)
        {
            var mat = indicatorRenderer.material;
            var col = interactable ? normalColor : disabledColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            else if (mat.HasProperty("_Color")) mat.color = col;
        }
    }

    void OnMouseDown()
    {
        if (!interactable) return;
        if (!PassesMicroGate()) return;
        PressVisual(true);
    }

    void OnMouseUp()
    {
        if (!interactable) return;
        if (!PassesMicroGate()) { PressVisual(false); return; }
        if (_isDown)
        {
            // Ŭ�� �Ϸ�� ����
            OnPressed?.Invoke();
            if (sfxSource && sfxClick) sfxSource.PlayOneShot(sfxClick);

            // 공용 버튼 클릭 사운드 재생 (있다면)
            CommonSoundController.Instance?.PlayButton();
        }
        PressVisual(false);
    }

    void OnMouseExit()
    {
        // ���� ä�� �ٱ����� ������ ����
        if (_isDown) PressVisual(false);
    }

    void PressVisual(bool down)
    {
        _isDown = down;
        _t?.Kill();
        var target = down ? _startLocalPos + downLocalOffset : _startLocalPos;
        _t = cap.DOLocalMove(target, tweenDuration).SetEase(tweenEase);
    }
}
