using UnityEngine;
using UnityEngine.AI;

public class RatAnimatorDriver : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    [Range(0f, 5f)] public float speedScale = 1f;

    void Update()
    {
        if (!agent || !animator) return;
        float spd = agent.velocity.magnitude * speedScale;
        animator.SetFloat("speed", spd);
    }
}
