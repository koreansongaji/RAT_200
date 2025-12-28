using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class ResearcherController : MonoBehaviour
{
    public enum State
    {
        Idle,
        SummonIntro,
        Searching,
        Focusing,
        Capture,
        BusyWithEvent // ★ [추가] 이벤트 중일 때 다른 행동 안 함
    }

    [Header("Refs")]
    public Light roomMainLight;
    public Light spotLight;
    public Transform eyePivot;
    public Transform doorHinge;
    public Transform player;

    [Header("Optional Target")]
    [Tooltip("플레이어 말고도 감지할 NPC가 있다면 연결.")]
    public Transform npcTarget;

    [Header("Events")]
    public UnityEvent OnSummonStarted;
    public UnityEvent OnIntroFinished;
    public UnityEvent OnSearchEnded;
    public UnityEvent OnPlayerCaught; // 플레이어 발견 시
    public UnityEvent OnNpcCaught;    // ★ [추가] NPC 발견 시

    [Header("Settings")]
    public Vector3 doorOpenEuler = new Vector3(0, 90, 0);
    public Vector3 doorClosedEuler = Vector3.zero;
    public float introDelay = 5f;
    public float searchDuration = 30f;
    public float focusDuration = 5f;
    public LayerMask obstacleMask;
    public float catchDistance = 1.0f;
    public Transform roomCenter;
    public Vector2 scanAreaSize = new Vector2(8f, 8f);
    public float scanMoveSpeed = 3f;

    [Header("Game Over")]
    public Transform handModel;
    public UnityEvent OnGameOver;

    State _state = State.Idle;
    float _stateTimer;
    Vector3 _scanTargetPos;
    Tween _scanTween;
    bool _subscribed;
    Transform _currentFocusTarget; // 현재 쳐다보고 있는 대상 (플레이어 or NPC)

    void Start()
    {
        SubscribeNoiseSystem();
        if (spotLight) spotLight.enabled = false;
        if (handModel) handModel.gameObject.SetActive(false);
        if (roomCenter) _scanTargetPos = roomCenter.position;
    }

    void OnEnable() => SubscribeNoiseSystem();
    void OnDisable()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && _subscribed) { ns.OnValueChanged -= HandleNoiseChanged; _subscribed = false; }
        _scanTween?.Kill();
    }

    void SubscribeNoiseSystem()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && !_subscribed) { ns.OnValueChanged += HandleNoiseChanged; _subscribed = true; }
    }

    void HandleNoiseChanged(float value01)
    {
        if (_state != State.Idle) return;
        if (value01 >= 0.999f) StartSummon();
    }

    public void StartSummon()
    {
        if (_state != State.Idle) return;
        _state = State.SummonIntro;
        _stateTimer = 0f;
        OnSummonStarted?.Invoke();
    }

    void Update()
    {
        // 시선 처리 (타겟이 있으면 타겟을, 없으면 바닥 스캔 지점을)
        Vector3 lookPos = _currentFocusTarget ? _currentFocusTarget.position : _scanTargetPos;

        if (eyePivot)
        {
            Vector3 dir = lookPos - eyePivot.position;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                eyePivot.rotation = Quaternion.Slerp(eyePivot.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        switch (_state)
        {
            case State.SummonIntro: UpdateSummonIntro(); break;
            case State.Searching: UpdateSearching(); break;
            case State.Focusing: UpdateFocusing(); break;
            case State.BusyWithEvent: /* 이벤트 감독이 알아서 함 */ break;
        }
    }

    void UpdateSummonIntro()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= introDelay)
        {
            if (doorHinge) doorHinge.DOLocalRotate(doorOpenEuler, 0.5f).SetEase(Ease.OutBack);
            if (spotLight) spotLight.enabled = true;
            if (roomMainLight) roomMainLight.enabled = false;
            OnIntroFinished?.Invoke();

            _state = State.Searching;
            _stateTimer = 0f;
            PickNextRandomScanPoint();
        }
    }

    void UpdateSearching()
    {
        _stateTimer += Time.deltaTime;
        DetectTargets(); // ★ 플레이어 & NPC 감지
        if (_stateTimer >= searchDuration) EndSearchAndResetNoise();
    }

    void UpdateFocusing()
    {
        _stateTimer += Time.deltaTime;
        DetectTargets(); // ★ 조사 중에도 감지
        if (_stateTimer >= focusDuration)
        {
            _state = State.Searching;
            _stateTimer = 0f;
            PickNextRandomScanPoint();
        }
    }

    // ▼▼▼ [핵심 수정] 타겟 감지 로직 ▼▼▼
    void DetectTargets()
    {
        if (!spotLight) return;

        // 1순위: 플레이어 체크 (걸리면 게임오버)
        if (CheckVisibility(player))
        {
            StartCapturePlayer();
            return;
        }

        // 2순위: NPC 체크 (걸리면 이벤트)
        if (npcTarget && CheckVisibility(npcTarget))
        {
            StartCaptureNPC();
            return;
        }
    }

    // 시야 체크 헬퍼 함수
    bool CheckVisibility(Transform target)
    {
        if (!target) return false;
        Vector3 toTarget = target.position - eyePivot.position;
        float dist = toTarget.magnitude;

        // 각도 체크
        float angle = Vector3.Angle(eyePivot.forward, toTarget);
        if (angle > spotLight.spotAngle * 0.5f) return false;

        // 장애물 체크
        if (Physics.Raycast(eyePivot.position, toTarget.normalized, dist, obstacleMask))
        {
            return false; // 막힘 (숨음 성공)
        }
        return true; // 보임
    }

    void StartCapturePlayer()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;
        _state = State.Capture;
        _scanTween?.Kill();

        _currentFocusTarget = player; // 플레이어 고정
        OnPlayerCaught?.Invoke();

        if (handModel)
        {
            handModel.gameObject.SetActive(true);
            handModel.DOMove(player.position, 0.4f).SetEase(Ease.InExpo).OnComplete(() => OnGameOver?.Invoke());
        }
        else OnGameOver?.Invoke();
    }

    void StartCaptureNPC()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;

        // ★ NPC 발견! -> 이벤트 모드로 전환
        _state = State.BusyWithEvent;
        _scanTween?.Kill();

        _currentFocusTarget = npcTarget; // 시선을 NPC에 고정 (따라다님)
        OnNpcCaught?.Invoke(); // 감독에게 신호 보냄
    }

    // ▼▼▼ [추가] 이벤트 감독이 쓸 함수들 ▼▼▼

    // 연구원 강제 퇴근 (이벤트 종료 시 호출)
    public void ForceLeave()
    {
        _state = State.Idle;
        _currentFocusTarget = null;
        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);
        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());
    }

    void PickNextRandomScanPoint()
    {
        if (_state != State.Searching) return;
        if (!roomCenter) return;
        float rx = Random.Range(-scanAreaSize.x * 0.5f, scanAreaSize.x * 0.5f);
        float rz = Random.Range(-scanAreaSize.y * 0.5f, scanAreaSize.y * 0.5f);
        Vector3 nextPos = roomCenter.position + new Vector3(rx, 0, rz);
        float dist = Vector3.Distance(_scanTargetPos, nextPos);

        _scanTween = DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, nextPos, dist / scanMoveSpeed)
                            .SetEase(Ease.InOutSine).OnComplete(PickNextRandomScanPoint);
    }

    public void NotifyNoiseEvent(Vector3 worldPos)
    {
        if (_state == State.Searching || _state == State.Focusing)
        {
            _state = State.Focusing;
            _stateTimer = 0f;
            _scanTween?.Kill();
            DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, worldPos, 0.5f).SetEase(Ease.OutCubic);
        }
    }

    void EndSearchAndResetNoise()
    {
        _state = State.Idle;
        _scanTween?.Kill();
        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);
        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());
        if (NoiseSystem.Instance) NoiseSystem.Instance.SetLevel01(0.2f);
    }

    System.Collections.IEnumerator Routine_FlickerLightOn()
    {
        yield return new WaitForSeconds(0.5f);
        roomMainLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.1f);
        roomMainLight.enabled = true;
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying && spotLight && _currentFocusTarget == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePivot.position, _scanTargetPos);
            Gizmos.DrawSphere(_scanTargetPos, 0.2f);
        }
    }
}