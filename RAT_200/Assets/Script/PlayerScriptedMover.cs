using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using Unity.Cinemachine;

public class PlayerScriptedMover : MonoBehaviour
{
    [Header("Refs")]
    public NavMeshAgent agent;
    public RatInput ratInput;      // 선택
    public Animator animator;      // 선택

    [Header("Anim")]
    public float climbAnimMultiplier = 1.2f;

    Tween _moveTween;
    bool _busy;
    float _baseAnimSpeed = 1f;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!ratInput) ratInput = GetComponent<RatInput>();
        if (animator) _baseAnimSpeed = animator.speed;
    }

    public bool IsBusy() => _busy;
    float _lastY;
    
    // 절대 월드 좌표로 이동 + VCam 전환(되돌림은 호출자에게 맡김)
    public void MoveToWorldWithCam(Vector3 worldPos, float duration, Ease ease,
                                   CinemachineCamera vcam, int onPriority = 10,
                                   bool keepCamOnAfterArrive = true)
    {
        if (_busy) return;
        _busy = true;

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

        //ToggleAgent(agent, false);

        // 6) 스크립트 이동
        _moveTween?.Kill();
        _moveTween = transform.DOMove(worldPos, duration)
                              .SetEase(ease)
                              .SetUpdate(UpdateType.Normal)
                              .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                              .OnUpdate(() => { agent.nextPosition = transform.position; }) // ★ 추가
                              .OnComplete(() =>
                              {
                                  //ToggleAgent(agent, true);
                                  // 7) 위치 동기화 + 기본 상태 복구(카메라는 유지할 수도 있으니 건드리지 않음)
                                  agent.Warp(transform.position);
                                  agent.updatePosition = true;
                                  agent.isStopped = true;
                                  agent.velocity = Vector3.zero;

                                  if (ratInput != null) ratInput.Click.Enable();
                                  if (animator) animator.speed = _baseAnimSpeed;

                                  _busy = false;
                              });
    }

    void OnDisable()
    {
        _moveTween?.Kill();

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
    void ToggleAgent(NavMeshAgent agent, bool on)
    {
        if (agent) agent.enabled = on;
    }
}
