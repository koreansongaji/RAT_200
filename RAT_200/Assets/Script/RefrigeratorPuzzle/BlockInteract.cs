using UnityEngine;

public class BlockInteract : BaseInteractable
{
    [Header("세션")]
    [SerializeField] MicroZoomSession micro;     // 같은 오브젝트나 부모/자식에서 찾아도 됨
    [SerializeField] MonoBehaviour hostOverride; // 필요 시 특정 Host(IMicroSessionHost) 강제 (선택)

    IMicroSessionHost _host;

    void Awake()
    {
        // MicroZoomSession 자동 탐색 (비워두면 자식/부모에서 찾음)
        if (!micro)
        {
            micro = GetComponentInChildren<MicroZoomSession>(true)
                 ?? GetComponentInParent<MicroZoomSession>();
        }

        // Host(IMicroSessionHost) 자동 탐색 (선택)
        _host = hostOverride as IMicroSessionHost
             ?? GetComponent<IMicroSessionHost>()
             ?? GetComponentInChildren<IMicroSessionHost>()
             ?? GetComponentInParent<IMicroSessionHost>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        // Micro 세션이 없으면 상호작용 불가
        if (!micro) return false;

        // Host가 있으면 그 조건을 그대로 사용 (예: 특정 아이템 필요 등)
        if (_host != null)
            return _host.CanBeginMicro(i);

        // Host 없으면 별도 조건 없이 허용
        return true;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!micro) return;

        // Closeup 상태에서 → Micro로 진입
        micro.TryEnter(i);
    }
}
