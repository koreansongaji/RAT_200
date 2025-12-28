using UnityEngine;
using System;
using System.Collections.Generic;

public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance { get; private set; }

    [Header("Settings")]
    [Min(0f)] public float decayPerSecond = 0.02f;
    [Min(0f)] public float waitBeforeDecay = 2f;
    [Range(0f, 1f)] public float surveillanceThreshold = 0.8f;
    [Range(0f, 0.2f)] public float thresholdHysteresis = 0.05f;

    [Header("Runtime (read-only)")]
    [Range(0f, 1f)] public float current01 = 0f;

    // ▼▼▼ [추가] 에디터 제어용 플래그 ▼▼▼
    [Header("Debug")]
    [Tooltip("체크하면 소음이 더 이상 증가하지 않습니다.")]
    public bool isDebugPaused = false;

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

    public void FireImpulse(float add01)
    {
        // ▼▼▼ [수정] 일시정지 상태면 소음 증가 차단 ▼▼▼
        if (isDebugPaused)
        {
#if UNITY_EDITOR
            Debug.Log($"[NoiseSystem] FireImpulse blocked (Amount: {add01}) because System is Paused.");
#endif
            return;
        }

        if (add01 <= 0f) return;

        current01 = Mathf.Clamp01(current01 + add01);
        _lastNoiseTime = Time.time;

        OnValueChanged?.Invoke(current01);
        CheckThresholdTransition();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        float sumRate = 0f;
        foreach (var s in _sources.Values)
            if (s.active) sumRate += s.ratePerSecond;

        bool hasContinuousNoise = sumRate > 0f;

        // ▼▼▼ [수정] 일시정지 상태가 아닐 때만 소음 증가 처리 ▼▼▼
        if (hasContinuousNoise && !isDebugPaused)
        {
            _lastNoiseTime = Time.time;

            float afterAcc = Mathf.Clamp01(current01 + sumRate * dt);
            if (!Mathf.Approximately(afterAcc, current01))
            {
                current01 = afterAcc;
                OnValueChanged?.Invoke(current01);
            }

            CheckThresholdTransition();
            return;
        }

        // 감소 로직 (일시정지여도 소음이 줄어드는 건 허용할지, 아예 멈출지 결정. 여기서는 줄어들게 둠)
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
        // 강제 설정의 경우 디버그여도 허용할지 차단할지 선택. 일단 차단하지 않음 (수동 조작은 가능하도록).
        float v = Mathf.Clamp01(value01);
        if (Mathf.Approximately(v, current01)) return;

        current01 = v;
        _lastNoiseTime = Time.time;
        OnValueChanged?.Invoke(current01);
        CheckThresholdTransition();
    }
}