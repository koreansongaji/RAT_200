using System;
using UnityEngine;
using DG.Tweening;

public class OptionPage : MonoBehaviour
{
    [SerializeField] private Transform _slideOutPosition;
    [SerializeField] private Transform _slideInPosition;
    [SerializeField] private TitlePage _titlePage;

    private Tween _currentTween;
    private bool _isTransitioning;

    private void Awake()
    {
        if (_slideOutPosition == null || _slideInPosition == null)
        {
            Debug.LogError("[OptionPage] Positions are not assigned.");
            enabled = false;
            return;
        }
        transform.position = _slideOutPosition.position;
    }
    private void Start()
    {
        OnBgmVolumeChange(OptionManager.Instance.OptionData.BGMsoundVolume);
        OnEffectVolumeChange(OptionManager.Instance.OptionData.EffectsoundVolume);
        OnMasterVolumeChange(OptionManager.Instance.OptionData.MastersoundVolume);
    }
    public void OnMasterVolumeChange(float volume)
    {
        OptionManager.Instance.OptionData.MastersoundVolume = volume;
        AudioManager.Instance.SetMasterVolume(OptionManager.Instance.OptionData.MastersoundVolume);
    }
    public void OnEffectVolumeChange(float volume)
    {
        OptionManager.Instance.OptionData.EffectsoundVolume = volume;
        AudioManager.Instance.SetEffectVolume(OptionManager.Instance.OptionData.EffectsoundVolume);
    }
    public void OnBgmVolumeChange(float volume)
    {
        OptionManager.Instance.OptionData.BGMsoundVolume = volume;
        AudioManager.Instance.SetBGMVolume(OptionManager.Instance.OptionData.BGMsoundVolume);
    }
    //------------- 페이지 이동 함수 (DOTween) -------------------
    public void BackToTitle()
    {
        if (_isTransitioning || _titlePage == null) return;

        _isTransitioning = true;
        KillTween();

        var timeout = DOVirtual.DelayedCall(1.2f, () => { _isTransitioning = false; }, ignoreTimeScale: true).SetUpdate(true);

        if ((transform.position - _slideOutPosition.position).sqrMagnitude < 0.0001f)
        {
            _titlePage.SlideInTitlePage(0.5f, Ease.OutCubic, () =>
            {
                if (timeout.IsActive()) timeout.Kill();
                _isTransitioning = false;
            });
            return;
        }

        var seq = DOTween.Sequence();
        seq.Append(transform.DOMove(_slideOutPosition.position, 0.5f).SetEase(Ease.InOutQuad).SetUpdate(true));
        seq.AppendCallback(() =>
        {
            _titlePage.SlideInTitlePage(0.5f, Ease.OutCubic, () =>
            {
                if (timeout.IsActive()) timeout.Kill();
                _isTransitioning = false;
            });
        });

        _currentTween = seq
            .SetUpdate(true)
            .OnKill(() =>
            {
                if (timeout.IsActive()) timeout.Kill();
                _isTransitioning = false;
            });
    }

    public void SlideOutOptionPage(float duration = 0.5f, Ease ease = Ease.InOutQuad, Action onComplete = null)
    {
        KillTween();
        _currentTween = transform.DOMove(_slideOutPosition.position, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke())
            .OnKill(() => onComplete?.Invoke()); // 중간에 Kill돼도 다음 단계 진행
    }

    public void SlideInOptionPage(float duration = 0.5f, Ease ease = Ease.OutCubic, Action onComplete = null)
    {
        KillTween();
        _currentTween = transform.DOMove(_slideInPosition.position, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke())
            .OnKill(() => onComplete?.Invoke());
    }

    private void KillTween()
    {
        if (_currentTween != null && _currentTween.IsActive())
            _currentTween.Kill();
        _currentTween = null;
    }
}
