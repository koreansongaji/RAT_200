using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class PressableButton3D : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("버튼 캡(눌려 움직일 파트). 지정 없으면 자기 자신.")]
    public Transform cap;
    [Tooltip("아래로 내려갈 오프셋(로컬).")]
    public Vector3 downLocalOffset = new(0f, -0.012f, 0f);
    public float tweenDuration = 0.06f;
    public Ease tweenEase = Ease.OutQuad;

    [Header("FX (선택)")]
    public AudioSource sfxSource;
    public AudioClip sfxClick;
    public Renderer indicatorRenderer; // 색 변화용(선택)
    public Color normalColor = Color.white;
    public Color disabledColor = Color.gray;

    [Header("Interactable")]
    public bool interactable = true;

    [Header("Micro Gate")]
    public bool requireMicroZoom = true;                // ← 기본 true로 두면 Micro에서만 작동
    public MicroZoomSession micro;                      // 비워두면 부모에서 자동 탐색
    [SerializeField] bool autoToggleCollider = true;    // Micro 아니면 콜라이더 자동 Off
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
            // 클릭 완료로 간주
            OnPressed?.Invoke();
            if (sfxSource && sfxClick) sfxSource.PlayOneShot(sfxClick);
        }
        PressVisual(false);
    }

    void OnMouseExit()
    {
        // 누른 채로 바깥으로 나가면 복귀
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
