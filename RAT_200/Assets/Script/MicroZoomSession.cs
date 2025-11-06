using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class MicroZoomSession : MonoBehaviour, IZoomStateProvider
{
    [Header("Cameras")]
    [SerializeField] CinemachineCamera benchCloseupCam; // (옵션, 안쓰면 무시)
    [SerializeField] CinemachineCamera microCloseupCam; // 0↔30

    [Header("Mixing (optional, legacy fallback)")]
    [SerializeField] ChemMixingStation mixingStation;   // Host 없을 때만 폴백
    [SerializeField] Canvas hintCanvas;                 // 힌트 패널 on/off

    [Header("Control Locks")]
    [SerializeField] bool lockPlayerWhileMicro = true;  // 플레이어 입력 잠금
    [SerializeField] bool showCursorWhileMicro = true;  // 마이크로 동안 커서 표시

    public UnityEvent OnEnterMicro, OnExitMicro;

    [Header("Re-enter guard")]
    [SerializeField] float reenterBlockSec = 0.2f;      // ESC 직후 재진입 차단

    [Header("Host Override (optional)")]
    [SerializeField] MonoBehaviour hostOverride;         // 특정 오브젝트를 호스트로 강제할 때

    bool _inMicro;
    float _blockUntil;
    PlayerInteractor _pi;

    public bool InMicro => _inMicro; // 기존 참조용
    public bool InZoom => _inMicro;  // IZoomStateProvider 구현

    IMicroSessionHost _host;

    void Awake()
    {
        if (microCloseupCam) microCloseupCam.Priority = CloseupCamManager.MicroOff;

        _host = hostOverride as IMicroSessionHost
             ?? GetComponent<IMicroSessionHost>()
             ?? GetComponentInChildren<IMicroSessionHost>()
             ?? GetComponentInParent<IMicroSessionHost>();
    }

    public bool TryEnter(PlayerInteractor player)
    {
        if (_inMicro) return false;
        if (Time.unscaledTime < _blockUntil) return false;

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

        if (_host != null) _host.OnMicroExit(_pi);
        else if (mixingStation) mixingStation.EndSessionFromExternal();

        OnExitMicro?.Invoke();
    }

    void Update()
    {
        if (_inMicro && Input.GetKeyDown(KeyCode.Escape))
            Exit();
    }

    void TogglePlayerControls(bool enable)
    {
        var input = _pi ? _pi.GetComponent<RatInput>() : null;
        if (input) input.enabled = enable;

        // Micro 중에도 다이얼/버튼 같은 IInteractable을 눌러야 하므로
        // ClickMoveOrInteract_Events는 비활성화하지 않는다.
        var clicker = _pi ? _pi.GetComponent<ClickMoveOrInteract_Events>() : null;
        // if (clicker) clicker.enabled = enable; // 비활성화 금지
    }

    void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
