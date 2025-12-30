using UnityEngine;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;

/// 4���̾3�� ���� ���� ��Ʈ�ѷ�.
/// - ��ǥ: ��� W�� ���� 0
/// - ��ư ����(���� ����): 
///   B1:(1,0,1,0)  B2:(0,1,0,1)  B3:(1,1,1,0)  B4:(0,1,1,1)
/// - �� ��ư�� ����޴� ���̾��� ���̸� +1(mod 3) ��Ŵ
/// - 8ȸ �̻� ������ ����(Noise 100%)
public class WirePuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    public bool hidePlayerDuringMicro = true; // ���񺰷� ���
    public bool HidePlayerDuringMicro => hidePlayerDuringMicro;

    [Header("Wires (Transforms at current piece root)")]
    public Transform w1;
    public Transform w2;
    public Transform w3;
    public Transform w4;

    [Header("Local snap positions for levels (0=����,1=��,2=�׺��� ��)")]
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

    [Header("UI (����)")]
    public TMP_Text txtMovesLeft;      // "Moves: 0/7" ��
    public TMP_Text[] wireDebugTexts;  // W1~W4 ���� ����� ǥ��(����)
    public Canvas worldCanvas;         // ���� �߿��� ON

    [Header("Rules")]
    [Tooltip("��ư ���� Ƚ�� 8 �̻��̸� ����")]
    public int failAtPresses = 8;      // 8 �̻��̸� ����
    [Tooltip("�ʱ� ���� (0/1/2)")]
    [Range(0, 2)] public int w1Init = 0;
    [Range(0, 2)] public int w2Init = 1;
    [Range(0, 2)] public int w3Init = 2;
    [Range(0, 2)] public int w4Init = 0;

    [Header("Events")]
    public UnityEvent OnSolved;
    public UnityEvent OnFailed;

    // ���� ����
    bool _session;
    int[] _h = new int[4];  // W1..W4
    int _pressCount;
    PlayerInteractor _lastPlayer;
    MicroZoomSession _micro;

    Vector3[] _baseLocalPos = new Vector3[4];

    // ��ư����̾� ���� (�ϵ��ڵ�: ���� ���� �״��)
    // rows: B1..B4, cols: W1..W4
    static readonly bool[,] INFL = new bool[4, 4] {
        { true,  false, true,  false }, // B1
        { false, true,  false, true  }, // B2
        { true,  true,  true,  false }, // B3
        { false, true,  true,  true  }  // B4
    };

    // Ŭ���� �ʵ忡 ���� �߰�
    bool _ending = false;

    
    private WirePuzzleSoundController _wirePuzzleSoundController;
    void Awake()
    {
        _micro = GetComponent<MicroZoomSession>();
        WireButtons();
        if (worldCanvas) worldCanvas.enabled = false;

        // �ʱ� ��ġ�� ĳ�� (�����Ϳ��� ��ġ�� �״�� ����)
        CacheBaseLocalPositions();

        if (TryGetComponent(out _wirePuzzleSoundController))
        {
            _wirePuzzleSoundController = gameObject.AddComponent<WirePuzzleSoundController>();
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

    // ===== BaseInteractable =====
    public override bool CanInteract(PlayerInteractor i)
    {
        // ���� �� �ߺ� ���� ����
        return !_session;
    }

    public override void Interact(PlayerInteractor i)
    {
        // Micro ���� ��Ʈ���� ���� MicroEntryInteractable�� �ᵵ �ǰ�,
        // ������ ���� Micro�� ��� ��. ���⼭�� Micro�� �پ��ִٸ� �������� ���Ը� ����.
        if (_session) return;
        _lastPlayer = i;
        if (_micro && _micro.TryEnter(i)) return;
        // Micro�� ���� �� ���� ����
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

        // Ȥ�� ��Ÿ�ӿ� ������ ���ġ���� �� ������ ���� ���� ���� �� �� �� ĳ��
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

        // ��ư ���� ���� (+1 mod 3)
        for (int w = 0; w < 4; w++)
            if (INFL[bIndex, w])
                _h[w] = (_h[w] + 1) % 3;

        _pressCount++;

        SnapAll();
        RefreshTexts();

        // ���� üũ
        if (_h[0] == 0 && _h[1] == 0 && _h[2] == 0 && _h[3] == 0)
        {
            _ending = true;
            SetButtonsInteractable(false);
            OnSolved?.Invoke();

            // 공용 사운드 재생: 와이어 퍼즐 성공 (스파크 소리)
            CommonSoundController.Instance?.PlaySpark();

            // Micro ���� ���� �� IMicroSessionHost.OnMicroExit �� CancelSession()
            if (_micro) _micro.Exit();
            else CancelSession();
            return;
        }

        // ���� üũ
        if (_pressCount >= failAtPresses)
        {
            _ending = true;
            SetButtonsInteractable(false);

            // ���� 100%
            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(1f);
            OnFailed?.Invoke();

            // 공용 사운드 재생: 와이어 퍼즐 실패 (퓨즈 스파크 소리)
            CommonSoundController.Instance?.PlayPuzzleFail();

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

        // level0/1/2Local�� y�� ��� (x/z�� �ʱⰪ ����)
        float y = h switch
        {
            0 => level0Local.y,
            1 => level1Local.y,
            _ => level2Local.y
        };

        // �ʱ� ��ġ�� x/z�� ���
        var basePos = _baseLocalPos[index];
        var target = new Vector3(basePos.x, y, basePos.z);

        // �ߺ� Ʈ�� ����
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
