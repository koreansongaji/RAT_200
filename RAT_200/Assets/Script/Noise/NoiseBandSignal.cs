using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// NoiseSystem.current01 ���� 0~100%�� �ؼ��ؼ�
/// 0~40 / 41~60 / 61~90 / 91~99 / 100 �������� UnityEvent�� ���ִ� ���.
/// ����(����, UI, ���� ��)�� ���� �̺�Ʈ���� �ٿ��� ó���ϸ� ��.
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

    [Header("���� �ɼ�")]
    [Tooltip("������ �ٲ� ������ �̺�Ʈ�� ���ϴ�. ���θ� �������� ȣ���ؾ� ��.")]
    public bool autoSubscribeToNoiseSystem = true;

    [Tooltip("���� ������ �������� ����(��: 61~90 �� 41~60)���� ���� ���� �̺�Ʈ�� �������� ����.")]
    public bool onlyWhenIncreasing = false;

    [Tooltip("�ʱ� ���� �� ���� ���� ������ ���� �̺�Ʈ�� �� �� ���� ����.")]
    public bool fireOnStart = true;

    [Header("�̺�Ʈ (���� ��)")]
    [Tooltip("0% ~ 40% ������ �������� �� ȣ��Ǵ� �̺�Ʈ.")]
    public UnityEvent onBand_0_40;

    [Tooltip("41% ~ 60% ������ �������� �� ȣ��Ǵ� �̺�Ʈ.")]
    public UnityEvent onBand_41_60;

    [Tooltip("61% ~ 90% ������ �������� �� ȣ��Ǵ� �̺�Ʈ.")]
    public UnityEvent onBand_61_90;

    [Tooltip("91% ~ 99% ������ �������� �� ȣ��Ǵ� �̺�Ʈ.")]
    public UnityEvent onBand_91_99;

    [Tooltip("100% (���� ��)�� �������� �� ȣ��Ǵ� �̺�Ʈ.")]
    public UnityEvent onBand_100;

    [Header("�����")]
    [Range(0, 100)] public int currentPercent;    // �����Ϳ��� ���� ���ϰ�
    public NoiseBand currentBand = NoiseBand.None;

    bool _subscribed;

    void Start()
    {
        if (autoSubscribeToNoiseSystem)
            TrySubscribe();

        // ���� �� ���� �� �������� �ʱ� ���� ��� �� �ɼǿ� ���� �̺�Ʈ �߻�
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

        // ���� �ʱ� ����(None)������ �׳� ���ø�
        if (currentBand == NoiseBand.None)
        {
            currentBand = newBand;
            if (fireOnStart)
                InvokeBandEvent(newBand);
            return;
        }

        if (newBand == currentBand)
            return; // ������ �� �ٲ������ �ƹ� �͵� �� ��

        // onlyWhenIncreasing �ɼ�: �� ������ �������� �ö� ���� �̺�Ʈ�� ������
        if (onlyWhenIncreasing && (int)newBand < (int)currentBand)
        {
            currentBand = newBand; // ���� ���´� ������Ʈ
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
        return NoiseBand.Full100; // �������� 100%
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
