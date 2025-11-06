using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;

/// 4와이어×3단 높이 퍼즐 컨트롤러.
/// - 목표: 모든 W가 높이 0
/// - 버튼 영향(문제 정의): 
///   B1:(1,0,1,0)  B2:(0,1,0,1)  B3:(1,1,1,0)  B4:(0,1,1,1)
/// - 각 버튼은 영향받는 와이어의 높이를 +1(mod 3) 시킴
/// - 8회 이상 누르면 실패(Noise 100%)
public class WirePuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    public bool hidePlayerDuringMicro = true; // 퍼즐별로 토글
    public bool HidePlayerDuringMicro => hidePlayerDuringMicro;

    [Header("Wires (Transforms at current piece root)")]
    public Transform w1;
    public Transform w2;
    public Transform w3;
    public Transform w4;

    [Header("Local snap positions for levels (0=연결,1=위,2=그보다 위)")]
    public Vector3 level0Local = new(0f, 0.00f, 0f);
    public Vector3 level1Local = new(0f, 0.04f, 0f);
    public Vector3 level2Local = new(0f, 0.08f, 0f);
    [Min(0f)] public float snapDuration = 0.12f;
    public Ease snapEase = Ease.OutQuad;

    [Header("3D Buttons")]
    public PressableButton3D b1;
    public PressableButton3D b2;
    public PressableButton3D b3;
    public PressableButton3D b4;

    [Header("UI (선택)")]
    public TMP_Text txtMovesLeft;      // "Moves: 0/7" 등
    public TMP_Text[] wireDebugTexts;  // W1~W4 높이 디버그 표기(선택)
    public Canvas worldCanvas;         // 세션 중에만 ON

    [Header("Rules")]
    [Tooltip("버튼 누름 횟수 8 이상이면 실패")]
    public int failAtPresses = 8;      // 8 이상이면 실패
    [Tooltip("초기 높이 (0/1/2)")]
    [Range(0, 2)] public int w1Init = 0;
    [Range(0, 2)] public int w2Init = 1;
    [Range(0, 2)] public int w3Init = 2;
    [Range(0, 2)] public int w4Init = 0;

    [Header("Events")]
    public UnityEvent OnSolved;
    public UnityEvent OnFailed;

    // 세션 상태
    bool _session;
    int[] _h = new int[4];  // W1..W4
    int _pressCount;
    PlayerInteractor _lastPlayer;
    MicroZoomSession _micro;

    Vector3[] _baseLocalPos = new Vector3[4];

    // 버튼→와이어 영향 (하드코드: 문제 정의 그대로)
    // rows: B1..B4, cols: W1..W4
    static readonly bool[,] INFL = new bool[4, 4] {
        { true,  false, true,  false }, // B1
        { false, true,  false, true  }, // B2
        { true,  true,  true,  false }, // B3
        { false, true,  true,  true  }  // B4
    };

    // 클래스 필드에 가드 추가
    bool _ending = false;

    void Awake()
    {
        _micro = GetComponent<MicroZoomSession>();
        WireButtons();
        if (worldCanvas) worldCanvas.enabled = false;

        // 초기 배치값 캐시 (에디터에서 배치한 그대로 보존)
        CacheBaseLocalPositions();
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

    // ===== BaseInteractable =====
    public override bool CanInteract(PlayerInteractor i)
    {
        // 세션 중 중복 진입 방지
        return !_session;
    }

    public override void Interact(PlayerInteractor i)
    {
        // Micro 세션 엔트리는 따로 MicroEntryInteractable을 써도 되고,
        // 퍼즐이 직접 Micro를 열어도 됨. 여기서는 Micro가 붙어있다면 그쪽으로 진입만 위임.
        if (_session) return;
        _lastPlayer = i;
        if (_micro && _micro.TryEnter(i)) return;
        // Micro가 없을 때 직접 시작
        StartSession();
    }

    // ===== IMicroSessionHost =====
    public bool CanBeginMicro(PlayerInteractor player) => !_session;

    public void OnMicroEnter(PlayerInteractor player)
    {
        _lastPlayer = player;
        StartSession();
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        CancelSession();
    }

    // ===== Session =====
    public void StartSession()
    {
        _session = true;
        _ending = false;
        _pressCount = 0;
        _h[0] = w1Init; _h[1] = w2Init; _h[2] = w3Init; _h[3] = w4Init;

        // 혹시 런타임에 프리팹 재배치됐을 수 있으니 세션 시작 때도 한 번 더 캐시
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
        if (!_session || _ending) return;

        // 버튼 영향 적용 (+1 mod 3)
        for (int w = 0; w < 4; w++)
            if (INFL[bIndex, w])
                _h[w] = (_h[w] + 1) % 3;

        _pressCount++;

        SnapAll();
        RefreshTexts();

        // 성공 체크
        if (_h[0] == 0 && _h[1] == 0 && _h[2] == 0 && _h[3] == 0)
        {
            _ending = true;
            SetButtonsInteractable(false);
            OnSolved?.Invoke();

            // Micro 종료 유도 → IMicroSessionHost.OnMicroExit → CancelSession()
            if (_micro) _micro.Exit();
            else CancelSession();
            return;
        }

        // 실패 체크
        if (_pressCount >= failAtPresses)
        {
            _ending = true;
            SetButtonsInteractable(false);

            // 소음 100%
            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(1f);
            OnFailed?.Invoke();

            if (_micro) _micro.Exit();
            else CancelSession();
        }
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

        // level0/1/2Local의 y만 사용 (x/z는 초기값 유지)
        float y = h switch
        {
            0 => level0Local.y,
            1 => level1Local.y,
            _ => level2Local.y
        };

        // 초기 배치한 x/z를 사용
        var basePos = _baseLocalPos[index];
        var target = new Vector3(basePos.x, y, basePos.z);

        // 중복 트윈 방지
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

#if UNITY_EDITOR
    void OnValidate()
    {
        failAtPresses = Mathf.Max(1, failAtPresses);
    }
#endif
}
