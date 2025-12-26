using UnityEngine;
using UnityEngine.AI;

public class FriendRatMovement : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float movementSpeed = 3.5f; // 이동 속도 설정 변수 추가
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTime = 1f;

    [Header("특정 위치 설정")]
    public Transform deadPosition;

    private NavMeshAgent agent;
    private Animator anim;
    
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isDeadPositionMode = false;

    private static readonly int SpeedHash = Animator.StringToHash("speed");

    /// <summary>
    /// 컴포넌트 초기화 및 NavMeshAgent의 기본 속도를 설정합니다.
    /// </summary>
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // 에디터에서 설정한 이동 속도를 에이전트에게 적용
        if (agent != null)
        {
            agent.speed = movementSpeed;
        }
    }

    /// <summary>
    /// 웨이포인트가 존재할 경우 첫 번째 목적지로 이동을 시작합니다.
    /// </summary>
    private void Start()
    {
        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    /// <summary>
    /// 매 프레임 애니메이션 상태를 업데이트하고 정해진 순찰 로직을 수행합니다.
    /// </summary>
    private void Update()
    {
        UpdateAnimation();

        if (isDeadPositionMode) return;

        PatrolLogic();
    }

    /// <summary>
    /// NavMeshAgent의 현재 속도를 기반으로 애니메이터의 Speed 파라미터를 업데이트합니다.
    /// </summary>
    private void UpdateAnimation()
    {
        if (anim == null) return;
        
        // 실제 속도를 애니메이터에 전달
        anim.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    /// <summary>
    /// 웨이포인트를 순회하는 순찰 로직입니다. 목적지 도착 시 대기 시간을 가진 후 다음 지점으로 이동합니다.
    /// </summary>
    private void PatrolLogic()
    {
        if (waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = waitTime;
            }

            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    isWaiting = false;
                    MoveToNextWaypoint();
                }
            }
        }
    }

    /// <summary>
    /// 다음 순찰 지점 인덱스를 계산하고 NavMeshAgent의 목적지를 갱신합니다.
    /// </summary>
    private void MoveToNextWaypoint()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    /// <summary>
    /// 일반 순찰을 중단하고 지정된 'DeadPosition'으로 즉시 이동하게 합니다.
    /// </summary>
    public void MoveToDeadPosition()
    {
        if (deadPosition == null) return;

        isDeadPositionMode = true;
        isWaiting = false;
        
        agent.SetDestination(deadPosition.position);
        agent.stoppingDistance = 0.1f;
    }

    /// <summary>
    /// 런타임 중에 쥐의 이동 속도를 동적으로 변경합니다.
    /// </summary>
    /// <param name="newSpeed">적용할 새로운 이동 속도</param>
    public void SetSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
        agent.speed = movementSpeed;
    }
}
