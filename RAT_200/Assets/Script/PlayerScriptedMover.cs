using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using Unity.Cinemachine;

/// <summary>
/// 절대 월드 좌표로의 스크립트 이동(DOTween) + NavMeshAgent 상태 동기화 + (선택) 소음 훅.
/// - 트윈 중에는 agent.updatePosition=false 로 두고, 매 프레임 agent.nextPosition 동기화
/// - 완료/중단 시에는 반드시 agent.Warp(transform.position)로 최종 동기화
/// - 소음은 TweenNoiseAdapter를 직접 Start/Stop 호출(WithNoise 사용 X)하여 콜백 순서 안정화
/// </summary>
public class PlayerScriptedMover : MonoBehaviour
{
    [Header("Refs")]
    public NavMeshAgent agent;
    public RatInput ratInput;      // 선택
    public Animator animator;      // 선택

    [Header("Anim")]
    public float climbAnimMultiplier = 1.2f;

    [Header("Noise (optional)")]
    public TweenNoiseAdapter climbNoise;               // Player에 붙인 TweenNoiseAdapter 참조
    [Min(0f)] public float climbNoiseRatePerSec = 0.30f;
    [Range(0f, 1f)] public float climbStartImpulse = 0f;
    [Range(0f, 1f)] public float climbEndImpulse = 0f;

