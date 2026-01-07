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

    public Vector3 GetInPosition() => _slideInPosition.position;

    private Tween _currentTween;
    private bool _isTransitioning;

    private void Awake()
    {
        // (기존 코드 동일)
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
        // 슬라이더 초기값 설정 (콜백이 호출되지 않도록)
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.value = OptionManager.Instance.OptionData.MastersoundVolume;
            _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChange);
        }
        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.value = OptionManager.Instance.OptionData.BGMsoundVolume;
            _bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChange);
        }
        if (_effectVolumeSlider != null)
        {
            _effectVolumeSlider.value = OptionManager.Instance.OptionData.EffectsoundVolume;
            _effectVolumeSlider.onValueChanged.AddListener(OnEffectVolumeChange);
        }
    }

    // ★ [추가] 옵션 창이 켜져있을 때 ESC 처리
    private void Update()
    {
        // 화면 안쪽(_slideInPosition)에 거의 도착했을 때만 ESC 작동
        if (_slideInPosition && Vector3.Distance(transform.position, _slideInPosition.position) < 0.1f)
        {
            // 전환 중이 아닐 때만
            if (!_isTransitioning && Input.GetKeyDown(KeyCode.Escape))
            {
                BackToTitle();
            }
        }
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

    public void BackToTitle()
    {
        if (_isTransitioning || _titlePage == null) return;

        _isTransitioning = true;
        KillTween();

        // (기존 애니메이션 로직 유지)
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
            .OnKill(() => onComplete?.Invoke());
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