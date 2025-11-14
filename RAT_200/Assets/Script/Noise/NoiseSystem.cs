using UnityEngine;
using System;
using System.Collections.Generic;

public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance { get; private set; }

    [Header("Settings")]
    [Min(0f)] public float decayPerSecond = 0.02f;        // ★ 기획: 초당 2% 감소 → 0.02
    [Min(0f)] public float waitBeforeDecay = 2f;          // ★ 마지막 소음 후 5초 대기
    [Range(0f, 1f)] public float surveillanceThreshold = 0.8f;
    [Range(0f, 0.2f)] public float thresholdHysteresis = 0.05f;

    [Header("Runtime (read-only)")]
    [Range(0f, 1f)] public float current01 = 0f;

    public event Action<float> OnValueChanged;
    public event Action OnThresholdEntered;
    public event Action OnThresholdExited;

    struct Source
    {
        public int id;
        public UnityEngine.Object owner;
        public float ratePerSecond;
        public string tag;
        public bool active;
    }

    int _nextId = 1;
    readonly Dictionary<int, Source> _sources = new();
    readonly Dictionary<UnityEngine.Object, List<int>> _ownerIndex = new();
    bool _aboveThreshold;

    // ★ 마지막으로 "소음이 추가된" 시간 (Impulse or Continuous)
    float _lastNoiseTime = -999f;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDisable()
    {
        _sources.Clear();
        _ownerIndex.Clear();
        _nextId = 1;
        _aboveThreshold = false;
    }

    // ===== Continuous 소음 소스 관리 =====
    public int BeginContinuous(UnityEngine.Object owner, string tag, float ratePerSecond)
    {
        if (owner == null) owner = this;
        if (!_ownerIndex.TryGetValue(owner, out var list))
        {
            list = new List<int>(4);
            _ownerIndex.Add(owner, list);
        }

        var src = new Source
        {
            id = _nextId++,
            owner = owner,
            ratePerSecond = Mathf.Max(0f, ratePerSecond),
            tag = tag,
            active = true
        };
        _sources[src.id] = src;
        list.Add(src.id);
        return src.id;
    }

    public void UpdateContinuous(int handle, float newRatePerSecond)
    {
        if (_sources.TryGetValue(handle, out var s))
        {
            s.ratePerSecond = Mathf.Max(0f, newRatePerSecond);
            _sources[handle] = s;
        }
    }

    public void EndContinuous(int handle)
    {
        if (_sources.TryGetValue(handle, out var s))
        {
            _sources.Remove(handle);
            if (s.owner != null && _ownerIndex.TryGetValue(s.owner, out var list))
            {
                list.Remove(handle);
                if (list.Count == 0) _ownerIndex.Remove(s.owner);
            }
        }
    }

    public void EndAllByOwner(UnityEngine.Object owner)
    {
        if (owner == null) return;
        if (_ownerIndex.TryGetValue(owner, out var list))
        {
            foreach (var h in list)
                _sources.Remove(h);
            _ownerIndex.Remove(owner);
        }
    }

    // ===== Impulse 소음 (즉시 한 번에 튀어오르는 소리) =====
    public void FireImpulse(float add01)
    {
        if (add01 <= 0f) return;

        current01 = Mathf.Clamp01(current01 + add01);
        _lastNoiseTime = Time.time;                 // ★ 마지막 소음 시각 갱신

        OnValueChanged?.Invoke(current01);
        CheckThresholdTransition();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1) 현재 활성화된 Continuous 소음 소스들의 rate 합
        float sumRate = 0f;
        foreach (var s in _sources.Values)
            if (s.active) sumRate += s.ratePerSecond;

        bool hasContinuousNoise = sumRate > 0f;

        if (hasContinuousNoise)
        {
            // 2) Continuous 소음이 있는 동안은 계속 게이지를 올리고,
            //    "마지막 소음 발생 시각"을 매 프레임 갱신한다.
            _lastNoiseTime = Time.time;

            float afterAcc = Mathf.Clamp01(current01 + sumRate * dt);
            if (!Mathf.Approximately(afterAcc, current01))
            {
                current01 = afterAcc;
                OnValueChanged?.Invoke(current01);
            }

            CheckThresholdTransition();
            return; // ★ 소리가 나는 동안에는 절대 감소 X
        }

        // 3) Continuous 소음이 없을 때만,
        //    "마지막 소음 발생 후 waitBeforeDecay 초가 지났다면" 서서히 감소
        if (current01 > 0f && Time.time - _lastNoiseTime >= waitBeforeDecay)
        {
            float afterDecay = Mathf.Max(0f, current01 - decayPerSecond * dt);
            if (!Mathf.Approximately(afterDecay, current01))
            {
                current01 = afterDecay;
                OnValueChanged?.Invoke(current01);
            }
        }

        CheckThresholdTransition();
    }

    void CheckThresholdTransition()
    {
        float enter = surveillanceThreshold;
        float exit = Mathf.Max(0f, surveillanceThreshold - thresholdHysteresis);

        if (!_aboveThreshold && current01 >= enter)
        {
            _aboveThreshold = true;
            OnThresholdEntered?.Invoke();
        }
        else if (_aboveThreshold && current01 < exit)
        {
            _aboveThreshold = false;
            OnThresholdExited?.Invoke();
        }
    }

    public void SetLevel01(float value01)
    {
        float v = Mathf.Clamp01(value01);
        if (Mathf.Approximately(v, current01)) return;

        current01 = v;
        _lastNoiseTime = Time.time;      // 이 시점을 '마지막 소음 시각'으로 간주
        OnValueChanged?.Invoke(current01);
        CheckThresholdTransition();
    }

}
