using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FellowRatController : MonoBehaviour
{
    private NavMeshAgent _agent;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        // NPC끼리 밀리지 않게 회피 순위 조정 (선택 사항)
        _agent.avoidancePriority = 50;
    }

    // 외부(트리거)에서 호출할 이동 함수
    public void MoveTo(Transform target)
    {
        if (target != null)
        {
            _agent.SetDestination(target.position);
            _agent.isStopped = false;
        }
    }
}