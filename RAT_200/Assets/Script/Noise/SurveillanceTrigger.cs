using UnityEngine;
using UnityEngine.Events;

public class SurveillanceTrigger : MonoBehaviour
{
    public UnityEvent onThresholdEntered; // 역치 이상 진입
    public UnityEvent onThresholdExited;  // 히스테리시스 아래로 하강

    void Start()
    {
        var ns = NoiseSystem.Instance; if (!ns) return;
        ns.OnThresholdEntered += () => onThresholdEntered?.Invoke();
        ns.OnThresholdExited += () => onThresholdExited?.Invoke();
    }
}
