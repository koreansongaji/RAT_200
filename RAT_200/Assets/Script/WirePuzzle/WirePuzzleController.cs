using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using System.Collections;

public class WirePuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    // ... (기존 변수들 유지) ...
    public bool hidePlayerDuringMicro = true;
    public bool HidePlayerDuringMicro => hidePlayerDuringMicro;

    [Header("Wires")]
    public Transform w1;
    public Transform w2;
    public Transform w3;
    public Transform w4;

    [Header("Local snap positions")]
    public Vector3 level0Local = new(0f, 0.00f, 0f);
    public Vector3 level1Local = new(0f, 0.04f, 0f);
    public Vector3 level2Local = new(0f, 0.08f, 0f);
    [Min(0f)] public float snapDuration = 0.12f;
    public Ease snapEase = Ease.OutQuad;

    [Header("Buttons")]
    public PressableButton3D b1;
    public PressableButton3D b2;
    public PressableButton3D b3;
    public PressableButton3D b4;

    [Header("UI")]
    public TMP_Text txtMovesLeft;
    public TMP_Text[] wireDebugTexts;
    public Canvas worldCanvas;

    [Header("Rules")]
    public int failAtPresses = 8;
    [Range(0, 2)] public int w1Init = 0;
    [Range(0, 2)] public int w2Init = 1;
    [Range(0, 2)] public int w3Init = 2;
    [Range(0, 2)] public int w4Init = 0;

    // ... (중간 생략: Event, Cage Rat Event 등) ...
    [Header("Cage Rat Event")]
    public GameObject ratObj;
    public GameObject deadRat;
    public ParticleSystem[] shockEffects;
    public GameObject crossbarObj;

    [Header("Reward")]
    public GameObject rewardCardObj;

    [Header("Events")]
    public UnityEvent OnSolved;
    public UnityEvent OnFailed;

    bool _session;
    int[] _h = new int[4];
    int _pressCount;
    PlayerInteractor _lastPlayer;
    MicroZoomSession _micro;
    Collider _mainCollider;

    bool _isSolved = false;
    bool _isAnimating = false;
    Vector3[] _baseLocalPos = new Vector3[4];

    static readonly bool[,] INFL = new bool[4, 4] {
        { true,  false, true,  false },
        { false, true,  false, true  },
        { true,  true,  true,  false },
        { false, true,  true,  true  }
    };

    private WirePuzzleSoundController _wirePuzzleSoundController;

    void Awake()
    {
        if (TryGetComponent(out _wirePuzzleSoundController))
        {
            _wirePuzzleSoundController = gameObject.AddComponent<WirePuzzleSoundController>();
        }

        _micro = GetComponent<MicroZoomSession>();
        _mainCollider = GetComponent<Collider>();

        RefreshMicroBind(b1);
        RefreshMicroBind(b2);
        RefreshMicroBind(b3);
        RefreshMicroBind(b4);

        WireButtons();
        if (worldCanvas) worldCanvas.enabled = false;
        CacheBaseLocalPositions();
        if (rewardCardObj) rewardCardObj.SetActive(false);
        if (crossbarObj) crossbarObj.SetActive(false);
        if (ratObj) ratObj.SetActive(true);
        if (deadRat) deadRat.SetActive(false);
        if (shockEffects != null)
        {
            foreach (var fx in shockEffects) if (fx) fx.Stop();
        }
    }

    void RefreshMicroBind(PressableButton3D btn)
    {
        if (!btn) return;
        if (!btn.micro && _micro) btn.micro = _micro;
        btn.enabled = false;
        btn.enabled = true;
    }

    // ... (CacheBaseLocalPositions, WireButtons, CanInteract 등 유지) ...
    void CacheBaseLocalPositions()
    {
        _baseLocalPos[0] = w1 ? w1.localPosition : Vector3.zero;
        _baseLocalPos[1] = w2 ? w2.localPosition : Vector3.zero;
        _baseLocalPos[2] = w3 ? w3.localPosition : Vector3.zero;
        _baseLocalPos[3] = w4 ? w4.localPosition : Vector3.zero;
    }

    void WireButtons()
    {
        if (b1) b1.OnPressed.AddListener(() => OnPress(0));
        if (b2) b2.OnPressed.AddListener(() => OnPress(1));
        if (b3) b3.OnPressed.AddListener(() => OnPress(2));
        if (b4) b4.OnPressed.AddListener(() => OnPress(3));
    }

    public override bool CanInteract(PlayerInteractor i) => !_session && !_isSolved && !_isAnimating;

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        _lastPlayer = i;
        if (_micro)
        {
            _micro.TryEnter(i);
            return;
        }
        StartSession();
    }

    public bool CanBeginMicro(PlayerInteractor player) => !_session && !_isSolved && !_isAnimating;

    public void OnMicroEnter(PlayerInteractor player)
    {
        if (_mainCollider) _mainCollider.enabled = false;
        _lastPlayer = player;
        StartSession();
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        if (_mainCollider) _mainCollider.enabled = true;
        CancelSession();
    }

    public void StartSession()
    {
        _session = true;
        _pressCount = 0;
        _h[0] = w1Init; _h[1] = w2Init; _h[2] = w3Init; _h[3] = w4Init;
        CacheBaseLocalPositions();
        if (worldCanvas) worldCanvas.enabled = true;
        SetButtonsInteractable(true);
        SnapAll();
        RefreshTexts();
    }

    // ★ [수정] 나갈 때(Cancel) 퍼즐 상태 초기화
    public void CancelSession()
    {
        _session = false;
        if (worldCanvas) worldCanvas.enabled = false;
        SetButtonsInteractable(false);

        if (!_isSolved)
        {
            // 실패했거나 도중에 나간 경우에만 초기값으로 복원
            _pressCount = 0;
            _h[0] = w1Init; _h[1] = w2Init; _h[2] = w3Init; _h[3] = w4Init;

            // 시각적 갱신 (초기 위치로 복귀)
            SnapAll();
            RefreshTexts();
        }

        // 시각적 갱신 (SnapAll로 즉시 이동)
        SnapAll();
        RefreshTexts();
    }

    // ... (SetButtonsInteractable, OnPress, Routine_SuccessSequence, SnapAll, Snap, RefreshTexts 기존 유지) ...
    void SetButtonsInteractable(bool on)
    {
        if (b1) b1.SetInteractable(on);
        if (b2) b2.SetInteractable(on);
        if (b3) b3.SetInteractable(on);
        if (b4) b4.SetInteractable(on);
    }

    void OnPress(int bIndex)
    {
        if (!_session || _isAnimating || _isSolved) return;
        for (int w = 0; w < 4; w++) if (INFL[bIndex, w]) _h[w] = (_h[w] + 1) % 3;
        _pressCount++;
        SnapAll();
        RefreshTexts();

        if (_h[0] == 0 && _h[1] == 0 && _h[2] == 0 && _h[3] == 0)
        {
            Debug.Log("[WirePuzzle] 성공!");
            _isSolved = true;
            StartCoroutine(Routine_SuccessSequence());
            return;
        }

        if (_pressCount >= failAtPresses)
        {
            Debug.Log("[WirePuzzle] 실패!");
            SetButtonsInteractable(false);
            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(1f);
            OnFailed?.Invoke();
            CommonSoundController.Instance?.PlayPuzzleFail();

            if (_micro && _micro.InMicro) _micro.Exit();
            else CancelSession();
        }
    }

    IEnumerator Routine_SuccessSequence()
    {
        _isAnimating = true;
        SetButtonsInteractable(false);
        if (worldCanvas) worldCanvas.enabled = false;

        if (_micro) _micro.SetExitLocked(true);

        CommonSoundController.Instance?.PlaySpark();
        if (shockEffects != null) foreach (var fx in shockEffects) if (fx) fx.Play();

        yield return new WaitForSeconds(1.5f);

        if (ratObj) ratObj.SetActive(false);
        if (deadRat) deadRat.SetActive(true);
        if (crossbarObj) crossbarObj.SetActive(true);
        if (rewardCardObj) rewardCardObj.SetActive(true);
        OnSolved?.Invoke();
        yield return new WaitForSeconds(0.5f);
        if (_micro)
        {
            _micro.SetExitLocked(false);
            _micro.Exit();
        }
        else
        {
            CancelSession();
        }
        _isAnimating = false;
    }

    void SnapAll()
    {
        Snap(w1, 0, _h[0]); Snap(w2, 1, _h[1]); Snap(w3, 2, _h[2]); Snap(w4, 3, _h[3]);
    }

    void Snap(Transform t, int index, int h)
    {
        if (!t) return;
        float y = h switch { 0 => level0Local.y, 1 => level1Local.y, _ => level2Local.y };
        var basePos = _baseLocalPos[index];
        t.DOKill();
        t.DOLocalMove(new Vector3(basePos.x, y, basePos.z), snapDuration).SetEase(snapEase);
    }

    void RefreshTexts()
    {
        if (txtMovesLeft) txtMovesLeft.text = $"Moves: {_pressCount}/{failAtPresses - 1}";
        if (wireDebugTexts != null && wireDebugTexts.Length >= 4)
        {
            if (wireDebugTexts[0]) wireDebugTexts[0].text = $"W1:{_h[0]}";
            if (wireDebugTexts[1]) wireDebugTexts[1].text = $"W2:{_h[1]}";
            if (wireDebugTexts[2]) wireDebugTexts[2].text = $"W3:{_h[2]}";
            if (wireDebugTexts[3]) wireDebugTexts[3].text = $"W4:{_h[3]}";
        }
    }
}