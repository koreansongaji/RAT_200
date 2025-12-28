using UnityEngine;

[RequireComponent(typeof(MicroZoomSession))]
public class KeypadInteractable : BaseInteractable, IMicroSessionHost
{
    [Header("Ref")]
    public KeypadController keypadLogic; // 로직 스크립트 (자동 연결 시도함)

    [Header("Buttons (3D Models)")]
    [Tooltip("숫자 버튼들에 붙어있는 콜라이더들을 여기에 몽땅 넣으세요.")]
    public Collider[] buttonColliders;

    private MicroZoomSession _micro;
    private bool _isSolved = false;

    void Awake()
    {
        _micro = GetComponent<MicroZoomSession>();

        // 만약 인스펙터 연결 까먹었으면 자동으로 찾아줌
        if (!keypadLogic) keypadLogic = GetComponent<KeypadController>();

        // 게임 시작 시 버튼들 클릭 못하게 막기
        SetButtonsInteractable(false);
    }

    void Start()
    {
        // ★ 핵심: 정답 맞추면 자동으로 'OnPuzzleSolved' 함수가 실행되게 연결
        if (keypadLogic)
        {
            keypadLogic.OnCorrectPassword.AddListener(OnPuzzleSolved);
        }
    }

    // --- [1] 상호작용 (전체 키패드 클릭 -> 줌인) ---
    public override bool CanInteract(PlayerInteractor i)
    {
        // 이미 풀린 상태면 줌인 안 함
        return !_isSolved;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (_micro) _micro.TryEnter(i);
    }

    // --- [2] 마이크로 세션 호스트 ---
    public bool CanBeginMicro(PlayerInteractor player)
    {
        return !_isSolved;
    }

    public void OnMicroEnter(PlayerInteractor player)
    {
        // 줌인 했으니 버튼 잠금 해제
        SetButtonsInteractable(true);
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        // 줌아웃 했으니 버튼 잠금
        SetButtonsInteractable(false);
    }

    // --- [3] 기능 함수 ---

    // ★ 정답 맞췄을 때 자동으로 호출됨
    public void OnPuzzleSolved()
    {
        _isSolved = true;

        // 1. 더 이상 버튼 못 누르게 즉시 잠금
        SetButtonsInteractable(false);

        // 2. "성공" 메시지를 볼 수 있게 0.5초만 기다렸다가 줌아웃
        Invoke("ExitSession", 0.5f);
    }

    void ExitSession()
    {
        if (_micro) _micro.Exit();
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (buttonColliders == null) return;
        foreach (var col in buttonColliders)
        {
            if (col) col.enabled = interactable;
        }
    }
}