using UnityEngine;
using UnityEngine.Events;
using DG.Tweening; // DOTween 필수

public class ResearcherController : MonoBehaviour
{
    public enum State
    {
        Idle,           // 대기
        SummonIntro,    // 100% 도달 후 5초간 대기 (발소리 등)
        Searching,      // 30초 일반 수색 (랜덤/패턴)
        Focusing,       // 특정 소음 위치 응시
        Capture         // 플레이어 발견 (게임오버)
    }

    [Header("Refs")]
    [Tooltip("방 전체를 비추는 조명 (연구원 등장 시 꺼짐).")]
    public Light roomMainLight;
    [Tooltip("실제 Spot Light 컴포넌트.")]
    public Light spotLight;
    [Tooltip("빛이 회전할 중심축 (문 위쪽). Eye Pivot.")]
    public Transform eyePivot;
    [Tooltip("문 힌지 (열고 닫기용).")]
    public Transform doorHinge;
    [Tooltip("플레이어 Transform (감지용).")]
    public Transform player;

    [Header("Settings - Door")]
    public Vector3 doorOpenEuler = new Vector3(0, 90, 0);
    public Vector3 doorClosedEuler = Vector3.zero;

    [Header("Settings - Timing")]
    public float introDelay = 5f;        // 5초 대기
    public float searchDuration = 30f;   // 30초 수색
    public float focusDuration = 5f;     // 5초 응시
    [Range(0f, 1f)] public float noiseResetLevel = 0.2f; // 복귀 시 20%

    [Header("Settings - Detection")]
    public LayerMask obstacleMask;       // 숨을 수 있는 장애물 레이어
    public float catchDistance = 1.0f;   // (옵션) 너무 가까우면 즉시 잡힘

    [Header("Settings - Scanning (Floor Target)")]
    [Tooltip("방 바닥 중앙.")]
    public Transform roomCenter;
    [Tooltip("수색 범위 (가로/세로).")]
    public Vector2 scanAreaSize = new Vector2(8f, 8f);
    [Tooltip("빛이 이동하는 속도.")]
    public float scanMoveSpeed = 3f;

    [Header("Game Over")]
    public Transform handModel; // 잡으러 오는 손
    public UnityEvent OnGameOver; // 게임 오버 이벤트

    [Header("Events")]
    public UnityEvent OnSummonStarted;    // 100% 도달 (인트로 시작)
    public UnityEvent OnIntroFinished;    // 5초 뒤 (문 열림, 수색 시작)
    public UnityEvent OnSearchEnded;      // 수색 종료 (문 닫힘)
    public UnityEvent OnPlayerCaught;     // 플레이어 발견

    // Internal State
    State _state = State.Idle;
    float _stateTimer;
    Vector3 _scanTargetPos; // 빛이 바라보는 바닥의 가상 지점
    Tween _scanTween;
    bool _subscribed;

    // 등장 직후 조사할 위치를 기억하는 변수
    Vector3? _pendingFocusPos = null;

    void Start()
    {
        SubscribeNoiseSystem();
        if (spotLight) spotLight.enabled = false;
        if (handModel) handModel.gameObject.SetActive(false);

        // 초기 타겟은 방 중앙으로
        if (roomCenter) _scanTargetPos = roomCenter.position;
    }

