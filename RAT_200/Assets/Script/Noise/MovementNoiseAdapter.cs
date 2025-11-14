using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MovementNoiseAdapter : MonoBehaviour
{
    [Header("Rates (기획 반영)")]
    [Tooltip("걷기 중 초당 소음 증가량 (0~1). 기획상 걷기는 0%.")]
    public float walkRatePerSecond = 0f;          // ★ 걷기 = 0%

    [Tooltip("달리기 중 초당 소음 증가량 (0~1). 기획상 달리기 = +1%p/초.")]
    public float runRatePerSecond = 0.01f;        // ★ 1%/sec → 0.01

    [Header("Run 판정")]
    public float velocityEps = 0.05f;
    public bool detectRunBySpeed = true;
    [Min(0f)] public float runSpeedThreshold = 3.5f;

    NavMeshAgent _agent;
    int _handle = 0;

    void Awake() { _agent = GetComponent<NavMeshAgent>(); }
    void OnDisable() { StopNoise(); if (NoiseSystem.Instance) NoiseSystem.Instance.EndAllByOwner(this); }

    void Update()
    {
        var ns = NoiseSystem.Instance; if (!ns || !_agent) return;

        float spd = _agent.velocity.magnitude;
        if (spd <= velocityEps) { StopNoise(); return; }

        float rate = (detectRunBySpeed && spd >= runSpeedThreshold) ? runRatePerSecond : walkRatePerSecond;
        if (_handle == 0) _handle = ns.BeginContinuous(this, "Move", rate);
        else ns.UpdateContinuous(_handle, rate);
    }

    void StopNoise()
    {
        if (_handle != 0 && NoiseSystem.Instance) { NoiseSystem.Instance.EndContinuous(_handle); _handle = 0; }
    }
}
