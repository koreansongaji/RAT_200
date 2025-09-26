using UnityEngine;
using System;
using System.Collections.Generic;

public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance { get; private set; }

    [Header("Settings")]
    [Min(0f)] public float decayPerSecond = 0.15f;         // 항상 줄어드는 속도(초당)
    [Range(0f, 1f)] public float surveillanceThreshold = 0.8f;
    [Range(0f, 0.2f)] public float thresholdHysteresis = 0.05f;

    [Header("Runtime (read-only)")]
    [Range(0f, 1f)] public float current01 = 0f;

    public event Action<float> OnValueChanged;
    public event Action OnThresholdEntered;
    public event Action OnThresholdExited;

    struct Source { public int id; public UnityEngine.Object owner; public float ratePerSecond; public string tag; public bool active; }
    int _nextId = 1;
    readonly Dictionary<int, Source> _sources = new();
    readonly Dictionary<UnityEngine.Object, List<int>> _ownerIndex = new();
    bool _aboveThreshold;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDisable()
    {
        _sources.Clear(); _ownerIndex.Clear(); _nextId = 1; _aboveThreshold = false;
    }

    public int BeginContinuous(UnityEngine.Object owner, string tag, float ratePerSecond)
    {
        if (owner == null) owner = this;
        if (!_ownerIndex.TryGetValue(owner, out var list)) { list = new List<int>(4); _ownerIndex.Add(owner, list); }
        var src = new Source { id = _nextId++, owner = owner, ratePerSecond = Mathf.Max(0f, ratePerSecond), tag = tag, active = true };
        _sources[src.id] = src; list.Add(src.id);
        return src.id;
    }

    public void UpdateContinuous(int handle, float newRatePerSecond)
    {
        if (_sources.TryGetValue(handle, out var s)) { s.ratePerSecond = Mathf.Max(0f, newRatePerSecond); _sources[handle] = s; }
    }

    public void EndContinuous(int handle)
    {
        if (_sources.TryGetValue(handle, out var s))
        {
            _sources.Remove(handle);
            if (s.owner != null && _ownerIndex.TryGetValue(s.owner, out var list))
            { list.Remove(handle); if (list.Count == 0) _ownerIndex.Remove(s.owner); }
        }
    }

    public void EndAllByOwner(UnityEngine.Object owner)
    {
        if (owner == null) return;
        if (_ownerIndex.TryGetValue(owner, out var list))
        {
            foreach (var h in list) _sources.Remove(h);
            _ownerIndex.Remove(owner);
        }
    }

    public void FireImpulse(float add01)
    {
        if (add01 <= 0f) return;
        current01 = Mathf.Clamp01(current01 + add01);
        OnValueChanged?.Invoke(current01);
        CheckThresholdTransition();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float sum = 0f;
        foreach (var s in _sources.Values) if (s.active) sum += s.ratePerSecond;

        float afterDecay = Mathf.Max(0f, current01 - decayPerSecond * dt);
        float afterAcc = afterDecay + sum * dt;
        float clamped = Mathf.Clamp01(afterAcc);

        if (!Mathf.Approximately(clamped, current01))
        {
            current01 = clamped;
            OnValueChanged?.Invoke(current01);
        }
        CheckThresholdTransition();
    }

    void CheckThresholdTransition()
    {
        float enter = surveillanceThreshold;
        float exit = Mathf.Max(0f, surveillanceThreshold - thresholdHysteresis);

        if (!_aboveThreshold && current01 >= enter) { _aboveThreshold = true; OnThresholdEntered?.Invoke(); }
        else if (_aboveThreshold && current01 < exit) { _aboveThreshold = false; OnThresholdExited?.Invoke(); }
    }
}
