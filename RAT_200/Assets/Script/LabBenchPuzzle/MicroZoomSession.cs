// using들 동일
using Unity.Cinemachine;
using UnityEngine.Events;
using UnityEngine;

public class MicroZoomSession : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] CinemachineCamera benchCloseupCam; // 20 유지
    [SerializeField] CinemachineCamera microCloseupCam; // 0↔30

    [Header("Mixing (optional)")]
    [SerializeField] ChemMixingStation mixingStation;
    [SerializeField] Canvas hintCanvas;

    [Header("Control Locks")]
    [SerializeField] bool lockPlayerWhileMicro = true;
    [SerializeField] bool showCursorWhileMicro = true;

    public UnityEvent OnEnterMicro, OnExitMicro;

    [Header("Re-enter guard")]
    [SerializeField] float reenterBlockSec = 0.2f;

    bool _inMicro;
    float _blockUntil;           // ESC 직후 재진입 차단 타임스탬프
    PlayerInteractor _pi;

    void Awake()
    {
        if (microCloseupCam) microCloseupCam.Priority = CloseupCamManager.MicroOff;
        if (hintCanvas) hintCanvas.enabled = false;
    }

    void Update()
    {
        if (_inMicro && Input.GetKeyDown(KeyCode.Escape))
            ExitMicro();
    }

    // ✅ ChemMixingStation이 호출하는 진입 API
    public bool TryEnterMicro(PlayerInteractor i)
    {
        if (_inMicro || !i) return false;
        //if (Time.unscaledTime < _blockUntil) return false;
        _pi = i;
        EnterMicro();
        return true;
    }

    void EnterMicro()
    {
        _inMicro = true;

        if (microCloseupCam) CloseupCamManager.ActivateMicro(microCloseupCam);

        if (hintCanvas) hintCanvas.enabled = true;
        if (lockPlayerWhileMicro) TogglePlayerControls(false);
        if (showCursorWhileMicro) SetCursor(true);

        if (mixingStation) mixingStation.BeginSessionFromExternal();

        OnEnterMicro?.Invoke();
        Debug.Log("[MicroZoomSession] Enter Micro View");
    }

    public void ExitMicro()
    {
        _inMicro = false;

        if (microCloseupCam) CloseupCamManager.DeactivateMicro(microCloseupCam);

        if (hintCanvas) hintCanvas.enabled = false;
        if (lockPlayerWhileMicro) TogglePlayerControls(true);
        if (!showCursorWhileMicro) SetCursor(false);

        _blockUntil = Time.unscaledTime + reenterBlockSec;

        if (mixingStation) mixingStation.EndSessionFromExternal();

        OnExitMicro?.Invoke();

        Debug.Log("[MicroZoomSession] Exit Micro View -> Bench Closeup");
    }

    void TogglePlayerControls(bool enable)
    {
        var input = _pi ? _pi.GetComponent<RatInput>() : null;
        if (input) input.enabled = enable;
        var clicker = _pi ? _pi.GetComponent<ClickMoveOrInteract_Events>() : null;
        if (clicker) clicker.enabled = enable;
    }

    void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
