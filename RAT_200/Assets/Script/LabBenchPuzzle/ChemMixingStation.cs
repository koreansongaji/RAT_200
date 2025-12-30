using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;
using System.Collections;

public class ChemMixingStation : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    // ... (기존 변수들) ...
    [Header("필요 아이템 ID")]
    [SerializeField] string sodiumId = "Sodium";
    [SerializeField] string gelId = "Gel";
    [SerializeField] string waterInFlaskId = "WaterInFlask";

    [Header("UI Refs (월드 스페이스)")]
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

    [Header("정답 레시피")]
    [Min(0)][SerializeField] int needNa = 3;
    [Min(0)][SerializeField] int needWater = 4;
    [Min(0)][SerializeField] int needGel = 2;

    [Header("3D Buttons (옵션)")]
    [SerializeField] PressableButton3D btnNa3D;
    [SerializeField] PressableButton3D btnWater3D;
    [SerializeField] PressableButton3D btnGel3D;
    [SerializeField] PressableButton3D btnMix3D;

    [Header("Visuals")]
    public GameObject tubeNaObj;
    public GameObject tubeWaterObj;
    public GameObject tubeGelObj;
    public ParticleSystem steamParticle;

    [Header("외부 연결")]
    public LabToFridgeManager bridgeManager;
    public CinemachineCamera bridgeSideCam;
    public float steamShowDuration = 1.5f;
    public float bridgeCamDuration = 3.0f;

    [Header("Reward")]
    public GameObject rewardCardObj;

    [Header("Sound & Event")]
    public UnityEvent OnMakeBigNoise;

    int _cNa, _cWater, _cGel;
    bool _hasPlacedNa, _hasPlacedWater, _hasPlacedGel;
    bool _session;
    bool _isSuccessSequence = false;
    bool _isSolved = false;
    MicroZoomSession _micro;
    Collider _mainCollider;

    public bool HidePlayerDuringMicro => true;

    void Awake()
    {
        if (panel) panel.enabled = true;
        _micro = GetComponent<MicroZoomSession>();
        _mainCollider = GetComponent<Collider>();

        // ★ [핵심] 하위 버튼들에게 Micro 세션 주입 & 재부팅
        RefreshMicroBind(btnNa3D);
        RefreshMicroBind(btnWater3D);
        RefreshMicroBind(btnGel3D);
        RefreshMicroBind(btnMix3D);

        if (tubeNaObj) tubeNaObj.SetActive(false);
        if (tubeWaterObj) tubeWaterObj.SetActive(false);
        if (tubeGelObj) tubeGelObj.SetActive(false);
        if (steamParticle) steamParticle.Stop();
        if (rewardCardObj) rewardCardObj.SetActive(false);

        WireButtons();
        RefreshTexts();
        if (txtRecipe) txtRecipe.text = "MIX";
    }

    // ★ [추가] 금고 퍼즐에서 가져온 바인딩 헬퍼 함수
    void RefreshMicroBind(PressableButton3D btn)
    {
        if (!btn) return;

        // Micro 세션 주입
        if (!btn.micro && _micro)
        {
            btn.micro = _micro;
        }

        // 껐다 켜서 OnEnable 다시 실행 (이벤트 구독 갱신 등)
        btn.enabled = false;
        btn.enabled = true;
    }

    // --- 1. 진입 ---
    public override bool CanInteract(PlayerInteractor i) => !_session && !_isSuccessSequence && !_isSolved;

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        if (_micro) _micro.TryEnter(i);
        else StartSession(i);
    }

    public bool CanBeginMicro(PlayerInteractor player) => !_session && !_isSuccessSequence && !_isSolved;

    // 줌인/아웃 시 메인 콜라이더 제어 (이것도 같이 챙겨드립니다)
    public void OnMicroEnter(PlayerInteractor player)
    {
        if (_mainCollider) _mainCollider.enabled = false;
        StartSession(player);
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        if (_mainCollider) _mainCollider.enabled = true;
        CancelSession();
    }

    // ... (이하 StartSession, WireButtons, 로직 등은 기존 유지) ...
    void StartSession(PlayerInteractor player)
    {
        _session = true;
        _cNa = 0; _cWater = 0; _cGel = 0;
        if (panel) panel.enabled = true;

        if (player != null)
        {
            CheckAndPlaceItem(player, sodiumId, ref _hasPlacedNa, tubeNaObj);
            CheckAndPlaceItem(player, waterInFlaskId, ref _hasPlacedWater, tubeWaterObj);
            CheckAndPlaceItem(player, gelId, ref _hasPlacedGel, tubeGelObj);
        }

        bool allReady = _hasPlacedNa && _hasPlacedWater && _hasPlacedGel;
        SetButtonsState(allReady);
        RefreshTexts();
        if (txtRecipe) txtRecipe.text = "MIX";
    }

    void CheckAndPlaceItem(PlayerInteractor player, string itemId, ref bool isPlaced, GameObject visualObj)
    {
        if (isPlaced) return;
        if (player && player.HasItem(itemId))
        {
            player.RemoveItem(itemId);
            isPlaced = true;
            if (visualObj)
            {
                visualObj.SetActive(true);
                visualObj.transform.DOKill();
                Vector3 originalPos = visualObj.transform.localPosition;
                visualObj.transform.localPosition = originalPos + Vector3.up * 0.2f;
                visualObj.transform.DOLocalMove(originalPos, 0.5f).SetEase(Ease.OutBounce);
            }
            Debug.Log($"[ChemStation] {itemId} 설치 완료!");
        }
    }

    void SetButtonsState(bool ready)
    {
        if (btnSodium) btnSodium.interactable = ready;
        if (btnWater) btnWater.interactable = ready;
        if (btnGel) btnGel.interactable = ready;
        if (btnMix) btnMix.interactable = ready;

        if (btnNa3D) btnNa3D.SetInteractable(ready);
        if (btnWater3D) btnWater3D.SetInteractable(ready);
        if (btnGel3D) btnGel3D.SetInteractable(ready);
        if (btnMix3D) btnMix3D.SetInteractable(ready);
    }

    public void CancelSession()
    {
        _session = false;
    }

    void WireButtons()
    {
        if (btnSodium) btnSodium.onClick.AddListener(() => Tap(ref _cNa, needNa));
        if (btnWater) btnWater.onClick.AddListener(() => Tap(ref _cWater, needWater));
        if (btnGel) btnGel.onClick.AddListener(() => Tap(ref _cGel, needGel));
        if (btnMix) btnMix.onClick.AddListener(Submit);

        if (btnNa3D) btnNa3D.OnPressed.AddListener(() => Tap3D(ref _cNa, needNa));
        if (btnWater3D) btnWater3D.OnPressed.AddListener(() => Tap3D(ref _cWater, needWater));
        if (btnGel3D) btnGel3D.OnPressed.AddListener(() => Tap3D(ref _cGel, needGel));
        if (btnMix3D) btnMix3D.OnPressed.AddListener(Submit);
    }

    void Tap(ref int counter, int need) { if (!_session) return; counter++; RefreshTexts(); }
    void Tap3D(ref int counter, int need) { if (!_session) return; counter++; RefreshTexts(); }

    void RefreshTexts()
    {
        string Mark(int c, int n) => c > n ? $"{c}" : c.ToString();
        if (txtNa) txtNa.text = $"Na: {Mark(_cNa, needNa)}";
        if (txtWater) txtWater.text = $"Water: {Mark(_cWater, needWater)}";
        if (txtGel) txtGel.text = $"Gel: {Mark(_cGel, needGel)}";
    }

    void Submit()
    {
        if (!_session || _isSuccessSequence) return;
        bool success = (_cNa == needNa) && (_cWater == needWater) && (_cGel == needGel);

        if (!success)
        {
            Debug.Log("[ChemStation] 혼합 실패!");
            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(0.5f);
            OnMakeBigNoise?.Invoke();
            CommonSoundController.Instance?.PlayPuzzleFail();
            if (_micro && _micro.InMicro) _micro.Exit();
            return;
        }

        Debug.Log("[ChemStation] 혼합 성공!");
        CommonSoundController.Instance?.PlayPuzzleSuccess();
        _isSolved = true;
        StartCoroutine(Routine_SuccessSequence());
    }

    IEnumerator Routine_SuccessSequence()
    {
        _isSuccessSequence = true;
        SetButtonsState(false);
        if (panel) panel.enabled = false;
        if (steamParticle) steamParticle.Play();
        yield return new WaitForSeconds(steamShowDuration);
        if (bridgeSideCam) bridgeSideCam.Priority = 100;
        yield return new WaitForSeconds(0.5f);
        if (bridgeManager) bridgeManager.PlaySequence();
        yield return new WaitForSeconds(bridgeCamDuration);
        if (bridgeSideCam) bridgeSideCam.Priority = 0;
        if (_micro && _micro.InMicro) _micro.Exit();
        if (rewardCardObj) rewardCardObj.SetActive(true);
        _isSuccessSequence = false;
    }

    public void BeginSessionFromExternal() => StartSession(null);
    public void EndSessionFromExternal() => CancelSession();

    public void SetGelNeed(int newNeedGel)
    {
        needGel = Mathf.Max(0, newNeedGel);
        RefreshTexts();
    }
}