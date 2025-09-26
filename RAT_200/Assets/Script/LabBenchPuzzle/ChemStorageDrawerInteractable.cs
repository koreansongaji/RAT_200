using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class ChemStorageDrawerInteractable : BaseInteractable
{
    [Header("Item IDs (지급할 재료)")]
    [SerializeField] string sodiumId = "Sodium";
    [SerializeField] string gelId = "Gel";

    [Header("Drawer (열림 연출)")]
    [SerializeField] Transform drawer;          // 서랍 트랜스폼(로컬 기준)
    [SerializeField] Vector3 openLocalOffset = new Vector3(0f, 0f, -0.3f);
    [SerializeField] float duration = 0.5f;
    [SerializeField] Ease ease = Ease.InOutSine;
    [SerializeField] bool openOnlyOnce = true;
    [SerializeField] bool disableColliderOnOpen = false;

    [Header("이벤트/사운드 등")]
    public UnityEvent OnOpen;    // 열릴 때 호출
    public UnityEvent OnGive;    // 아이템 지급 시 호출

    // 내부 상태
    bool _opened;
    Vector3 _startLocalPos;
    Collider _col;
    Tween _tOpen;

    void Awake()
    {
        if (!drawer) drawer = transform; // 안전장치: drawer 미지정 시 자기 자신
        _startLocalPos = drawer.localPosition;
        _col = GetComponent<Collider>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (!i) return false;
        if (openOnlyOnce && _opened) return false;   // 한 번만 열도록
        return true;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;

        // 열림 연출
        _tOpen?.Kill();
        _tOpen = drawer.DOLocalMove(_startLocalPos + openLocalOffset, duration).SetEase(ease);

        _opened = true;
        OnOpen?.Invoke();

        // 즉시 아이템 지급(소모/카운트 없음: 보유 플래그만 ON)
        i.AddItem(sodiumId);
        i.AddItem(gelId);
        OnGive?.Invoke();

        Debug.Log("[ChemStorageDrawer] Sodium + Gel 지급 완료");

        if (disableColliderOnOpen && _col) _col.enabled = false;
    }

    // (선택) 에디터/디버그용으로 리셋
    [ContextMenu("Reset Drawer (Editor)")]
    void ResetDrawer()
    {
        _tOpen?.Kill();
        drawer.localPosition = _startLocalPos;
        _opened = false;
        if (_col) _col.enabled = true;
    }
}
