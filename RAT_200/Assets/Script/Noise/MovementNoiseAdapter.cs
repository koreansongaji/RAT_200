using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MovementNoiseAdapter : MonoBehaviour
{
    public float walkRatePerSecond = 0.25f;
    public float runRatePerSecond = 0.40f;
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
