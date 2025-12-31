using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

// ★ 우선순위는 그대로 유지 (마이크로 줌 감지용)
[DefaultExecutionOrder(-100)]
public class TitlePage : MonoBehaviour
{
    [SerializeField] private Transform _slideOutPosition;
    [SerializeField] private Transform _slideInPosition;
    [SerializeField] private OptionPage _optionPage;
    [SerializeField] private GameObject _uiBlocker;

    // 게임 시작 시 외부(인트로 매니저)로 신호를 보낼 이벤트
    [Header("Events")]
    public UnityEvent onGameStart;

    // 캐싱 변수 삭제! (_cachedOutPos 등 제거)

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

        // ★ [수정] 캐싱하지 않고, 바로 위치를 잡습니다.
        // Awake 시점엔 UI 좌표가 불안정할 수 있지만, 초기 배치용으로는 일단 둡니다.
        // 실제 중요한 이동(Start 이후)은 아래 함수들에서 처리합니다.
        transform.position = _slideOutPosition.position;

        if (_bgmController == null)
        {
            _bgmController = FindFirstObjectByType<BgmController>();
        }
    }

    private void Start()
    {
        _isPageHidden = false;
        SetBlocker(true);

        // ★ Start 시점에는 UI 레이아웃이 잡혔을 것이므로 정상적으로 들어옵니다.
        SlideInTitlePage();

        if (_titleBGM == null) _titleBGM = Resources.Load<AudioClip>("Sounds/BGM/bgm_title");
        AudioManager.Instance.Play(_titleBGM, AudioManager.Sound.BGM);
    }

    private void Update()
    {
        if (_isPageHidden && Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsAnyMicroZoomActive()) return;
            OpenMenu();
        }
    }

    private bool IsAnyMicroZoomActive()
    {
        var sessions = FindObjectsByType<MicroZoomSession>(FindObjectsSortMode.None);
        foreach (var session in sessions)
        {
            if (session != null && session.InMicro) return true;
        }
        return false;
    }

    private void OpenMenu()
    {
        _isPageHidden = false;
        SetBlocker(true);
        SlideInTitlePage();
    }

    public void StartGame()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        // BGM 에러가 나도 게임은 시작되도록 예외 처리
        if (_bgmController != null)
        {
            try { _bgmController.PlayFirstStep(); }
            catch (Exception e) { Debug.LogWarning($"BGM Error: {e.Message}"); }
        }

        // 인트로 연출 시작 (뚜껑 열기 등)
        onGameStart?.Invoke();

        // 2. 타이틀을 치우고 게임 상태로 전환
        SlideOutTitlePage(onComplete: () =>
        {
            _isPageHidden = true;
            _isTransitioning = false;
            SetBlocker(false);
        });
    }

    public void OnOpenOptionPage()
    {
        if (_isTransitioning || _optionPage == null) return;
        _isTransitioning = true;

        // ★ [수정] 람다 식 오류 해결 (onComplete 명시)
        SlideOutTitlePage(onComplete: () =>
        {
            _optionPage.SlideInOptionPage(onComplete: () =>
            {
                _isTransitioning = false;
            });
        });
    }

    void SetBlocker(bool active)
    {
        if (_uiBlocker) _uiBlocker.SetActive(active);
    }

    // ★ [수정] Transform.position을 직접 사용하여 "현재 시점의 정확한 위치"로 이동
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}