using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class OptionPage : MonoBehaviour
{
    [SerializeField] private Transform _slideOutPosition;
    [SerializeField] private Transform _slideInPosition;
    [SerializeField] private TitlePage _titlePage;

    [Header("Volume Sliders")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private Slider _bgmVolumeSlider;
    [SerializeField] private Slider _effectVolumeSlider;

    /// <summary>
    /// 옵션 페이지가 화면에 표시될 때의 기준 위치를 반환합니다.
    /// </summary>
    public Vector3 GetInPosition() => _slideInPosition.position;

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

        // 슬라이더 리스너 등록
        if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChange);
        if (_bgmVolumeSlider != null) _bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChange);
        if (_effectVolumeSlider != null) _effectVolumeSlider.onValueChanged.AddListener(OnEffectVolumeChange);
    }

    private void Start()
    {
        // 슬라이더 초기값 설정
        if (_masterVolumeSlider != null) _masterVolumeSlider.value = OptionManager.Instance.OptionData.MastersoundVolume;
        if (_bgmVolumeSlider != null) _bgmVolumeSlider.value = OptionManager.Instance.OptionData.BGMsoundVolume;
        if (_effectVolumeSlider != null) _effectVolumeSlider.value = OptionManager.Instance.OptionData.EffectsoundVolume;

        // 초기 볼륨 적용은 OptionManager.Start() 또는 여기서 직접 Slider 이벤트를 통해 발생함
    }
    public void OnMasterVolumeChange(float volume)
    {
        OptionManager.Instance.OnMasterVolumeChange(volume);
    }
    public void OnEffectVolumeChange(float volume)
    {
        OptionManager.Instance.OnEffectVolumeChange(volume);
    }
    public void OnBgmVolumeChange(float volume)
    {
        OptionManager.Instance.OnBgmVolumeChange(volume);
    }
    /// <summary>
    /// 옵션 페이지를 닫고 타이틀 페이지로 돌아갑니다.
    /// </summary>
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

    /// <summary>
    /// 옵션 페이지를 설정된 화면 안 위치로 이동시킵니다.
    /// </summary>
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
