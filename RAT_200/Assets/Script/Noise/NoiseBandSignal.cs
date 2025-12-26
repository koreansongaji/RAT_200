using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NoiseSystem.current01 값을 0~100%로 해석해서
/// 0~40 / 41~60 / 61~90 / 91~99 / 100 구간별로 UnityEvent를 쏴주는 허브.
/// 연출(사운드, UI, 조명 등)은 여기 이벤트에만 붙여서 처리하면 됨.
/// </summary>
public class NoiseBandSignal : MonoBehaviour
{
    public enum NoiseBand
    {
        None = -1,
        B0_40 = 0,    // 0~40%
        B41_60 = 1,   // 41~60%
        B61_90 = 2,   // 61~90%
        B91_99 = 3,   // 91~99%
        Full100 = 4   // 100%
    }

    [Header("동작 옵션")]
    [Tooltip("구간이 바뀔 때마다 이벤트를 쏩니다. 꺼두면 수동으로 호출해야 함.")]
    public bool autoSubscribeToNoiseSystem = true;

    [Tooltip("소음 구간이 낮아지는 방향(예: 61~90 → 41~60)으로 변할 때는 이벤트를 무시할지 여부.")]
    public bool onlyWhenIncreasing = false;

    [Tooltip("초기 시작 시 현재 소음 구간에 대한 이벤트를 한 번 쏠지 여부.")]
    public bool fireOnStart = true;

    [Header("이벤트 (연출 훅)")]
    [Tooltip("0% ~ 40% 구간에 진입했을 때 호출되는 이벤트.")]
    public UnityEvent onBand_0_40;

    [Tooltip("41% ~ 60% 구간에 진입했을 때 호출되는 이벤트.")]
    public UnityEvent onBand_41_60;

    [Tooltip("61% ~ 90% 구간에 진입했을 때 호출되는 이벤트.")]
    public UnityEvent onBand_61_90;

    [Tooltip("91% ~ 99% 구간에 진입했을 때 호출되는 이벤트.")]
    public UnityEvent onBand_91_99;

    [Tooltip("100% (가득 참)에 진입했을 때 호출되는 이벤트.")]
    public UnityEvent onBand_100;

    [Header("디버그")]
    [Range(0, 100)] public int currentPercent;    // 에디터에서 보기 편하게
    public NoiseBand currentBand = NoiseBand.None;

    bool _subscribed;

    void Start()
    {
        if (autoSubscribeToNoiseSystem)
            TrySubscribe();

        // 시작 시 현재 값 기준으로 초기 구간 계산 및 옵션에 따라 이벤트 발사
        var ns = NoiseSystem.Instance;
        if (ns != null)
        {
            HandleNoiseChanged(ns.current01, fromStart: true);
        }
    }

    void OnEnable()
    {
        if (autoSubscribeToNoiseSystem)
            TrySubscribe();
    }

    void OnDisable()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && _subscribed)
        {
            ns.OnValueChanged -= OnNoiseValueChanged;
            _subscribed = false;
        }
    }

    void TrySubscribe()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && !_subscribed)
        {
            ns.OnValueChanged += OnNoiseValueChanged;
            _subscribed = true;
        }
    }

    void OnNoiseValueChanged(float value01)
    {
        HandleNoiseChanged(value01, fromStart: false);
    }

    void HandleNoiseChanged(float value01, bool fromStart)
    {
        int percent = Mathf.Clamp(Mathf.RoundToInt(value01 * 100f), 0, 100);
        currentPercent = percent;

        NoiseBand newBand = GetBandFromPercent(percent);

        // 아직 초기 상태(None)였으면 그냥 세팅만
        if (currentBand == NoiseBand.None)
        {
            currentBand = newBand;
            if (fireOnStart)
                InvokeBandEvent(newBand);
            return;
        }

        if (newBand == currentBand)
            return; // 구간이 안 바뀌었으면 아무 것도 안 함

        // onlyWhenIncreasing 옵션: 더 위험한 구간으로 올라갈 때만 이벤트를 보낼지
        if (onlyWhenIncreasing && (int)newBand < (int)currentBand)
        {
            currentBand = newBand; // 내부 상태는 업데이트
            return;
        }

        currentBand = newBand;
        InvokeBandEvent(newBand);
    }

    NoiseBand GetBandFromPercent(int percent)
    {
        if (percent <= 40) return NoiseBand.B0_40;
        if (percent <= 60) return NoiseBand.B41_60;
        if (percent <= 90) return NoiseBand.B61_90;
        if (percent <= 99) return NoiseBand.B91_99;
        return NoiseBand.Full100; // 나머지는 100%
    }

    void InvokeBandEvent(NoiseBand band)
    {
        switch (band)
        {
            case NoiseBand.B0_40:
                onBand_0_40?.Invoke();
                break;
            case NoiseBand.B41_60:
                onBand_41_60?.Invoke();
                break;
            case NoiseBand.B61_90:
                onBand_61_90?.Invoke();
                break;
            case NoiseBand.B91_99:
                onBand_91_99?.Invoke();
                break;
            case NoiseBand.Full100:
                onBand_100?.Invoke();
                break;
        }
    }
}
