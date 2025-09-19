using System;
using UnityEngine;
using DG.Tweening;

public class TitlePage : MonoBehaviour
{
    [SerializeField] private Transform _slideOutPosition;
    [SerializeField] private Transform _slideInPosition;
    [SerializeField] private OptionPage _optionPage;
    
    private Tween _currentTween;
    private bool _isTransitioning;
    
    [SerializeField] private AudioClip _audioClip;
    private void Awake()
    {
        if (_slideOutPosition == null || _slideInPosition == null)
        {
            Debug.LogError("[TitlePage] Positions are not assigned.");
            enabled = false;
            return;
        }
        transform.position = _slideOutPosition.position;
    }

    private void Start()
    {
        SlideInTitlePage();
        AudioManager.Instance.Play(_audioClip, AudioManager.Sound.BGM);
    }

    public void OnOpenOptionPage()
    {
        if (_isTransitioning || _optionPage == null) return;
        _isTransitioning = true;

        KillTween();

        // 타임아웃(실패 대비)
        var timeout = DOVirtual.DelayedCall(1.2f, () => { _isTransitioning = false; }, ignoreTimeScale: true)
            .SetUpdate(true);

        // 이미 같은 위치면 즉시 다음 단계
        if ((transform.position - _slideOutPosition.position).sqrMagnitude < 0.0001f)
        {
            _optionPage.SlideInOptionPage(0.5f, Ease.OutCubic, () =>
            {
                if (timeout.IsActive()) timeout.Kill();
                _isTransitioning = false;
            });
            return;
        }

        // 직렬 시퀀스
        var seq = DOTween.Sequence();
        seq.Append(transform.DOMove(_slideOutPosition.position, 0.5f).SetEase(Ease.InOutQuad).SetUpdate(true));
        seq.AppendCallback(() =>
        {
            _optionPage.SlideInOptionPage(0.5f, Ease.OutCubic, () =>
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
    //--------------- 페이지 이동 함수 (DOTween) ---------------------
    
    public void SlideOutTitlePage(float duration = 0.5f, Ease ease = Ease.InOutQuad, Action onComplete = null)
    {
        KillTween();
        _currentTween = transform.DOMove(_slideOutPosition.position, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke())
            .OnKill(() => onComplete?.Invoke());
    }

    public void SlideInTitlePage(float duration = 0.5f, Ease ease = Ease.OutCubic, Action onComplete = null)
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
