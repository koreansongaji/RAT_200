using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;

public class ChemMixingStation : BaseInteractable, IMicroSessionHost
{
    [Header("요구 보유 플래그(보유만 체크, 소모X)")]
    [SerializeField] string sodiumId = "Sodium";
    [SerializeField] string gelId = "Gel";
    [SerializeField] string waterInFlaskId = "WaterInFlask";

    [Header("UI (월드 스페이스)")]
    [SerializeField] Canvas panel;                 // 퍼즐 세션 중에만 On
    [SerializeField] Button btnSodium;             // 나트륨 버튼
    [SerializeField] Button btnWater;              // 물 버튼
    [SerializeField] Button btnGel;                // 젤 버튼
    [SerializeField] Button btnMix;                // 혼합(제출) 버튼

    [Header("카운터 표시(TMP)")]
    [SerializeField] TMP_Text txtNa;               // "Na: 0/2" 등
    [SerializeField] TMP_Text txtWater;            // "H₂O: 0/1"
    [SerializeField] TMP_Text txtGel;              // "Gel: 0/4"
    [SerializeField] TMP_Text txtRecipe;           // 상단 설명(선택)

    [Header("필요량(기본값)")]
    [Min(0)][SerializeField] int needNa = 2;      // 변동 거의 없음
    [Min(0)][SerializeField] int needWater = 1;   // 변동 거의 없음
    [Min(0)][SerializeField] int needGel = 4;     // ★ 매판 레시피로 바뀜

    [Header("결과 처리")]
    public UnityEvent OnMakeBigNoise;              // 외부 연결용(연구원 소환 등)

    [Header("성공 시 이동/스폰")]
    [SerializeField] Transform fridgeTop;          // 냉장고 위 좌표
    [SerializeField] CinemachineCamera fridgeVCam; // 도착 시 클로즈업(선택)
    [SerializeField] float moveDuration = 0.8f;
    [SerializeField] Ease moveEase = Ease.InOutSine;
    [SerializeField] GameObject diamondCardPrefab; // 다이아 문양 카드 프리팹
    [SerializeField] Transform cardSpawnPoint;     // 카드 생성 위치(실험대 앞)

    [Header("3D Buttons (optional)")]
    [SerializeField] PressableButton3D btnNa3D;
    [SerializeField] PressableButton3D btnWater3D;
    [SerializeField] PressableButton3D btnGel3D;
    [SerializeField] PressableButton3D btnMix3D;

    // 세션 상태
    int _cNa, _cWater, _cGel;
    bool _session;
    PlayerInteractor _lastPlayer;

    // 내부 캐시
    MicroZoomSession _micro;

    void Awake()
    {
        if (panel) panel.enabled = false;
        _micro = GetComponent<MicroZoomSession>();
        WireButtons();
        RefreshTexts();
    }

    void WireButtons()
    {
        if (btnSodium) btnSodium.onClick.AddListener(() => Tap(ref _cNa, needNa, btnSodium));
        if (btnWater) btnWater.onClick.AddListener(() => Tap(ref _cWater, needWater, btnWater));
        if (btnGel) btnGel.onClick.AddListener(() => Tap(ref _cGel, needGel, btnGel));
        if (btnMix) btnMix.onClick.AddListener(Submit);

        // 3D 버튼 연결
        if (btnNa3D) btnNa3D.OnPressed.AddListener(() => Tap3D(ref _cNa, needNa, btnNa3D));
        if (btnWater3D) btnWater3D.OnPressed.AddListener(() => Tap3D(ref _cWater, needWater, btnWater3D));
        if (btnGel3D) btnGel3D.OnPressed.AddListener(() => Tap3D(ref _cGel, needGel, btnGel3D));
        if (btnMix3D) btnMix3D.OnPressed.AddListener(Submit);
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (!i) return false;
        // 재료 '보유'는 해야 세션 시작 가능. 세션 중 중복 진입 방지
        bool hasAll = i.HasItem(sodiumId) && i.HasItem(gelId) && i.HasItem(waterInFlaskId);
        return hasAll && !_session;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (_session) return;
        if (!CanInteract(i))
        {
            Debug.Log("[ChemMixingStation] 요구 재료가 부족합니다.");
            return;
        }

        _lastPlayer = i;

        if (_micro) // ★ Micro가 붙어있다면…
        {
            // === 새 MicroZoomSession 시그니처 ===
            if (_micro.TryEnter(i))
            {
                Debug.Log("[ChemMixingStation] Enter Micro zoom");
            }
            // === 구(舊) 시그니처를 쓰는 프로젝트라면 위 한 줄을 아래로 교체 ===
            // if (_micro.TryEnterMicro(i)) { Debug.Log("[ChemMixingStation] Enter Micro zoom"); }
            return; // ★★★ 폴백 StartSession() 금지(동작 유지)
        }

        // Micro 컨트롤러가 아예 없을 때만 직접 세션 시작
        StartSession();
    }

    // ===== IMicroSessionHost 구현 (MicroZoomSession이 자동 호출) =====
    public bool CanBeginMicro(PlayerInteractor player)
    {
        // 기존 CanInteract과 동일한 조건 사용(보유만 체크 + 세션 중복 방지)
        if (!player) return false;
        bool hasAll = player.HasItem(sodiumId) && player.HasItem(gelId) && player.HasItem(waterInFlaskId);
        return hasAll && !_session;
    }

