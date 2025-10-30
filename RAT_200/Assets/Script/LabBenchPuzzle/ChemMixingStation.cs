using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;

public class ChemMixingStation : BaseInteractable
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

    // --- ChemMixingStation 내부에 필드만 추가 ---
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
            // 1) 아직 Micro가 아니면 진입 시도
            if (_micro.TryEnterMicro(i))
            {
                Debug.Log("[ChemMixingStation] Enter Micro zoom");
            }
            // 2) 이미 Micro 상태거나, 블록 윈도우라면 아무 것도 하지 않음
            return; // ★★★ 여기서 폴백 StartSession() 절대 금지!
        }

        // Micro 컨트롤러(컴포넌트)가 아예 없을 때만 직접 세션 시작
        StartSession();
    }

    // ===== 세션 ====
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

        // Micro 종료(ESC로도 나오지만 제출 이후 자동 복귀)
        //if (_micro) _micro.ExitMicro();
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

        if (!success)
        {
            // 실패: 소음을 최대로 올려 연구원 소환 트리거
            if (NoiseSystem.Instance)
            {
                // 1로 고정 세팅 보장: 현재값과 상관없이 최대로
                NoiseSystem.Instance.FireImpulse(1f);
            }
            OnMakeBigNoise?.Invoke();
            Debug.Log("[ChemMixingStation] 혼합 실패 → 소음 최대치");
            EndSession(true);
            return;
        }

        // 성공 처리
        Debug.Log("[ChemMixingStation] 혼합 성공!");

        // 카드 생성(폭발 연출은 추후; 지금은 간단히 스폰)
        if (diamondCardPrefab && cardSpawnPoint)
        {
            Instantiate(diamondCardPrefab, cardSpawnPoint.position, cardSpawnPoint.rotation);
        }

        // 냉장고 위로 플레이어 이동(선택 카메라 클로즈업)
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
                    CloseupCamManager.CloseOn,
                    true
                );
            }
        }

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
    public void BeginSessionFromExternal()
    {
        // 기존 외부 진입 호출과 동일 동작
        StartSession();
    }

    public void EndSessionFromExternal()
    {
        // 제출이 아닌 외부 종료 플로우로 정리
        // EndSession(bool fromSubmit) 내부에서 Micro 종료까지 처리됨
        // (세션 중이 아닐 때 호출되어도 안전)
        var wasSession = true; // 의미만 전달; 내부에서 플래그 검사함
        CancelSession();       // EndSession(false) 호출과 동일
    }

    // 에디터에서 숫자 바뀌면 텍스트 갱신되도록
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
