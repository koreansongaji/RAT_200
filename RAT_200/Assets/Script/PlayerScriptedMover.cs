using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using Unity.Cinemachine;

public class PlayerScriptedMover : MonoBehaviour
{
    [Header("Refs")]
    public NavMeshAgent agent;
    public RatInput ratInput;
    public Animator animator;

    [Header("Anim")]
    public float climbAnimMultiplier = 1.2f;

    [Header("Noise (optional)")]
    public TweenNoiseAdapter climbNoise;
    [Min(0f)] public float climbNoiseRatePerSec = 0.30f;
    [Range(0f, 1f)] public float climbStartImpulse = 0f;
    [Range(0f, 1f)] public float climbEndImpulse = 0f;

    Tween _moveTween;
    bool _busy;
    float _baseAnimSpeed = 1f;
    bool _finalizeGuard = false;

    // ★ [수정] 스케일 복구용 변수
    Vector3 _savedScale;
    bool _isScaleOverridden = false;

    private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
    private static readonly int ClimbStateHash = Animator.StringToHash("ClimbState");

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!ratInput) ratInput = GetComponent<RatInput>();

        if (!animator) animator = GetComponent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (animator) _baseAnimSpeed = animator.speed;
    }

    public bool IsBusy() => _busy;

    // [사다리용]
    public void MoveToWorldWithCam(Vector3 worldPos, float duration, Ease ease,
                                   CinemachineCamera vcam, int onPriority = 10,
                                   bool keepCamOnAfterArrive = true)
    {
        if (_busy) return;
        _busy = true;
        _finalizeGuard = false;

        PrepareMovement(vcam, onPriority);

        _moveTween?.Kill();
        _moveTween = transform.DOMove(worldPos, duration)
                              .SetEase(ease)
                              .SetUpdate(UpdateType.Normal)
                              .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                              .OnUpdate(SyncNavMesh)
                              .OnComplete(() => FinalizeTween(true))
                              .OnKill(() => FinalizeTween(false));
    }

    // [밧줄용] overrideScaleX (float?) 파라미터로 변경
    public void MovePathWithCam(Vector3[] path, float duration, Ease ease,
                                CinemachineCamera vcam, int onPriority = 10,
                                bool useRopeAnim = false,
                                float? overrideScaleX = null) // ★ [수정] 특정 값으로 강제 설정 (null이면 안 함)
    {
        if (_busy) return;
        _busy = true;
        _finalizeGuard = false;

        PrepareMovement(vcam, onPriority);

        // ★ [수정] 스케일 강제 설정 로직
        if (overrideScaleX.HasValue)
        {
            _savedScale = transform.localScale;
            // X값만 입력받은 값으로 교체 (Y, Z는 유지)
            transform.localScale = new Vector3(overrideScaleX.Value, _savedScale.y, _savedScale.z);
            _isScaleOverridden = true;
        }

        // 밧줄 애니메이션
        if (useRopeAnim && animator)
        {
            animator.SetBool(IsClimbingHash, true);
            animator.SetInteger(ClimbStateHash, 0); // Up

            DOVirtual.DelayedCall(0.2f, () =>
            {
                if (_busy) animator.SetInteger(ClimbStateHash, 1); // Keep
            });
        }

        _moveTween?.Kill();
        _moveTween = transform.DOPath(path, duration, PathType.Linear, PathMode.Full3D)
                              .SetEase(ease)
                              .SetUpdate(UpdateType.Normal)
                              .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                              .OnUpdate(SyncNavMesh)
                              .OnComplete(() =>
                              {
                                  if (useRopeAnim && animator)
                                      animator.SetInteger(ClimbStateHash, 2); // Down
                                  FinalizeTween(true);
                              })
                              .OnKill(() => FinalizeTween(false));
    }

    void PrepareMovement(CinemachineCamera vcam, int onPriority)
    {
        if (agent.hasPath) agent.ResetPath();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        if (ratInput != null) ratInput.Click.Disable();
        agent.updatePosition = false;
        if (animator) animator.speed = _baseAnimSpeed * climbAnimMultiplier;
        if (vcam) vcam.Priority = onPriority;
        StartClimbNoise();
    }

    void SyncNavMesh()
    {
        if (agent) agent.nextPosition = transform.position;
    }

    void FinalizeTween(bool normalComplete)
    {
        if (_finalizeGuard) return;
        _finalizeGuard = true;

        StopClimbNoise(normalComplete);

        // ★ [수정] 스케일 원상 복구
        if (_isScaleOverridden)
        {
            transform.localScale = _savedScale;
            _isScaleOverridden = false;
        }

        if (agent && agent.enabled)
        {
            if (agent.isOnNavMesh) agent.Warp(transform.position);
            agent.updatePosition = true;
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (ratInput && ratInput.isActiveAndEnabled) ratInput.Click.Enable();

        if (animator)
        {
            animator.speed = _baseAnimSpeed;
            animator.SetBool(IsClimbingHash, false);
        }

        _busy = false;
    }

    void StartClimbNoise()
    {
        if (!climbNoise) return;
        climbNoise.ratePerSecond = climbNoiseRatePerSec;
        climbNoise.startImpulse = climbStartImpulse;
        climbNoise.endImpulse = 0f;
        climbNoise.StartTweenNoise();
    }

    void StopClimbNoise(bool normalComplete)
    {
        if (!climbNoise) return;
        climbNoise.StopTweenNoise();
        if (normalComplete && climbEndImpulse > 0f && NoiseSystem.Instance)
        {
            NoiseSystem.Instance.FireImpulse(climbEndImpulse);
        }
    }

    void OnDisable()
    {
        if (_moveTween != null && _moveTween.IsActive()) _moveTween.Kill();
        else FinalizeTween(false);
    }
}