    public void OnMicroEnter(PlayerInteractor player)
    {
        StartSession();
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        CancelSession();
    }

    // ===== 세션 =====
    public void StartSession()
    {
        _session = true;
        _cNa = _cWater = _cGel = 0;

        if (panel) panel.enabled = true;

        if (btnSodium) btnSodium.interactable = needNa > 0;
        if (btnWater) btnWater.interactable = needWater > 0;
        if (btnGel) btnGel.interactable = needGel > 0;
        if (btnMix) btnMix.interactable = true;

        // 3D 버튼
        if (btnNa3D) btnNa3D.SetInteractable(needNa > 0);
        if (btnWater3D) btnWater3D.SetInteractable(needWater > 0);
        if (btnGel3D) btnGel3D.SetInteractable(needGel > 0);
        if (btnMix3D) btnMix3D.SetInteractable(true);

        RefreshTexts();
    }

    public void CancelSession() => EndSession(false);

    void EndSession(bool fromSubmit)
    {
        _session = false;
        if (panel) panel.enabled = false;

        // Micro 종료는 MicroZoomSession에서 ESC/제출 흐름에 맞춰 처리
        // if (_micro) _micro.Exit(); // (구버전: ExitMicro())
    }

    void Tap(ref int counter, int need, Button src)
    {
        if (!_session) return;
        if (need <= 0) return;

        counter++;
        RefreshTexts();
    }

    // 3D 버튼 전용 탭(Interactable 토글 반영)
    void Tap3D(ref int counter, int need, PressableButton3D src)
    {
        if (!_session) return;
        if (need <= 0) { if (src) src.SetInteractable(false); return; }

        counter++;
        RefreshTexts();
    }

    void RefreshTexts()
    {
        string Mark(int c, int n) => c > n ? $"<color=#ff6060>{c}</color>" : c.ToString();

        if (txtNa) txtNa.text = $"Na: {Mark(_cNa, needNa)}/{needNa}";
        if (txtWater) txtWater.text = $"Water: {Mark(_cWater, needWater)}/{needWater}";
        if (txtGel) txtGel.text = $"Gel: {Mark(_cGel, needGel)}/{needGel}";
        if (txtRecipe) txtRecipe.text = $"Rate 2:1:{needGel} (Na:Water:Gel)";
    }

    void Submit()
    {
        if (!_session) return;

        bool success = (_cNa == needNa) && (_cWater == needWater) && (_cGel == needGel);

        // ★ 성공/실패 공통: 마이크로에서 반드시 빠져나오기(ESC 누른 것처럼)
        if (_micro && _micro.InMicro)
            _micro.Exit();

        if (!success)
        {
            // 실패: 소음 최대로, 외부 이벤트
            if (NoiseSystem.Instance)
                NoiseSystem.Instance.FireImpulse(1f);
            OnMakeBigNoise?.Invoke();
            Debug.Log("[ChemMixingStation] 혼합 실패 → 소음 최대치");

            // 세션 종료(Exit()에서 CancelSession이 호출되지만,
            // Micro가 없을 수도 있으니 로컬도 안전하게 종료)
            EndSession(true);
            return;
        }

        // === 성공 처리 ===
        Debug.Log("[ChemMixingStation] 혼합 성공!");

        // 1) 카드 생성
        if (diamondCardPrefab && cardSpawnPoint)
            Instantiate(diamondCardPrefab, cardSpawnPoint.position, cardSpawnPoint.rotation);

        // 2) CloseCam 전환: 실험대 closecam은 0으로, 냉장고 closecam을 20으로
        //    Activate()가 기존 활성 closecam의 우선순위를 0으로 내리고 새 cam을 20으로 올려줌.
        if (fridgeVCam) CloseupCamManager.Activate(fridgeVCam);

        // 3) 플레이어를 절대 좌표(냉장고 위)로 이동 + 냉장고 closecam 유지
        if (_lastPlayer)
        {
            var mover = _lastPlayer.GetComponent<PlayerScriptedMover>();
            if (mover && fridgeTop)
            {
                mover.MoveToWorldWithCam(
                    fridgeTop.position,
                    moveDuration,
                    moveEase,
                    fridgeVCam,
                    CloseupCamManager.CloseOn, // = 20
                    true
                );
            }
        }

        // Micro가 없는 폴백 케이스 대비 로컬 세션 종료
        EndSession(true);
    }


    // ===== 외부 레시피 주입 API =====
    /// <summary>매판 바뀌는 젤 필요량을 설정(예: 2~6).</summary>
    public void SetGelNeed(int newNeedGel)
    {
        needGel = Mathf.Max(0, newNeedGel);
        // 버튼/텍스트 즉시 반영
        if (btnGel) btnGel.interactable = _session ? (_cGel < needGel) : (needGel > 0);
        if (btnGel3D) btnGel3D.SetInteractable(_session ? (_cGel < needGel) : (needGel > 0));
        RefreshTexts();
    }

    // === Backward-compat wrappers (for MicroZoomSession) ===
    public void BeginSessionFromExternal() => StartSession();
    public void EndSessionFromExternal() => CancelSession();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (needNa < 0) needNa = 0;
        if (needWater < 0) needWater = 0;
        if (needGel < 0) needGel = 0;
        RefreshTexts();
    }
#endif
}
