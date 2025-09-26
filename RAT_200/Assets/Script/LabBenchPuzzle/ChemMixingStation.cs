using UnityEngine;
using UnityEngine.Events;

public class ChemMixingStation : BaseInteractable
{
    [Header("요구 보유 플래그")]
    [SerializeField] string sodiumId = "Sodium";
    [SerializeField] string gelId = "Gel";
    [SerializeField] string waterInFlaskId = "WaterInFlask";

    [Header("UI (월드 스페이스 패널)")]
    [SerializeField] Canvas panel;             // 세션 중에만 On
    [SerializeField] UnityEngine.UI.Button btnNa;
    [SerializeField] UnityEngine.UI.Button btnWater;
    [SerializeField] UnityEngine.UI.Button btnGel;
    [SerializeField] UnityEngine.UI.Button btnMix;

    [Header("목표 탭 수(고정)")]
    [SerializeField] int needNa = 2;
    [SerializeField] int needWater = 1;
    [SerializeField] int needGel = 4;

    [Header("피드백(임시)")]
    public UnityEvent OnMakeBigNoise;          // 성공/실패 모두 호출(지금은 큰 소음만)

    // 세션 상태
    int _cNa, _cWater, _cGel;
    bool _session;

    void Awake()
    {
        if (panel) panel.enabled = false;
        WireButtons();
    }

    void WireButtons()
    {
        if (btnNa) btnNa.onClick.AddListener(() => Tap(ref _cNa, needNa, btnNa));
        if (btnWater) btnWater.onClick.AddListener(() => Tap(ref _cWater, needWater, btnWater));
        if (btnGel) btnGel.onClick.AddListener(() => Tap(ref _cGel, needGel, btnGel));
        if (btnMix) btnMix.onClick.AddListener(Submit);
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        // 재료 소모는 없지만, '보유'는 해야 세션 시작 가능
        if (!i) return false;
        bool ok = i.HasItem(sodiumId) && i.HasItem(gelId) && i.HasItem(waterInFlaskId);
        return ok && !_session;
    }

    public override void Interact(PlayerInteractor i)
    {
        // 이미 세션 중이면 무시
        if (_session) return;

        var micro = GetComponent<MicroZoomSession>();
        if (micro && micro.TryEnterMicro(i))
        {
            Debug.Log("[ChemMixingStation] Micro zoom + mixing session started");
            return;
        }

        //// (백업 루트) 혹시 마이크로 컨트롤러가 없으면, 기존처럼 바로 세션 시작
        if (!CanInteract(i))
        {
            Debug.Log("물품이 부족합니다!");
            return;
        }

        StartSession();
    }

    void StartSession()
    {
        _session = true;
        _cNa = _cWater = _cGel = 0;

        if (panel) panel.enabled = true;

        // 버튼 리셋
        if (btnNa) btnNa.interactable = true;
        if (btnWater) btnWater.interactable = true;
        if (btnGel) btnGel.interactable = true;
        if (btnMix) btnMix.interactable = true;

        // (선택) 카메라 클로즈업/입력잠금 등은 사다리 예시처럼 필요 시 연동
        // CloseupCamManager.Activate(vcam) ... PlayerScriptedMover는 성공 연출때 사용 가능
    }

    void Tap(ref int counter, int need, UnityEngine.UI.Button srcButton)
    {
        if (!_session) return;
        counter++;
        // 빼기 불가: 한계 도달 시 해당 버튼만 비활성
        if (counter >= need && srcButton) srcButton.interactable = false;

        // (선택) 현재 카운트 UI 표시 텍스트 갱신
    }

    void Submit()
    {
        if (!_session) return;

        bool success = (_cNa == needNa) && (_cWater == needWater) && (_cGel == needGel);

        // 지금은 성공/실패 모두 '큰 소음'만
        OnMakeBigNoise?.Invoke();

        // (선택) 성공 시 냉장고 위 이동 연출을 바로 넣을 수도 있음:
        // mover.MoveToWorldWithCam(fridgeTop.position, dur, ease, vcam, CloseupCamManager.CloseOn, true);
        // (연동은 사다리/스크립트 무버 참조)

        EndSession();
    }

    public void CancelSession() => EndSession();

    void EndSession()
    {
        _session = false;
        if (panel) panel.enabled = false;
        // (선택) 카메라 복귀 CloseupCamManager.Deactivate(...)
    }

    // 외부에서 세션 열기/닫기 전용 얇은 API
    public void BeginSessionFromExternal()
    {
        if (!_session) StartSession();
    }

    public void EndSessionFromExternal()
    {
        if (_session) EndSession();
    }
}
