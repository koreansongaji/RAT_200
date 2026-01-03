using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MicroZoomSession : MonoBehaviour, IZoomStateProvider
{
    [Header("Cameras")]
    [SerializeField] CinemachineCamera benchCloseupCam;
    [SerializeField] CinemachineCamera microCloseupCam;

    [Header("UI")]
    [SerializeField] Button closeButton;

    [Header("Mixing (optional, legacy fallback)")]
    [SerializeField] ChemMixingStation mixingStation;
    [SerializeField] Canvas hintCanvas;

    [Header("Control Locks")]
    [SerializeField] bool lockPlayerWhileMicro = true;
    [SerializeField] bool showCursorWhileMicro = true;

    // ★ [New] 줌 상태에서 숨길 오브젝트 리스트 (사다리 등)
    [Header("Visibility Control")]
    [Tooltip("Micro Zoom 진입 시 잠시 숨길 오브젝트들 (예: 사다리)")]
    [SerializeField] GameObject[] objectsToHide;

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

        if (closeButton)
        {
            closeButton.onClick.AddListener(Exit);
            closeButton.gameObject.SetActive(false);
        }

        _researcher = FindFirstObjectByType<ResearcherController>();
    }

    public bool TryEnter(PlayerInteractor player)
    {
        if (_inMicro) return false;
        if (Time.unscaledTime < _blockUntil) return false;

        // PlayerReach 기반 거리 체크
        if (player != null)
        {
            float limitDistance = 2.0f;
            bool horizontalOnly = true;

            var reach = player.GetComponent<PlayerReach>();
            if (reach != null)
            {
                limitDistance = reach.radius;
                horizontalOnly = reach.horizontalOnly;
            }

            float currentDist;
            if (horizontalOnly)
            {
                Vector3 p1 = player.transform.position;
                Vector3 p2 = transform.position;
                p1.y = 0; p2.y = 0;
                currentDist = Vector3.Distance(p1, p2);
            }
            else
            {
                currentDist = Vector3.Distance(player.transform.position, transform.position);
            }

            if (currentDist > limitDistance + 0.1f)
            {
                Debug.Log($"[MicroZoom] 거리가 멉니다. (현재: {currentDist:F2} > 제한: {limitDistance})");
                return false;
            }
        }

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

        if (closeButton) closeButton.gameObject.SetActive(true);

        if (_pi != null)
        {
            var col = _pi.GetComponent<Collider>();
            if (col) col.enabled = false;
        }

        // ★ [New] 방해되는 오브젝트 숨기기
        if (objectsToHide != null)
        {
            foreach (var obj in objectsToHide)
            {
                if (obj) obj.SetActive(false);
            }
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

        StartCoroutine(Routine_ExitCleanup());

        if (_host != null) _host.OnMicroExit(_pi);
        else if (mixingStation) mixingStation.EndSessionFromExternal();

        OnExitMicro?.Invoke();
    }

    IEnumerator Routine_ExitCleanup()
    {
        yield return new WaitForEndOfFrame();

        if (lockPlayerWhileMicro) TogglePlayerControls(true);
        if (showCursorWhileMicro) SetCursor(true);
        if (hintCanvas) hintCanvas.enabled = false;
        if (closeButton) closeButton.gameObject.SetActive(false);

        if (_pi != null)
        {
            var col = _pi.GetComponent<Collider>();
            if (col) col.enabled = true;
        }

        // ★ [New] 숨겼던 오브젝트 다시 보이기
        if (objectsToHide != null)
        {
            foreach (var obj in objectsToHide)
            {
                if (obj) obj.SetActive(true);
            }
        }
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
    }

    void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}