    void OnEnable() => SubscribeNoiseSystem();
    void OnDisable()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && _subscribed)
        {
            ns.OnValueChanged -= HandleNoiseChanged;
            _subscribed = false;
        }
        _scanTween?.Kill();
    }

    void SubscribeNoiseSystem()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && !_subscribed)
        {
            ns.OnValueChanged += HandleNoiseChanged;
            _subscribed = true;
        }
    }

    // ===== NoiseSystem 콜백 =====
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
        OnSummonStarted?.Invoke(); // 여기서 발자국 소리 재생
        Debug.Log("[Researcher] Summon Started (Wait 5s)");
    }

    void Update()
    {
        // 빛이 항상 타겟을 바라보도록 회전 (자연스러운 3축 회전)
        if (eyePivot && (_state == State.Searching || _state == State.Focusing))
        {
            Vector3 dir = _scanTargetPos - eyePivot.position;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            eyePivot.rotation = Quaternion.Slerp(eyePivot.rotation, targetRot, Time.deltaTime * 5f);
        }

        switch (_state)
        {
            case State.SummonIntro: UpdateSummonIntro(); break;
            case State.Searching: UpdateSearching(); break;
            case State.Focusing: UpdateFocusing(); break;
            case State.Capture: /* 게임오버 연출 중 */ break;
        }
    }

    // [수정] 5초 대기 후 문 열릴 때
    void UpdateSummonIntro()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= introDelay)
        {
            // 문 열기
            if (doorHinge) doorHinge.DOLocalRotate(doorOpenEuler, 0.5f).SetEase(Ease.OutBack);
            if (spotLight) spotLight.enabled = true;
            if (roomMainLight) roomMainLight.enabled = false; // 방 불 끄기

            OnIntroFinished?.Invoke();

            // ▼▼▼ [핵심 수정] 기억해둔 소음 위치가 있는가? ▼▼▼
            if (_pendingFocusPos.HasValue)
            {
                // 있다 -> 바로 '조사(Focusing)' 상태로 진입
                Debug.Log("[Researcher] Door Open -> Immediately Focusing on Target!");
                _state = State.Focusing;
                _stateTimer = 0f;

                // 시선 타겟을 기억해둔 위치로 설정
                _scanTargetPos = _pendingFocusPos.Value;
                _pendingFocusPos = null; // 사용했으니 초기화
            }
            else
            {
                // 없다 -> 그냥 '일반 수색(Searching)' 시작
                Debug.Log("[Researcher] Door Open -> Random Search Start");
                _state = State.Searching;
                _stateTimer = 0f;
                PickNextRandomScanPoint();
            }
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
        }
    }

    void UpdateSearching()
    {
        _stateTimer += Time.deltaTime;

        // 감지 로직
        DetectPlayer();

        // 시간 종료 체크
        if (_stateTimer >= searchDuration)
        {
            EndSearchAndResetNoise();
        }
    }

    void UpdateFocusing()
    {
        _stateTimer += Time.deltaTime;

        // 감지 로직 (조사 중에도 들킬 수 있음)
        DetectPlayer();

        if (_stateTimer >= focusDuration)
        {
            // 조사 끝 -> 다시 랜덤 수색으로 복귀
            _state = State.Searching;
            _stateTimer = 0f; // 수색 타이머 리셋 여부는 기획에 따라 결정 (여기선 전체 시간 공유 안 함)
            PickNextRandomScanPoint();
        }
    }

    // ===== 감지 로직 (Raycast & Spotlight Angle) =====
    void DetectPlayer()
    {
        if (!player || !spotLight) return;

        Vector3 toPlayer = player.position - eyePivot.position;
        float dist = toPlayer.magnitude;

        // 1. 빛의 각도 안에 들어왔는가?
        float angle = Vector3.Angle(eyePivot.forward, toPlayer);
        if (angle > spotLight.spotAngle * 0.5f) return; // 빛 밖임

        // 2. 장애물 검사 (Shadow Casting)
        // 빛의 원점부터 플레이어까지 레이를 쐈을 때 장애물에 막히는지
        if (!Physics.Raycast(eyePivot.position, toPlayer.normalized, dist, obstacleMask))
        {
            // 장애물이 없다면 -> 플레이어 노출됨 -> 검거
            StartCapture();
        }
    }

    void StartCapture()
    {
        if (_state == State.Capture) return;
        _state = State.Capture;

        _scanTween?.Kill();
        // 시선을 플레이어에게 고정
        _scanTargetPos = player.position;

        Debug.Log("[Researcher] CAUGHT YOU!");
        OnPlayerCaught?.Invoke(); // 심장박동 소리 등

        // 손 모델 연출
        if (handModel)
        {
            handModel.gameObject.SetActive(true);
            handModel.DOMove(player.position, 0.4f).SetEase(Ease.InExpo)
                     .OnComplete(() => OnGameOver?.Invoke());
        }
        else
        {
            OnGameOver?.Invoke();
        }
    }

    // ===== Scanning Logic (바닥 타겟 이동) =====
    void PickNextRandomScanPoint()
    {
        if (_state != State.Searching) return;
        if (!roomCenter) return;

        // 랜덤 위치 선정
        float rx = Random.Range(-scanAreaSize.x * 0.5f, scanAreaSize.x * 0.5f);
        float rz = Random.Range(-scanAreaSize.y * 0.5f, scanAreaSize.y * 0.5f);
        Vector3 nextPos = roomCenter.position + new Vector3(rx, 0, rz);

        // 현재 위치에서 다음 위치까지 거리 비례 시간 계산 (일정 속도 유지)
        float dist = Vector3.Distance(_scanTargetPos, nextPos);
        float moveTime = dist / scanMoveSpeed;

        _scanTween = DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, nextPos, moveTime)
                            .SetEase(Ease.InOutSine)
                            .OnComplete(PickNextRandomScanPoint); // 도착하면 다음 점으로
    }

    // ===== 외부 알림용 (NoiseTrapReportToResearcher에서 호출) =====
    // [수정] 외부 알림용
    public void NotifyNoiseEvent(Vector3 worldPos)
    {
        if (_state == State.Capture) return;

        // 경우 1: 이미 나와서 활동 중일 때 -> 즉시 쳐다봄
        if (_state == State.Searching || _state == State.Focusing)
        {
            Debug.Log($"[Researcher] Investigating Noise at {worldPos}");
            _state = State.Focusing;
            _stateTimer = 0f;
            _scanTween?.Kill();

            DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, worldPos, 0.5f)
                   .SetEase(Ease.OutCubic);
        }
        // 경우 2: 대기 중이거나, 등장 연출(5초) 중일 때 -> 위치만 기억해둠
        else if (_state == State.Idle || _state == State.SummonIntro)
        {
            _pendingFocusPos = worldPos;
            Debug.Log($"[Researcher] Noise detected at {worldPos}. Will investigate after door opens.");
        }
    }

    // ===== 수색 종료 및 복귀 =====
    void EndSearchAndResetNoise()
    {
        _state = State.Idle;
        _scanTween?.Kill();

        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);

        OnSearchEnded?.Invoke();

        // 소음 게이지 리셋
        if (NoiseSystem.Instance)
        {
            NoiseSystem.Instance.SetLevel01(noiseResetLevel);
        }

        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());

        Debug.Log("[Researcher] Returned to Idle.");
    }

    System.Collections.IEnumerator Routine_FlickerLightOn()
    {
        // 문 닫히는 시간(0.5초) 정도 기다렸다가 켜기 시작
        yield return new WaitForSeconds(0.5f);

        // 치..직.. (켜졌다 꺼졌다 반복)
        roomMainLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.1f);

        roomMainLight.enabled = true;
        yield return new WaitForSeconds(0.1f);
        roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.2f); // 잠깐 텀

        // 탁! (완전히 켜짐)
        roomMainLight.enabled = true;
    }

    // 에디터 기즈모 (수색 범위 확인용)
    void OnDrawGizmosSelected()
    {
        if (roomCenter)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(roomCenter.position, new Vector3(scanAreaSize.x, 0.1f, scanAreaSize.y));
        }
        if (spotLight)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spotLight.transform.position, catchDistance);
        }
    }

    // ▼▼▼ [새로 추가] 게임 실행 중에 빨간 공(타겟)과 시선(선)을 그림 ▼▼▼
    void OnDrawGizmos()
    {
        // 게임 플레이 중이 아니거나, 수색/조사 상태가 아니면 그리지 않음
        if (!Application.isPlaying) return;
        if (_state != State.Searching && _state != State.Focusing) return;

        Gizmos.color = Color.red;

        // 1. 바닥을 기어다니는 타겟 (빨간 공)
        Gizmos.DrawSphere(_scanTargetPos, 0.3f);

        // 2. 눈에서 타겟으로 쏘는 레이저 (빨간 선)
        if (eyePivot)
        {
            Gizmos.DrawLine(eyePivot.position, _scanTargetPos);
        }
    }
}