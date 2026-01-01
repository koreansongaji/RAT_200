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

    [Header("Impulse Smooth Settings")]
    public float impulseRiseSpeed = 2f;

    // ���� [�߰�] ������ ����� �÷��� ����
    [Header("Debug")]
    [Tooltip("üũ�ϸ� ������ �� �̻� �������� �ʽ��ϴ�.")]
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

    private float _targetNoiseLevel = 0f;

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
        _targetNoiseLevel = 0f;
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
        // ���� [����] �Ͻ����� ���¸� ���� ���� ���� ����
        if (isDebugPaused)
        {
#if UNITY_EDITOR
            Debug.Log($"[NoiseSystem] FireImpulse blocked (Amount: {add01}) because System is Paused.");
#endif
            return;
        }

        if (add01 <= 0f) return;


        //current01 = Mathf.Clamp01(current01 + add01);
        //_lastNoiseTime = Time.time;

        //OnValueChanged?.Invoke(current01);
        //CheckThresholdTransition();

        float baseLevel = Mathf.Max(current01, _targetNoiseLevel);
        _targetNoiseLevel = Mathf.Clamp01(baseLevel + add01);

        _lastNoiseTime = Time.time;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float sumRate = 0f;
        foreach (var s in _sources.Values) if (s.active) sumRate += s.ratePerSecond;
        bool hasContinuousNoise = sumRate > 0f;

        // 1. 지속 소음 처리 (Continuous)
        if (hasContinuousNoise && !isDebugPaused)
        {
            _lastNoiseTime = Time.time;
            float nextVal = current01 + sumRate * dt;

            // 지속 소음은 즉시 반영하되, Impulse 목표치도 같이 밀어올림
            current01 = Mathf.Clamp01(nextVal);
            if (current01 > _targetNoiseLevel) _targetNoiseLevel = current01;

            if (!Mathf.Approximately(current01, nextVal)) // 값이 변했으면 알림
                OnValueChanged?.Invoke(current01);

            CheckThresholdTransition();
            return;
        }

        // 2. Impulse로 인한 부드러운 상승 처리 (Impulse Rise)
        if (current01 < _targetNoiseLevel)
        {
            // 부드럽게 증가 (MoveTowards)
            current01 = Mathf.MoveTowards(current01, _targetNoiseLevel, impulseRiseSpeed * dt);

            // 상승 중에는 감쇄 타이머 리셋 (감소 방지)
            _lastNoiseTime = Time.time;

            OnValueChanged?.Invoke(current01);
            CheckThresholdTransition();
            return; // 상승 중에는 감쇄 로직 실행 X
        }
        else
        {
            // 목표치에 도달했거나 더 높다면, 목표치를 현재값으로 맞춤 (하강 준비)
            _targetNoiseLevel = current01;
        }

        // 3. 자연 감쇄 처리 (Decay)
        if (current01 > 0f && Time.time - _lastNoiseTime >= waitBeforeDecay)
        {
            float afterDecay = Mathf.Max(0f, current01 - decayPerSecond * dt);
            if (!Mathf.Approximately(afterDecay, current01))
            {
                current01 = afterDecay;
                _targetNoiseLevel = current01; // 감쇄 시 목표치도 같이 내림
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
        // ���� ������ ��� ����׿��� ������� �������� ����. �ϴ� �������� ���� (���� ������ �����ϵ���).
        float v = Mathf.Clamp01(value01);
        if (Mathf.Approximately(v, current01)) return;

        current01 = v;
        _targetNoiseLevel = v;


        _lastNoiseTime = Time.time;
        OnValueChanged?.Invoke(current01);
        CheckThresholdTransition();
    }
}