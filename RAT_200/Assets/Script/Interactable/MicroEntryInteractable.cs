using UnityEngine;

public class MicroEntryInteractable : BaseInteractable
{
    [Header("세션")]
    [SerializeField] MicroZoomSession micro;         // 같은 오브젝트나 부모/자식에서 찾아도 됨
    [SerializeField] MonoBehaviour hostOverride;     // 특정 Host 강제하고 싶을 때(선택)

    IMicroSessionHost _host;

    void Awake()
    {
        if (!micro) micro = GetComponentInChildren<MicroZoomSession>(true)
                          ?? GetComponentInParent<MicroZoomSession>();
        _host = hostOverride as IMicroSessionHost
             ?? GetComponent<IMicroSessionHost>()
             ?? GetComponentInChildren<IMicroSessionHost>()
             ?? GetComponentInParent<IMicroSessionHost>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        // Micro 세션이 있어야 함
        if (!micro) return false;

        // Host가 있으면 그것의 CanBeginMicro를 그대로 사용
        if (_host != null)
            return _host.CanBeginMicro(i);

        // Host가 없다면 별도 조건 없음(바로 진입 허용)
        return true;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!micro) return;
        // CloseupCam을 거치지 않고 바로 Micro로
        micro.TryEnter(i);
    }

    // ▼ [추가됨] 외부(ClickMoveOrInteract)에서 "지금 거리 무시하고 바로 진입해도 돼?"라고 물어볼 때 사용
    public bool ShouldBypassDistanceCheck(Vector3 playerPos)
    {
        if (!micro) return false;

        // 연결된 세션(MicroZoomSession)에게 판정을 위임합니다.
        // 세션이 'Zone 모드'이고 플레이어가 구역 안에 있다면 true를 반환합니다.
        return micro.IsPlayerInsideZone(playerPos);
    }
}