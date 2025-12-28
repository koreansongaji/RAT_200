using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;
using System.Collections;

public class ChemMixingStation : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    [Header("요구 보유 플래그(보유만 체크, 소모X)")]
    [SerializeField] string sodiumId = "Sodium";
    [SerializeField] string gelId = "Gel";
    [SerializeField] string waterInFlaskId = "WaterInFlask";

    [Header("UI (월드 스페이스)")]
    [SerializeField] Canvas panel;
    [SerializeField] Button btnSodium;
    [SerializeField] Button btnWater;
    [SerializeField] Button btnGel;
    [SerializeField] Button btnMix;

    [Header("카운터 표시(TMP)")]
    [SerializeField] TMP_Text txtNa;
    [SerializeField] TMP_Text txtWater;
    [SerializeField] TMP_Text txtGel;
    [SerializeField] TMP_Text txtRecipe;

    [Header("필요량(기본값)")]
    [Min(0)][SerializeField] int needNa = 2;
    [Min(0)][SerializeField] int needWater = 1;
    [Min(0)][SerializeField] int needGel = 4;

    [Header("결과 처리")]
    public UnityEvent OnMakeBigNoise; // 실패 시 소음 이벤트

    // ▼▼▼ [수정] 플레이어 이동 관련 변수 삭제 & 연출용 변수 추가 ▼▼▼
    [Header("성공 연출 (Bridge)")]
    public LabToFridgeManager bridgeManager; // ★ 필수 연결: 책 다리/밧줄 연출 관리자
    public CinemachineCamera bridgeSideCam;        // ★ 필수 연결: 책 다리 쪽 사이드 카메라
    public float bridgeCamDuration = 2.5f;         // 카메라가 비추고 있을 시간

    // (기존 다이아몬드 카드는 BridgeManager에서 스페이드 카드를 떨구는 것으로 대체되므로 삭제하거나 유지 선택)
    // [SerializeField] GameObject diamondCardPrefab; 
    // [SerializeField] Transform cardSpawnPoint;     
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    [Header("3D Buttons (optional)")]
    [SerializeField] PressableButton3D btnNa3D;
    [SerializeField] PressableButton3D btnWater3D;
    [SerializeField] PressableButton3D btnGel3D;
    [SerializeField] PressableButton3D btnMix3D;

    // 세션 상태
    int _cNa, _cWater, _cGel;
    bool _session;

    // 내부 캐시
    MicroZoomSession _micro;

    public bool hidePlayerDuringMicro = true;
    public bool HidePlayerDuringMicro => hidePlayerDuringMicro;

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

        if (btnNa3D) btnNa3D.OnPressed.AddListener(() => Tap3D(ref _cNa, needNa, btnNa3D));
        if (btnWater3D) btnWater3D.OnPressed.AddListener(() => Tap3D(ref _cWater, needWater, btnWater3D));
        if (btnGel3D) btnGel3D.OnPressed.AddListener(() => Tap3D(ref _cGel, needGel, btnGel3D));
        if (btnMix3D) btnMix3D.OnPressed.AddListener(Submit);
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (!i) return false;
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

        if (_micro)
        {
            if (_micro.TryEnter(i))
            {
                Debug.Log("[ChemMixingStation] Enter Micro zoom");
            }
            return;
        }
        StartSession();
    }

    // ===== IMicroSessionHost 구현 =====
    public bool CanBeginMicro(PlayerInteractor player)
    {
        if (!player) return false;
        bool hasAll = player.HasItem(sodiumId) && player.HasItem(gelId) && player.HasItem(waterInFlaskId);
        return hasAll && !_session;
    }

    public void OnMicroEnter(PlayerInteractor player) => StartSession();
    public void OnMicroExit(PlayerInteractor player) => CancelSession();

    // ===== 세션 로직 =====
    public void StartSession()
    {
        _session = true;
        _cNa = _cWater = _cGel = 0;

        if (panel) panel.enabled = true;

        bool interactableState = true;
        SetBtnInteractable(btnSodium, btnNa3D, needNa > 0);
        SetBtnInteractable(btnWater, btnWater3D, needWater > 0);
        SetBtnInteractable(btnGel, btnGel3D, needGel > 0);
        SetBtnInteractable(btnMix, btnMix3D, true);

        RefreshTexts();
    }

    // 헬퍼: 버튼 활성화 일괄 처리
    void SetBtnInteractable(Button uiBtn, PressableButton3D worldBtn, bool active)
    {
        if (uiBtn) uiBtn.interactable = active;
        if (worldBtn) worldBtn.SetInteractable(active);
    }

    public void CancelSession() => EndSession(false);

    void EndSession(bool fromSubmit)
    {
        _session = false;
        if (panel) panel.enabled = false;
        // Micro 종료는 Submit이나 Exit 호출부에서 처리됨
    }

    void Tap(ref int counter, int need, Button src)
    {
        if (!_session || need <= 0) return;
        counter++;
        RefreshTexts();
    }

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
        if (txtRecipe) txtRecipe.text = $"Rate 2:1:{needGel}";
    }

    // ★★★ [제출 로직 수정] ★★★
    void Submit()
    {
        if (!_session) return;

        bool success = (_cNa == needNa) && (_cWater == needWater) && (_cGel == needGel);

        if (!success)
        {
            // 실패 시: 즉시 Micro 탈출하고 소음 발생
            if (_micro && _micro.InMicro) _micro.Exit();

            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(1f);
            OnMakeBigNoise?.Invoke();
            Debug.Log("[ChemMixingStation] 혼합 실패!");
            EndSession(true);
            return;
        }

        // === 성공 시 ===
        Debug.Log("[ChemMixingStation] 혼합 성공!");

        // 1. 물리/시각 연출 시작 (책 떨어짐, 밧줄 끊어짐)
        if (bridgeManager)
        {
            bridgeManager.PlaySequence();
        }

        // 2. 카메라 전환 코루틴 시작
        // ★ 중요: 여기서 _micro.Exit()를 호출하지 않습니다! (Lab 카메라 유지)
        StartCoroutine(Routine_ShowBridgeSequence());

        // 세션 로직만 내부적으로 종료 (버튼 비활성화 등)
        EndSession(true);
    }

    // ★★★ [카메라 시퀀스] ★★★
    IEnumerator Routine_ShowBridgeSequence()
    {
        // 1. Bridge 카메라 켜기 (Priority를 높게 설정해서 Lab 카메라를 덮어씀)
        // 주의: BridgeSideCam의 Priority는 Micro(30)보다 높아야 합니다. (예: 40~50)
        if (bridgeSideCam)
        {
            bridgeSideCam.Priority = 100; // 확실하게 높임
            // 혹은 CloseupCamManager에 별도 함수를 추가해도 됨
        }

        // 2. 책 넘어지는 연출 감상
        yield return new WaitForSeconds(bridgeCamDuration);

        // 3. Bridge 카메라 끄기 -> Lab 카메라(Micro)가 밑에 켜져 있으므로 자연스럽게 돌아옴
        if (bridgeSideCam)
        {
            bridgeSideCam.Priority = 0;
        }

        // 4. (선택 사항) 잠시 Lab을 보여준 뒤에 플레이어 조작을 돌려주고 싶다면:
        // yield return new WaitForSeconds(0.5f);

        // 5. 이제 진짜 종료 (플레이어 조작 복구, Room 뷰로 복귀할지는 유저가 ESC 누르거나 여기서 강제 종료)
        // 여기서는 "Lab 뷰로 돌아와서 멈춤" 상태를 유지하려면 아래 줄을 주석 처리하세요.
        // 자동으로 나가게 하려면 주석을 푸세요.
        if (_micro && _micro.InMicro)
            _micro.Exit();
    }

    // ===== API =====
    public void SetGelNeed(int newNeedGel)
    {
        needGel = Mathf.Max(0, newNeedGel);
        bool canPress = _session ? (_cGel < needGel) : (needGel > 0);
        SetBtnInteractable(btnGel, btnGel3D, canPress);
        RefreshTexts();
    }

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