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
    private bool _isPageHidden; // 페이지가 숨겨졌는지 상태 확인

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
    
    // TODO: 지금은 Update에서 Esc키 받는데 InputManager로 바꾸면 좋을 것 같음
    private void Update()
    {
        // 페이지가 숨겨진 상태에서 ESC를 누르면 다시 나타남
        if (_isPageHidden && Input.GetKeyDown(KeyCode.Escape))
        {
            SlideInTitlePage(onComplete: () => _isPageHidden = false);
        }

        // TitlePage나 OptionPage가 화면에 있는지 확인하여 TimeScale 조절
        UpdateTimeScale();
    }

    /// <summary>
    /// UI 페이지들의 가시성 상태에 따라 게임의 시간 흐름(TimeScale)을 제어합니다.
    /// </summary>
    private void UpdateTimeScale()
    {
        bool isTitleVisible = (transform.position - _slideInPosition.position).sqrMagnitude < 0.01f;
        bool isOptionVisible = _optionPage != null && (_optionPage.transform.position - _optionPage.GetInPosition()).sqrMagnitude < 0.01f;

        if (isTitleVisible || isOptionVisible)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// 게임을 시작합니다. 타이틀 페이지를 화면 밖으로 밀어내고 게임을 활성화합니다.
    /// </summary>
    public void StartGame()
    {
        if (_isTransitioning) return;
        
        SlideOutTitlePage(onComplete: () => 
        {
            _isPageHidden = true;
        });
    }

    /// <summary>
    /// 애플리케이션을 종료합니다. 에디터에서는 재생 모드를 종료합니다.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 옵션 페이지를 엽니다. 현재 타이틀 페이지를 밖으로 밀어낸 후 옵션 페이지를 불러옵니다.
    /// </summary>
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
    /// <summary>
    /// 타이틀 페이지를 설정된 화면 밖 위치로 이동시킵니다.
    /// </summary>
    public void SlideOutTitlePage(float duration = 0.5f, Ease ease = Ease.InOutQuad, Action onComplete = null)
    {
        KillTween();
        _currentTween = transform.DOMove(_slideOutPosition.position, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke())
            .OnKill(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 타이틀 페이지를 설정된 화면 안 위치로 이동시킵니다.
    /// </summary>
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
