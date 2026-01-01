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
        BusyWithEvent // 이벤트 진행 중 (수색 중단)
    }

    [Header("Refs")]
    public Light roomMainLight;
    public Light spotLight;
    public Transform eyePivot;
    public Transform doorHinge;
    public Transform player;

    [Header("Optional Target")]
    [Tooltip("플레이어 외에 수색할 NPC가 있다면 설정.")]
    public Transform npcTarget;

    [Header("Events")]
    public UnityEvent OnSummonStarted;
    public UnityEvent OnIntroFinished;
    public UnityEvent OnSearchEnded;
    public UnityEvent OnPlayerCaught; // 플레이어 발견 시
    public UnityEvent OnNpcCaught;    // NPC 발견 시 -> Director가 받아서 처리

    [Header("Settings")]
    public Vector3 doorOpenEuler = new Vector3(0, 90, 0);
    public Vector3 doorClosedEuler = Vector3.zero;
    public float introDelay = 5f;
    public float searchDuration = 30f;
    public float focusDuration = 5f;
    public LayerMask obstacleMask;
    public Transform roomCenter;
    public Vector2 scanAreaSize = new Vector2(8f, 8f);
    public float scanMoveSpeed = 3f;

    [Header("Game Over")]
    public UnityEvent OnGameOver;

    [Header("Sfx Clips")]
    [SerializeField] private AudioClip _summonAlertClip;

    State _state = State.Idle;

    public State CurrentState => _state;
        
    float _stateTimer;
    Vector3 _scanTargetPos;
    Tween _scanTween;
    bool _subscribed;
    Transform _currentFocusTarget;
    Vector3? _pendingFocusPos = null;

    void Start()
    {
        SubscribeNoiseSystem();
        if (spotLight) spotLight.enabled = false;
        if (roomCenter) _scanTargetPos = roomCenter.position;
        if (_summonAlertClip == null) _summonAlertClip = Resources.Load<AudioClip>("Sounds/Effect/Researcher/alert");
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

    // ★ 소음이 100(1.0)에 도달하면 소환!
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
        _pendingFocusPos = null;

        if (_summonAlertClip != null) AudioManager.Instance.Play(_summonAlertClip, AudioManager.Sound.Effect, 1.0f, 0.8f);

        OnSummonStarted?.Invoke();
        Debug.Log("[Researcher] 소음 감지! 연구원 소환 시작.");
    }

    void Update()
    {
        // ★ 연구원이 집에 있는 동안은 긴장감 유지를 위해 Noise 100 고정
        if (_state != State.Idle)
        {
            if (NoiseSystem.Instance != null) NoiseSystem.Instance.SetLevel01(1.0f);
        }

        // 시선 처리
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
            case State.BusyWithEvent: /* 이벤트 중엔 아무것도 안 함 (Director가 연출) */ break;
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

            if (_pendingFocusPos.HasValue)
            {
                _state = State.Focusing;
                _stateTimer = 0f;
                _scanTargetPos = _pendingFocusPos.Value;
                _pendingFocusPos = null;
            }
            else
            {
                _state = State.Searching;
                _stateTimer = 0f;
                PickNextRandomScanPoint();
            }
        }
    }

    void UpdateSearching()
    {
        _stateTimer += Time.deltaTime;
        DetectTargets(); // 타겟 감지
        if (_stateTimer >= searchDuration) EndSearchAndResetNoise();
    }

    void UpdateFocusing()
    {
        _stateTimer += Time.deltaTime;
        DetectTargets();
        if (_stateTimer >= focusDuration)
        {
            _state = State.Searching;
            _stateTimer = 0f;
            PickNextRandomScanPoint();
        }
    }

    void DetectTargets()
    {
        if (!spotLight) return;

        // 1. 플레이어 먼저 감지 (우선순위 높음) -> 게임 오버
        if (CheckVisibility(player))
        {
            StartCapturePlayer();
            return;
        }

        // 2. 동료 NPC 감지 -> 이벤트(사망 연출)
        if (npcTarget && CheckVisibility(npcTarget))
        {
            StartCaptureNPC();
            return;
        }
    }

    bool CheckVisibility(Transform target)
    {
        if (!target) return false;
        Vector3 toTarget = target.position - eyePivot.position;
        float dist = toTarget.magnitude;

        float angle = Vector3.Angle(eyePivot.forward, toTarget);
        if (angle > spotLight.spotAngle * 0.5f) return false;

        if (Physics.Raycast(eyePivot.position, toTarget.normalized, dist, obstacleMask))
            return false;

        return true;
    }

    void StartCapturePlayer()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;
        _state = State.Capture;
        _scanTween?.Kill();
        _currentFocusTarget = player;
        OnPlayerCaught?.Invoke();

        if (GameLoopManager.Instance) GameLoopManager.Instance.TriggerGameOver();
        else OnGameOver?.Invoke();
    }

    void StartCaptureNPC()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;

        // ★ 상태를 이벤트 중으로 변경하여 수색 루프를 멈춤
        _state = State.BusyWithEvent;
        _scanTween?.Kill();

        _currentFocusTarget = npcTarget; // 시선을 NPC에 고정
        OnNpcCaught?.Invoke(); // -> Director에게 신호 보냄
    }

    public void ForceLeave()
    {
        _state = State.Idle;
        _currentFocusTarget = null;
        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);
        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());

        // ★ 퇴근 시 소음 초기화
        if (NoiseSystem.Instance) NoiseSystem.Instance.SetLevel01(0f);
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

    void EndSearchAndResetNoise()
    {
        OnSearchEnded?.Invoke();
        _state = State.Idle;
        _scanTween?.Kill();
        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);
        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());
        if (NoiseSystem.Instance) NoiseSystem.Instance.SetLevel01(0.0f);
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
}