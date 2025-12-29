using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using System.Collections;

public class WirePuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
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

    [Header("Cage Rat Event (성공 연출)")]
    public GameObject ratObj;

    // ★ [수정] 단일 ParticleSystem -> 배열([])로 변경
    [Tooltip("감전 시 재생할 파티클들 (여러 개 연결 가능)")]
    public ParticleSystem[] shockEffects;

    public GameObject crossbarObj;

    [Header("Reward Card (다이아 10)")]
    public GameObject rewardCardObj;
    public Transform cardSpawnPos;
    public Transform cardLandPos;

    [Header("Events")]
    public UnityEvent OnSolved;
    public UnityEvent OnFailed;

    // 내부 상태
    bool _session;
    int[] _h = new int[4];
    int _pressCount;
    PlayerInteractor _lastPlayer;
    MicroZoomSession _micro;
    bool _isSolved = false;
    bool _isAnimating = false;

    Vector3[] _baseLocalPos = new Vector3[4];

    static readonly bool[,] INFL = new bool[4, 4] {
        { true,  false, true,  false }, // B1
        { false, true,  false, true  }, // B2
        { true,  true,  true,  false }, // B3
        { false, true,  true,  true  }  // B4
    };

    void Awake()
    {
        _micro = GetComponent<MicroZoomSession>();
        WireButtons();
        if (worldCanvas) worldCanvas.enabled = false;

        CacheBaseLocalPositions();

        // 초기 상태 설정
        if (rewardCardObj) rewardCardObj.SetActive(false);
        if (crossbarObj) crossbarObj.SetActive(false);
        if (ratObj) ratObj.SetActive(true);

        // ★ [수정] 여러 이펙트 모두 정지
        if (shockEffects != null)
        {
            foreach (var fx in shockEffects)
            {
                if (fx) fx.Stop();
            }
        }
    }

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
        if (_micro && _micro.TryEnter(i)) return;
        StartSession();
    }

    public bool CanBeginMicro(PlayerInteractor player) => !_session && !_isSolved && !_isAnimating;

    public void OnMicroEnter(PlayerInteractor player)
    {
        _lastPlayer = player;
        StartSession();
    }

    public void OnMicroExit(PlayerInteractor player)
    {
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

    public void CancelSession()
    {
        _session = false;
        if (worldCanvas) worldCanvas.enabled = false;
        SetButtonsInteractable(false);
    }

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

        // 회전 로직
        for (int w = 0; w < 4; w++)
            if (INFL[bIndex, w])
                _h[w] = (_h[w] + 1) % 3;

        _pressCount++;

        SnapAll();
        RefreshTexts();

        // 1. 성공 체크: 모두 0일 때만 (높이 1, 2는 무반응)
        if (_h[0] == 0 && _h[1] == 0 && _h[2] == 0 && _h[3] == 0)
        {
            _isSolved = true;
            Debug.Log("[WirePuzzle] 성공! (All Zero)");
            StartCoroutine(Routine_SuccessSequence());
            return;
        }

        // 2. 실패 체크: 횟수 초과
        if (_pressCount >= failAtPresses)
        {
            Debug.Log("[WirePuzzle] 실패! (횟수 초과)");
            _isAnimating = true;
            SetButtonsInteractable(false);

            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(1f);
            OnFailed?.Invoke();
            CommonSoundController.Instance?.PlaySpark();

            if (_micro) _micro.Exit();
            else CancelSession();

            _isAnimating = false;
        }
    }

    IEnumerator Routine_SuccessSequence()
    {
        _isAnimating = true;
        SetButtonsInteractable(false);
        if (worldCanvas) worldCanvas.enabled = false;

        // 1. 쥐 감전 연출 (소리 & 이펙트들)
        CommonSoundController.Instance?.PlaySpark();

        // ★ [수정] 연결된 모든 이펙트 재생
        if (shockEffects != null)
        {
            foreach (var fx in shockEffects)
            {
                if (fx) fx.Play();
            }
        }

        yield return new WaitForSeconds(1.5f);

        // 2. 쥐 사라짐 & 가로대 등장
        if (ratObj) ratObj.SetActive(false);
        if (crossbarObj) crossbarObj.SetActive(true);

        // 3. 보상 카드 등장 
        if (rewardCardObj)
        {
            rewardCardObj.SetActive(true);

            if (cardSpawnPos && cardLandPos)
            {
                rewardCardObj.transform.position = cardSpawnPos.position;
                rewardCardObj.transform.rotation = cardSpawnPos.rotation;

                Sequence seq = DOTween.Sequence();
                seq.Append(rewardCardObj.transform.DOJump(cardLandPos.position, 0.5f, 1, 0.8f));
                seq.Join(rewardCardObj.transform.DORotate(cardLandPos.eulerAngles, 0.8f));
            }
            else
            {
                rewardCardObj.transform.localScale = Vector3.zero;
                rewardCardObj.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }
        }

        OnSolved?.Invoke();

        yield return new WaitForSeconds(0.5f);

        if (_micro) _micro.Exit();
        else CancelSession();

        _isAnimating = false;
    }

    void SnapAll()
    {
        Snap(w1, 0, _h[0]);
        Snap(w2, 1, _h[1]);
        Snap(w3, 2, _h[2]);
        Snap(w4, 3, _h[3]);
    }

    void Snap(Transform t, int index, int h)
    {
        if (!t) return;
        float y = h switch { 0 => level0Local.y, 1 => level1Local.y, _ => level2Local.y };
        var basePos = _baseLocalPos[index];
        var target = new Vector3(basePos.x, y, basePos.z);
        t.DOKill();
        t.DOLocalMove(target, snapDuration).SetEase(snapEase);
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