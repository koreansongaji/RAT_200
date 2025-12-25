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

    private void Start()
    {
        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    private void Update()
    {
        UpdateAnimation();

        if (isDeadPositionMode) return;

        PatrolLogic();
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;
        
        // 실제 속도를 애니메이터에 전달
        anim.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

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

    private void MoveToNextWaypoint()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    public void MoveToDeadPosition()
    {
        if (deadPosition == null) return;

        isDeadPositionMode = true;
        isWaiting = false;
        
        agent.SetDestination(deadPosition.position);
        agent.stoppingDistance = 0.1f;
    }

    // 필요 시 런타임에 속도를 변경할 수 있는 메서드
    public void SetSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
        agent.speed = movementSpeed;
    }
}
