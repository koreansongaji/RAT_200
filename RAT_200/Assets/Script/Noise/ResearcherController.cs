using UnityEngine;
using UnityEngine.Events;

public class ResearcherController : MonoBehaviour
{
    public enum State
    {
        Idle,           // 대기 (연구원 없음)
        SummonIntro,    // 100% 도달 후 5초간 접근/발소리
        Searching,      // 30초 일반 수색
        Focusing        // 특정 소음 위치를 5초 동안 응시
    }

    [Header("Refs")]
    [Tooltip("연구원 시선 회전의 기준이 되는 Transform (문 앞에 서 있는 연구원 루트나 머리 회전축 등).")]
    public Transform eyePivot;

    [Header("타이밍")]
    [Tooltip("소음 100% 도달 후, 문 열리고 시선 활성화까지 대기 시간(초). 기획: 5초")]
    public float introDelay = 5f;

    [Tooltip("수색 상태 유지 시간(초). 기획: 30초")]
    public float searchDuration = 30f;

    [Tooltip("조사 모드(소음 위치 응시) 지속 시간(초). 기획: 5초")]
    public float focusDuration = 5f;

    [Tooltip("수색 종료 후 사운드 게이지를 이 값(0~1)으로 리셋. 기획: 20% → 0.2")]
    [Range(0f, 1f)] public float noiseResetLevel = 0.2f;

    [Header("수색(일반) 회전 설정")]
    [Tooltip("방 전체를 둘러볼 때 왼쪽 끝 회전(로컬 기준).")]
    public Vector3 sweepLeftEuler = new Vector3(0, -45, 0);

    [Tooltip("방 전체를 둘러볼 때 오른쪽 끝 회전(로컬 기준).")]
    public Vector3 sweepRightEuler = new Vector3(0, 45, 0);

    [Tooltip("초당 회전 속도(도/초).")]
    public float sweepSpeed = 30f;

    [Header("이벤트 (연출 훅용)")]
    public UnityEvent OnSummonStarted;    // 100% 도달 → 인트로 시작(음악 전환, 발자국 SFX 등)
    public UnityEvent OnIntroFinished;    // 5초 뒤, 문 열리고 시선 활성화
    public UnityEvent OnSearchEnded;      // 30초 수색 종료(문 닫기 등)
    public UnityEvent OnStateChanged;     // 상태가 바뀔 때마다(디버그/연출용)

    State _state = State.Idle;
    float _stateTimer;          // 현재 상태 경과 시간
    bool _sweepToRight = true;  // 왼↔오 번갈아 회전
    Vector3 _focusWorldPos;     // 조사 모드에서 바라볼 위치

    bool _subscribed;

    void Start()
    {
        SubscribeNoiseSystem();
    }

    void OnEnable()
    {
        SubscribeNoiseSystem();
    }

    void OnDisable()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && _subscribed)
        {
            ns.OnValueChanged -= HandleNoiseChanged;
            _subscribed = false;
        }
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
        // 이미 소환/수색 중이면 100%가 또 와도 무시
        if (_state != State.Idle) return;

        // 거의 1.0에 도달했을 때 소환 시작
        if (value01 >= 0.999f)
        {
            StartSummon();
        }
    }

    // 외부에서 수동으로 소환하고 싶을 때도 호출 가능
    public void StartSummon()
    {
        if (_state != State.Idle) return;

        _state = State.SummonIntro;
        _stateTimer = 0f;
        OnSummonStarted?.Invoke();
        OnStateChanged?.Invoke();
        // 이 시점에서 BGM 변경, 발자국 루프 재생 등을 연결하면 됨.
    }

    void Update()
    {
        switch (_state)
        {
            case State.SummonIntro:
                UpdateSummonIntro();
                break;
            case State.Searching:
                UpdateSearching();
                break;
            case State.Focusing:
                UpdateFocusing();
                break;
        }
    }

    // ===== SummonIntro: 5초 대기 후 수색 시작 =====
    void UpdateSummonIntro()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= introDelay)
        {
            // 문이 열리고, 시선 활성화되는 시점
            _state = State.Searching;
            _stateTimer = 0f;
            OnIntroFinished?.Invoke();
            OnStateChanged?.Invoke();
        }
    }

    // ===== Searching: 방 전체 수색 =====
    void UpdateSearching()
    {
        _stateTimer += Time.deltaTime;

        // 단순한 좌↔우 스윕 로직
        if (eyePivot)
        {
            Vector3 targetEuler = _sweepToRight ? sweepRightEuler : sweepLeftEuler;
            Quaternion targetRot = Quaternion.Euler(targetEuler);
            eyePivot.localRotation = Quaternion.RotateTowards(
                eyePivot.localRotation,
                targetRot,
                sweepSpeed * Time.deltaTime
            );

            // 거의 도달하면 방향 반전
            if (Quaternion.Angle(eyePivot.localRotation, targetRot) < 1f)
            {
                _sweepToRight = !_sweepToRight;
            }
        }

        // 30초 수색 종료
        if (_stateTimer >= searchDuration)
        {
            EndSearchAndResetNoise();
        }
    }

    // ===== Focusing: 소음 위치를 5초 동안 응시 =====
    void UpdateFocusing()
    {
        _stateTimer += Time.deltaTime;

        if (eyePivot)
        {
            // 월드 위치 → 방향 → 회전
            Vector3 dir = _focusWorldPos - eyePivot.position;
            dir.y = 0f; // 수평만 고려(필요하면 제거)
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                eyePivot.rotation = Quaternion.RotateTowards(
                    eyePivot.rotation,
                    targetRot,
                    sweepSpeed * Time.deltaTime
                );
            }
        }

        // focusDuration 동안 응시 후 다시 일반 수색으로
        if (_stateTimer >= focusDuration)
        {
            _state = State.Searching;
            _stateTimer = 0f;
            OnStateChanged?.Invoke();
        }
    }

    // ===== 외부에서 "소음이 이 위치에서 났다"라고 알려줄 때 사용 =====
    public void NotifyNoiseEvent(Vector3 worldPos)
    {
        // 수색 중이거나 이미 포커싱 중일 때만 의미 있음
        if (_state != State.Searching && _state != State.Focusing) return;

        _focusWorldPos = worldPos;
        _state = State.Focusing;
        _stateTimer = 0f;
        OnStateChanged?.Invoke();
    }

    // ===== 수색 종료 + 게이지 리셋 =====
    void EndSearchAndResetNoise()
    {
        _state = State.Idle;
        _stateTimer = 0f;
        OnStateChanged?.Invoke();
        OnSearchEnded?.Invoke();

        // 기획: 사운드 게이지 20%로 재설정
        var ns = NoiseSystem.Instance;
        if (ns != null)
        {
            ns.SetLevel01(noiseResetLevel);
        }
    }
}
