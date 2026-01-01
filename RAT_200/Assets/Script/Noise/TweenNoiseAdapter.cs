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

        // Ʈ���� �̹� �÷��� ���̸� ���� ���� ��� 1ȸ ȣ��
        if (t.active && t.IsPlaying())
        {
            adapter.StartTweenNoise();
        }
        else
        {
            // ù ��� ������ Ȯ���� ȣ��
            t.OnStart(adapter.StartTweenNoise);
        }

        // ���� ���� �Ϸ�/ų ��ο��� ����
        t.OnComplete(adapter.StopTweenNoise)
         .OnKill(adapter.StopTweenNoise);

        return t;
    }
}
