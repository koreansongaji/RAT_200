using UnityEngine;
using UnityEngine.Events;

public class ChemStorageDrawerInteractable : BaseInteractable
{
    [Header("Item IDs (지급할 재료)")]
    [SerializeField] string sodiumId = "Sodium";
    [SerializeField] string gelId = "Gel";

    [Header("설정")]
    [SerializeField] bool giveOnlyOnce = true;          // 한 번만 지급할지
    [SerializeField] bool disableColliderAfterGive = false; // 지급 후 상호작용 막기

    [Header("이벤트/사운드 등")]
    [Tooltip("클릭해서 상호작용할 때(지급 직전) 호출")]
    public UnityEvent OnInteract;
    [Tooltip("아이템 지급이 완료되었을 때 호출")]
    public UnityEvent OnGive;

    bool _given;           // 이미 지급했는지 여부
    Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (!i) return false;
        if (giveOnlyOnce && _given) return false;
        return true; // 필요하면 base.CanInteract(i) && ... 로 바꿔도 됨
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;

        OnInteract?.Invoke();

        // 아이템 지급
        i.AddItem(sodiumId, transform.position);
        i.AddItem(gelId, transform.position);
        _given = true;

        OnGive?.Invoke();
        Debug.Log("[ChemStorageDrawer] Sodium + Gel 지급 완료");

        if (disableColliderAfterGive && _col)
            _col.enabled = false;
    }

    // (선택) 에디터용 리셋
    [ContextMenu("Reset Given (Editor)")]
    void ResetGiven()
    {
        _given = false;
        if (_col) _col.enabled = true;
    }
}
