using UnityEngine;
using DG.Tweening;

public class TweenNoiseAdapter : MonoBehaviour
{
    public float ratePerSecond = 0.30f;
    public float startImpulse = 0f;
    public float endImpulse = 0f;

    int _handle = 0;

    public void StartTweenNoise()
    {
        var ns = NoiseSystem.Instance; if (!ns) return;
        if (startImpulse > 0f) ns.FireImpulse(startImpulse);
        if (_handle == 0) _handle = ns.BeginContinuous(this, "Tween", ratePerSecond);
        else ns.UpdateContinuous(_handle, ratePerSecond);
    }
    public void StopTweenNoise()
    {
        var ns = NoiseSystem.Instance; if (!ns) return;
        if (_handle != 0) { ns.EndContinuous(_handle); _handle = 0; }
        if (endImpulse > 0f) ns.FireImpulse(endImpulse);
    }

    void OnDisable()
    {
        if (_handle != 0 && NoiseSystem.Instance != null) NoiseSystem.Instance.EndContinuous(_handle);
        _handle = 0;
    }

    public static Tween WithNoise(Tween t, TweenNoiseAdapter adapter)
    {
        if (t == null || adapter == null) return t;

        // 트윈이 이미 플레이 중이면 시작 훅을 즉시 1회 호출
        if (t.active && t.IsPlaying())
        {
            adapter.StartTweenNoise();
        }
        else
        {
            // 첫 재생 시점에 확실히 호출
            t.OnStart(adapter.StartTweenNoise);
        }

        // 종료 훅은 완료/킬 모두에서 보장
        t.OnComplete(adapter.StopTweenNoise)
         .OnKill(adapter.StopTweenNoise);

        return t;
    }
}
