using System;
using UnityEngine;
using UnityEngine.Rendering;

public class GlitchHandler : MonoBehaviour
{
    [SerializeField] private Volume _glitchVolume;
    private void Awake()
    {
        if (_glitchVolume == null) _glitchVolume = GetComponent<Volume>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _glitchVolume.weight = 0f;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.G))
        {
            // 최대치 1.0, 나타날 때 속도 5.0, 유지 0.2초, 사라질 때 속도 1.0
            PulseGlitch(1f, 5.0f, 0.2f, 1.0f);
        }
#endif
    }

    public void PulseGlitch(float maxWeight = 1f, float fadeInSpeed = 1f, float holdTime = 0f, float fadeOutSpeed = 1f)
    {
        StartCoroutine(GlitchPulseRoutine(maxWeight, fadeInSpeed, holdTime, fadeOutSpeed));
    }

    private System.Collections.IEnumerator GlitchPulseRoutine(float maxWeight, float fadeInSpeed, float holdTime, float fadeOutSpeed)
    {
        float currentWeight = 0f;

        // 1. Weight 증가 (Fade In)
        while (currentWeight < maxWeight)
        {
            currentWeight += Time.deltaTime * fadeInSpeed;
            _glitchVolume.weight = Mathf.Min(currentWeight, maxWeight);
            yield return null;
        }

        // 2. 최대 상태 유지 (Hold)
        if (holdTime > 0)
        {
            yield return new WaitForSeconds(holdTime);
        }

        // 3. Weight 감소 (Fade Out)
        while (currentWeight > 0f)
        {
            currentWeight -= Time.deltaTime * fadeOutSpeed;
            _glitchVolume.weight = Mathf.Max(currentWeight, 0f);
            yield return null;
        }

        _glitchVolume.weight = 0f;
    }
}
