using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using VolFx;

public class GlitchController : MonoBehaviour
{
    [Header("소음 수치")]
    [SerializeField] private NoiseBandSignal _noiseBandSignal;
    
    [SerializeField] private Volume _glitchVolume;
    VolumeProfile _glitchProfile;
    private GlitchVol _glitchComponent;
    private Tween _glitchTween;
    [SerializeField] private float _tweenDuration = 0.5f;
    
    private void Awake()
    {
        if (_glitchVolume == null)
        {
            _glitchVolume = GetComponent<Volume>();
        }

        if (_glitchVolume != null)
        {
            _glitchProfile = _glitchVolume.profile;
            if (_glitchProfile != null)
            {
                // 여러 볼륨 컴포넌트 중 GlitchVol만 가져옵니다.
                if (!_glitchProfile.TryGet<GlitchVol>(out _glitchComponent))
                {
                    Debug.LogWarning("GlitchController: GlitchVol component not found in the VolumeProfile.");
                }
            }
            else
            {
                Debug.LogWarning("GlitchController: VolumeProfile is null on the assigned Volume.");
            }
        }
    }

    private void FixedUpdate()
    {
        if (_noiseBandSignal != null)
        {
            SetGlitchValue((_noiseBandSignal.currentPercent - 50)/100f);
        }
    }

    public void SetGlitchValue(float weight)
    {

        if (_glitchComponent == null)
        {
            // Try to recover if profile changed after Awake
            if (_glitchProfile != null && _glitchProfile.TryGet<GlitchVol>(out _glitchComponent) == false)
            {
                Debug.LogWarning("GlitchController: GlitchVol component not available to tween.");
                return;
            }
        }

        _glitchTween?.Kill();
        _glitchTween = DOTween.To(() => _glitchComponent._weight.value, x => _glitchComponent._weight.value = x, weight, _tweenDuration);
    }
}
