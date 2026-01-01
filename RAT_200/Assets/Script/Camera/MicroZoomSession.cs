using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // [New] 버튼 제어를 위해 추가 필수!

public class MicroZoomSession : MonoBehaviour, IZoomStateProvider
{
    [Header("Cameras")]
    [SerializeField] CinemachineCamera benchCloseupCam;
    [SerializeField] CinemachineCamera microCloseupCam;

    [Header("UI")]
    [SerializeField] Button closeButton; // [New] 닫기(X) 버튼 연결용

    [Header("Mixing (optional, legacy fallback)")]
    [SerializeField] ChemMixingStation mixingStation;
    [SerializeField] Canvas hintCanvas;

    [Header("Control Locks")]
    [SerializeField] bool lockPlayerWhileMicro = true;
    [SerializeField] bool showCursorWhileMicro = true;

    public UnityEvent OnEnterMicro, OnExitMicro;

    [Header("Re-enter guard")]
    [SerializeField] float reenterBlockSec = 0.2f;

    [Header("Host Override (optional)")]
    [SerializeField] MonoBehaviour hostOverride;

    bool _inMicro;
    float _blockUntil;
    PlayerInteractor _pi;

    public bool InMicro => _inMicro;
    public bool InZoom => _inMicro;

    IMicroSessionHost _host;

    private ResearcherController _researcher;
    void Awake()
    {
        if (microCloseupCam) microCloseupCam.Priority = CloseupCamManager.MicroOff;

        _host = hostOverride as IMicroSessionHost
             ?? GetComponent<IMicroSessionHost>()
             ?? GetComponentInChildren<IMicroSessionHost>()
             ?? GetComponentInParent<IMicroSessionHost>();

        // [New] 버튼이 할당되어 있다면 클릭 이벤트 연결 & 시작 시 숨기기
        if (closeButton)
        {
            closeButton.onClick.AddListener(Exit); // 클릭하면 Exit() 함수 실행
            closeButton.gameObject.SetActive(false); // 평소엔 안 보임
        }

        _researcher = FindFirstObjectByType<ResearcherController>();
    }

    public bool TryEnter(PlayerInteractor player)
    {
        if (_inMicro) return false;
        if (Time.unscaledTime < _blockUntil) return false;

        if (_researcher != null && _researcher.CurrentState != ResearcherController.State.Idle)
        {
            return false;
        }

        if (_host != null && !_host.CanBeginMicro(player))
            return false;

        EnterImpl(player);
        return true;
    }

    void EnterImpl(PlayerInteractor player)
    {
        _inMicro = true;
        _pi = player;

        if (microCloseupCam) CloseupCamManager.ActivateMicro(microCloseupCam);

        if (lockPlayerWhileMicro) TogglePlayerControls(false);
        if (showCursorWhileMicro) SetCursor(true);
        if (hintCanvas) hintCanvas.enabled = true;

        // [New] 마이크로 줌 진입 시 X 버튼 켜기
        if (closeButton) closeButton.gameObject.SetActive(true);

        if (_pi != null)
        {
            var col = _pi.GetComponent<Collider>();
            if (col) col.enabled = false;
        }

        if (_host != null) _host.OnMicroEnter(_pi);
        else if (mixingStation) mixingStation.BeginSessionFromExternal();

        OnEnterMicro?.Invoke();
    }

    public void Exit()
    {
        if (!_inMicro) return;

        ExitImpl();
        _blockUntil = Time.unscaledTime + reenterBlockSec;
    }

    void ExitImpl()
    {
        _inMicro = false;

        if (microCloseupCam) CloseupCamManager.DeactivateMicro(microCloseupCam);

        if (lockPlayerWhileMicro) TogglePlayerControls(true);
        if (showCursorWhileMicro) SetCursor(true);
        if (hintCanvas) hintCanvas.enabled = false;

        // 여기서 즉시 끄지 않고, 코루틴으로 처리를 미룹니다.
        // 이렇게 해야 이번 프레임의 이동 판정(Update)에서 UI가 살아있는 것으로 인식되어
        // 바닥 클릭(이동)을 막을 수 있습니다.
        StartCoroutine(Routine_ExitCleanup());

        if (_host != null) _host.OnMicroExit(_pi);
        else if (mixingStation) mixingStation.EndSessionFromExternal();

        OnExitMicro?.Invoke();
    }

    IEnumerator Routine_ExitCleanup()
    {
        // 이번 프레임의 모든 렌더링/로직이 끝날 때까지 대기
        // (이동 스크립트의 Update보다 뒤에 실행됨)
        yield return new WaitForEndOfFrame();

        if (lockPlayerWhileMicro) TogglePlayerControls(true);
        if (showCursorWhileMicro) SetCursor(true);
        if (hintCanvas) hintCanvas.enabled = false;

        // 버튼도 이제서야 끕니다.
        if (closeButton) closeButton.gameObject.SetActive(false);

        if (_pi != null)
        {
            var col = _pi.GetComponent<Collider>();
            if (col) col.enabled = true;
        }
    }

    void Update()
    {
        if (_inMicro && Input.GetKeyDown(KeyCode.Escape))
            Exit();
    }

    // ... (아래 TogglePlayerControls, SetCursor 등은 기존 유지) ...
    void TogglePlayerControls(bool enable)
    {
        var input = _pi ? _pi.GetComponent<RatInput>() : null;
        if (input) input.enabled = enable;
    }

    void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}