    Tween _moveTween;
    bool _busy;
    float _baseAnimSpeed = 1f;
    bool _finalizeGuard = false;   // 완료/중단 정리 루틴 1회 보장
    // 필요시 사용할 수 있도록 남겨둠
    float _lastY;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!ratInput) ratInput = GetComponent<RatInput>();
        if (animator) _baseAnimSpeed = animator.speed;
    }

    public bool IsBusy() => _busy;

    // 절대 월드 좌표로 이동 + VCam 전환(되돌림은 호출자에게 맡김)
    public void MoveToWorldWithCam(Vector3 worldPos, float duration, Ease ease,
                                   CinemachineCamera vcam, int onPriority = 10,
                                   bool keepCamOnAfterArrive = true)
    {
        if (_busy) return;
        _busy = true;
        _finalizeGuard = false;

        // 1) NavMesh 이동 정지
        if (agent.hasPath) agent.ResetPath();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 2) 입력 잠금
        if (ratInput != null) ratInput.Click.Disable();

        // 3) NavMeshAgent가 transform을 움직이지 않도록
        agent.updatePosition = false;

        // 4) 애니(선택)
        if (animator) animator.speed = _baseAnimSpeed * climbAnimMultiplier;

        // 5) 카메라 전환: 우선순위만 올림 (되돌리기는 호출자가 제어)
        if (vcam) vcam.Priority = onPriority;

        // 6) 소음 시작(명시적 호출: 콜백 순서 보장 목적)
        StartClimbNoise();

        // 7) 스크립트 이동 트윈
        _moveTween?.Kill();
        _moveTween = transform.DOMove(worldPos, duration)
                              .SetEase(ease)
                              .SetUpdate(UpdateType.Normal)
                              .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                              .OnUpdate(() =>
                              {
                                  // 트윈 중 위치 동기화
                                  if (agent) agent.nextPosition = transform.position;
                              })
                              .OnComplete(() =>
                              {
                                  FinalizeTween(normalComplete: true);
                              })
                              .OnKill(() =>
                              {
                                  // 씬 파괴/강제 취소 등 비정상 종료 시에도 동일 정리
                                  FinalizeTween(normalComplete: false);
                              });
    }

    /// <summary>
    /// 트윈 완료/중단 정리: 소음 종료 → NavMesh 최종 동기화 → 입력/애니 복원
    /// (중복 실행 방지를 위해 가드)
    /// </summary>
    void FinalizeTween(bool normalComplete)
    {
        if (_finalizeGuard) return;
        _finalizeGuard = true;

        // 소음 종료 (완료/중단 모두 동일하게)
        StopClimbNoise(normalComplete);

        // NavMeshAgent 최종 동기화
        if (agent && agent.enabled)
        {
            // 가능한 경우에만 Warp (NavMesh 위가 아닐 때 Warp 호출하면 예외)
            if (agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
            }
            // Warp 여부와 무관하게 기본 상태 복구
            agent.updatePosition = true;
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 입력/애니 복구
        if (ratInput && ratInput.isActiveAndEnabled)
            ratInput.Click.Enable();

        if (animator)
            animator.speed = _baseAnimSpeed;

        _busy = false;
    }

    // ▼▼▼ [추가] 경로(Path)를 따라 이동하는 함수 ▼▼▼
    public void MovePathWithCam(Vector3[] path, float duration, Ease ease,
                                CinemachineCamera vcam, int onPriority = 10)
    {
        if (_busy) return;
        _busy = true;
        _finalizeGuard = false;

        // 1) NavMesh 정지 & 입력 잠금 (기존과 동일)
        if (agent.hasPath) agent.ResetPath();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        if (ratInput != null) ratInput.Click.Disable();
        agent.updatePosition = false;

        // 애니메이션 가속
        if (animator) animator.speed = _baseAnimSpeed * climbAnimMultiplier;

        // 카메라 전환
        if (vcam) vcam.Priority = onPriority;

        // 소음 시작
        StartClimbNoise();

        // 2) ★ DOPath로 경로 이동 실행
        _moveTween?.Kill();
        _moveTween = transform.DOPath(path, duration, PathType.Linear, PathMode.Full3D)
                              .SetEase(ease)
                              .SetUpdate(UpdateType.Normal)
                              .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                              .OnUpdate(() =>
                              {
                                  if (agent) agent.nextPosition = transform.position;
                              })
                              .OnComplete(() => FinalizeTween(true))
                              .OnKill(() => FinalizeTween(false));
    }

    void StartClimbNoise()
    {
        if (!climbNoise) return;

        // endImpulse는 완료 시점에서 직접 쏴서 순서를 보장
        climbNoise.ratePerSecond = climbNoiseRatePerSec;
        climbNoise.startImpulse = climbStartImpulse;
        climbNoise.endImpulse = 0f;

        climbNoise.StartTweenNoise();
    }

    void StopClimbNoise(bool normalComplete)
    {
        if (!climbNoise) return;

        // 지속 소음 종료
        climbNoise.StopTweenNoise();

        // 정상 완료시에만 종료 임펄스 선택적으로 발사
        if (normalComplete && climbEndImpulse > 0f)
        {
            // NoiseSystem이 씬에 없다면 조용히 무시됨
            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(climbEndImpulse);
        }
    }

    void OnDisable()
    {
        // 트윈이 진행 중이면 Kill → OnKill에서 FinalizeTween 호출됨
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Kill();
        }
        else
        {
            // 트윈이 없거나 이미 정리된 경우에도 안전하게 한 번 더 마무리 시도
            FinalizeTween(normalComplete: false);
        }

        // (아래는 기존 방어 로직 유지)
        // 입력/애니 복원은 가능한 경우에만
        if (ratInput && ratInput.isActiveAndEnabled)
            ratInput.Click.Enable();

        if (animator)
            animator.speed = _baseAnimSpeed;

        // ★ NavMeshAgent가 '활성 + NavMesh 위'일 때만 정지 루틴 실행
        if (agent && agent.enabled && gameObject.activeInHierarchy && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        else if (agent) // 최소한의 상태 복구만
        {
            agent.updatePosition = true;
            // isStopped/ResetPath는 건드리지 않음
        }

        _busy = false;
    }

    // 필요 시 사용할 수 있도록 남겨둔 헬퍼
    void ToggleAgent(NavMeshAgent agent, bool on)
    {
        if (agent) agent.enabled = on;
    }
}
