using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MicroZoomSession : MonoBehaviour, IZoomStateProvider
{
    // ▼ [모드 선택 기능]
    public enum EntryMode
    {
        Distance,   // 기존 방식: PlayerReach 거리 내에서만 진입
        Zone        // 구역 방식: 특정 Trigger 안에 있으면 거리 무관 진입
    }

    [Header("Entry Settings")]
    public EntryMode entryMode = EntryMode.Distance; // 기본값은 거리
    public Collider zoneCollider;                    // Zone 모드일 때 사용할 콜라이더 (CameraZoneTrigger 등)

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

    private bool _isExitLocked = false;

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

    public void SetExitLocked(bool locked)
    {
        _isExitLocked = locked;
        // UI 버튼(closeButton)도 잠금 상태에 따라 비활성화하면 좋습니다.
        if (closeButton) closeButton.interactable = !locked;
    }

    // ▼ [핵심] 플레이어가 Zone 안에 있는지 검사
    public bool IsPlayerInsideZone(Vector3 playerPos)
    {
        // Distance 모드거나 콜라이더가 없으면 Zone 판정은 False
        if (entryMode == EntryMode.Distance || zoneCollider == null) return false;

        // ClosestPoint를 사용하여 플레이어 위치가 콜라이더 내부인지 확인
        Vector3 closest = zoneCollider.ClosestPoint(playerPos);
        return Vector3.Distance(playerPos, closest) < 0.05f; // 약간의 오차 허용
    }

    public bool TryEnter(PlayerInteractor player)
    {
        if (_inMicro) return false;
        if (Time.unscaledTime < _blockUntil) return false;

        // 1. 연구원 상태 체크 (Busy하면 진입 불가)
        if (_researcher != null && _researcher.CurrentState != ResearcherController.State.Idle)
        {
            return false;
        }

        // 2. Host 조건 체크 (퍼즐 로직상 진입 가능한지)
        if (_host != null && !_host.CanBeginMicro(player))
            return false;

        // 3. 거리 vs Zone 모드에 따른 진입 판정 분기
        bool canEnter = false;

        if (entryMode == EntryMode.Zone)
        {
            // [Zone 모드] 플레이어가 Zone 안에 있으면 거리 무시하고 통과
            if (player != null && IsPlayerInsideZone(player.transform.position))
            {
                canEnter = true;
            }
            else
            {
                Debug.Log("[MicroZoom] Zone 모드인데 플레이어가 구역 밖에 있습니다.");
            }
        }
        else
        {
            // [Distance 모드] 기존 거리 계산 로직 유지
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

                if (currentDist <= limitDistance + 0.1f)
                {
                    canEnter = true;
                }
                else
                {
                    Debug.Log($"[MicroZoom] 거리가 멉니다. (현재: {currentDist:F2} > 제한: {limitDistance})");
                }
            }
        }

        // 판정 결과 실패면 리턴
        if (!canEnter) return false;

        // 성공하면 진입 로직 실행
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
        if (_isExitLocked) return;
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