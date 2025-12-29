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
    private bool _isPageHidden;

    [SerializeField] private AudioClip _titleBGM;
    [SerializeField] private BgmController _bgmController;
    
    private void Awake()
    {
        if (_slideOutPosition == null || _slideInPosition == null)
        {
            Debug.LogError("[TitlePage] Positions are not assigned.");
            enabled = false;
            return;
        }
        transform.position = _slideOutPosition.position;

        if (_bgmController == null)
        {
            Debug.LogError("[TitlePage] BgmController is not assigned.");
            _bgmController = FindFirstObjectByType<BgmController>();
        }
    }

    private void Start()
    {
        // 처음에 타이틀을 보여줌 (Time.timeScale 제거)
        _isPageHidden = false;
        
        SlideInTitlePage();
        if(_titleBGM == null) _titleBGM = Resources.Load<AudioClip>("Sounds/BGM/bgm_title");
        AudioManager.Instance.Play(_titleBGM, AudioManager.Sound.BGM);
    }

    private void Update()
    {
        // 1. 게임 중 ESC를 누르면 타이틀을 켬 (Time.timeScale 제거)
        if (_isPageHidden && Input.GetKeyDown(KeyCode.Escape))
        {
            OpenMenu();
        }
    }

    private void OpenMenu()
    {
        _isPageHidden = false;
        SlideInTitlePage();
    }

    public void StartGame()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        
        // 1. 게임 시작 버튼을 누르면 BGM을 기본(0~80)으로 교체
        if (_bgmController != null)
        {
            _bgmController.PlayFirstStep();
        }
        else
        {
            Debug.LogWarning("[TitlePage] BgmController is not assigned.");
        }

        // 2. 타이틀을 치우고 게임 상태로 전환 (Time.timeScale 제거)
        SlideOutTitlePage(onComplete: () => 
        {
            _isPageHidden = true;
            _isTransitioning = false;
        });
    }

    // 옵션 페이지 열기 (위치만 이동)
    public void OnOpenOptionPage()
    {
        if (_isTransitioning || _optionPage == null) return;
        _isTransitioning = true;

        SlideOutTitlePage(onComplete: () =>
        {
            _optionPage.SlideInOptionPage(onComplete: () => 
            {
                _isTransitioning = false;
            });
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
        /// <summary>
        /// 애플리케이션을 종료합니다. 에디터에서는 재생 모드를 종료합니다.
        /// </summary>
        public void QuitGame()
        {
            #if UNITY_EDITOR
                // 유니티 에디터에서 실행 중일 때
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                // 실제 빌드된 게임에서 실행 중일 때
                Application.Quit();
            #endif
        }
}
