using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class ResearcherController : MonoBehaviour
{
    public enum State
    {
        Idle, SummonIntro, Searching, Focusing, Capture, BusyWithEvent
    }

    [Header("Refs")]
    public Light roomMainLight;
    public Light spotLight;
    public Transform eyePivot;
    public Transform doorHinge;
    public Transform player;

    [Tooltip("연구원이 문을 열고 등장하면 꺼질(Disable) Outline Volume 오브젝트")]
    public GameObject outlineVolumeObject;

    [Header("Optional Target")]
    public Transform npcTarget;

    [Header("Events")]
    public UnityEvent OnSummonStarted;
    public UnityEvent OnIntroFinished;
    public UnityEvent OnSearchEnded;
    public UnityEvent OnPlayerCaught; // ★ 여기서는 호출 시점을 Manager에게 넘기거나 제거합니다.
    public UnityEvent OnNpcCaught;

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

    [Header("Sprite")]
    public SimpleColorChanger sprite1;
    public SimpleColorChanger sprite2;

    [Header("Game Over Event")]
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

    // ... (Start, OnEnable, OnDisable, SubscribeNoiseSystem, HandleNoiseChanged, StartSummon, SetOutlineVolume, Update 등은 기존 유지) ...
    // 코드가 너무 길어지므로 변경되지 않은 윗부분은 생략합니다. 기존 코드 그대로 두세요.

    void Start()
    {
        SubscribeNoiseSystem();
        if (spotLight) spotLight.enabled = false;
        if (roomCenter) _scanTargetPos = roomCenter.position;
        if (_summonAlertClip == null) _summonAlertClip = Resources.Load<AudioClip>("Sounds/Effect/Researcher/alert");
        SetOutlineVolume(true);
    }

    // ... (중간 생략: StartSummon, Update, UpdateSummonIntro, UpdateSearching, UpdateFocusing, DetectTargets, CheckVisibility 등) ...
    // 기존 Update 함수들은 그대로 유지.

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
        _pendingFocusPos = null;

        if (_summonAlertClip != null) AudioManager.Instance.Play(_summonAlertClip, AudioManager.Sound.Effect, 1.0f, 0.8f);

        OnSummonStarted?.Invoke();
        Debug.Log("[Researcher] 소음 감지! 연구원 접근 중.");
    }

    void SetOutlineVolume(bool active)
    {
        if (outlineVolumeObject) outlineVolumeObject.SetActive(active);
    }

    void Update()
    {
        if (_state != State.Idle)
        {
            if (NoiseSystem.Instance != null) NoiseSystem.Instance.SetLevel01(1.0f);
        }

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
            case State.BusyWithEvent: break;
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
            SetOutlineVolume(false); // 아웃라인 끄기

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
        DetectTargets();
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
        if (CheckVisibility(player)) { StartCapturePlayer(); return; }
        if (npcTarget && CheckVisibility(npcTarget)) { StartCaptureNPC(); return; }
    }

    bool CheckVisibility(Transform target)
    {
        // ★ [핵심 수정] 타겟이 없거나, 비활성화(죽음) 상태라면 안 보이는 것으로 처리
        if (!target || !target.gameObject.activeInHierarchy) return false;

        Vector3 toTarget = target.position - eyePivot.position;
        float dist = toTarget.magnitude;

        float angle = Vector3.Angle(eyePivot.forward, toTarget);
        if (angle > spotLight.spotAngle * 0.5f) return false;

        // 장애물 체크
        if (Physics.Raycast(eyePivot.position, toTarget.normalized, dist, obstacleMask))
            return false;

        return true;
    }

    // ★ [핵심 수정] 플레이어 포획 시퀀스
    void StartCapturePlayer()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;

        //OnPlayerCaught?.Invoke();

        // 상태만 변경해두고 (중복 방지)
        _state = State.Capture;
        _scanTween?.Kill();
        _currentFocusTarget = player;

        Debug.Log("[Researcher] 플레이어 잡음 -> 매니저에게 처형 요청");

        // 모든 연출을 매니저에게 위임 (코드 재사용)
        if (GameLoopManager.Instance)
        {
            GameLoopManager.Instance.TriggerDeath(player.gameObject);
        }
    }

    void StartCaptureNPC()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;

        _state = State.BusyWithEvent;
        _scanTween?.Kill();
        _currentFocusTarget = npcTarget;

        Debug.Log("[Researcher] 동료 잡음 -> 매니저에게 처형 요청");

        if (GameLoopManager.Instance && npcTarget)
        {
            GameLoopManager.Instance.TriggerDeath(npcTarget.gameObject);
        }

        // ★ [핵심 수정] 잡았으니 이제 타겟 변수를 비워줍니다.
        // 그래야 나중에 연구원이 다시 활동할 때, 죽은 동료를 또 쳐다보지 않습니다.
        npcTarget = null;

        //OnNpcCaught?.Invoke();
    }

    public void ForceLeave()
    {
        _state = State.Idle;
        _currentFocusTarget = null;
        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);
        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());
        if (NoiseSystem.Instance) NoiseSystem.Instance.SetLevel01(0f);
        SetOutlineVolume(true);
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
        SetOutlineVolume(true);
    }

    System.Collections.IEnumerator Routine_FlickerLightOn()
    {
        yield return new WaitForSeconds(0.5f);
        roomMainLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.1f);
        roomMainLight.enabled = true;
        if (sprite1) sprite1.ChangeToNormal();
        if (sprite2) sprite2.ChangeToNormal();
    }
}