using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FellowRatController : MonoBehaviour
{
    private NavMeshAgent _agent;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent)
        {
            _agent.avoidancePriority = 50;
        }
    }

    // 외부(트리거)에서 호출할 이동 함수
    public void MoveTo(Transform target)
    {
        // ★ [핵심 수정] 에이전트가 NavMesh 위에 있고 켜져 있을 때만 명령 수행
        if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
        {
            // 이미 사라진 쥐라면 조용히 리턴 (경고 방지)
            return;
        }

        if (target != null)
        {
            _agent.SetDestination(target.position);
            _agent.isStopped = false; // 이게 "Resume" 경고의 주범인데, 위 조건문으로 해결됨
        }
    }
}