using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // 혹시 기본 Text를 쓴다면 필요
using TMPro;          // ★ [New] TextMeshPro 사용 시 필요
using DG.Tweening;

[DefaultExecutionOrder(-100)]
public class TitlePage : MonoBehaviour
{
    [SerializeField] private Transform _slideOutPosition;
    [SerializeField] private Transform _slideInPosition;
    [SerializeField] private OptionPage _optionPage;
    [SerializeField] private HowToPlayPage _howToPlayPage;
    [SerializeField] private GameObject _uiBlocker;

    // ★ [New] 버튼 텍스트를 바꾸기 위한 참조 변수
    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI _startButtonText;
    // 만약 Legacy Text를 쓴다면: [SerializeField] private Text _startButtonText;

    [Header("Events")]
    public UnityEvent onGameStart;

    private Tween _currentTween;
    private bool _isTransitioning;
    private bool _isPageHidden;

    // ★ [New] 게임이 한 번이라도 시작되었는지 체크하는 플래그
    private bool _hasGameStarted = false;

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
            _bgmController = FindFirstObjectByType<BgmController>();
        }
    }

    private void Start()
    {
        _isPageHidden = false;
        SetBlocker(true);

        // ★ [New] 씬이 처음 로드되었을 때는 무조건 START
        _hasGameStarted = false;
        UpdateStartButtonText();

        SlideInTitlePage();
        if (_optionPage) _optionPage.SlideOutOptionPage();

        if (_titleBGM == null) _titleBGM = Resources.Load<AudioClip>("Sounds/BGM/bgm_title");
        AudioManager.Instance.Play(_titleBGM, AudioManager.Sound.BGM);
    }

    private void Update()
    {
        // 1. 게임 플레이 중일 때 -> ESC 누르면 메뉴 열기 (기존 로직)
        if (_isPageHidden)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 마이크로 줌(퍼즐) 중이면 메뉴 안 열림 (퍼즐 나가기가 우선)
                if (IsAnyMicroZoomActive()) return;

                OpenMenu();
            }
        }
        // 2. 메뉴(타이틀)가 열려 있을 때 -> ESC 누르면 메뉴 닫기 (새로운 로직)
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // ★ 핵심 조건:
                // (1) 게임이 이미 시작된 상태여야 함 (START 모드에선 ESC로 못 끔)
                // (2) 화면 전환 중(슬라이드 중)이 아니어야 함
                if (_hasGameStarted && !_isTransitioning)
                {
                    // StartGame() 내부에서 _hasGameStarted가 true면
                    // 자동으로 'Resume(단순 닫기)' 로직이 실행됩니다.
                    StartGame();
                }
            }
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

        // ★ [New] 메뉴가 열릴 때 텍스트 갱신 (이미 시작했다면 RESUME이 됨)
        UpdateStartButtonText();

        SlideInTitlePage();
    }

    // ★ [New] 상태에 따라 텍스트를 바꿔주는 헬퍼 함수
    private void UpdateStartButtonText()
    {
        if (_startButtonText == null) return;

        // 게임이 이미 시작된 상태(_hasGameStarted)라면 RESUME, 아니면 START
        _startButtonText.text = _hasGameStarted ? "RESUME" : "START";
    }

    public void StartGame()
    {
        if (_isTransitioning) return;

        // ★ [New] 이미 게임이 시작된 상태에서 'RESUME'을 누른 거라면
        // 이벤트를 다시 발동시키지 않고 단순히 페이지만 닫아야 할 수도 있습니다.
        // 하지만 만약 단순히 닫는 게 아니라, 매번 연출을 원한다면 아래 로직을 조정해야 합니다.
        // 여기서는 "Resume 시에는 오프닝 연출(뚜껑 열기 등)을 스킵하고 페이지만 닫기"로 구현해 봅니다.

        if (_hasGameStarted)
        {
            // [Resume 로직]
            // 이미 게임 중이므로 onGameStart(오프닝 연출)은 실행하지 않음
            CloseTitleOnly();
        }
        else
        {
            // [First Start 로직]
            _isTransitioning = true;
            _hasGameStarted = true; // 이제부터는 게임 중인 상태

            if (_bgmController != null)
            {
                try { _bgmController.PlayFirstStep(); }
                catch (Exception e) { Debug.LogWarning($"BGM Error: {e.Message}"); }
            }

            onGameStart?.Invoke(); // 인트로/오프닝 실행

            SlideOutTitlePage(onComplete: () =>
            {
                _isPageHidden = true;
                _isTransitioning = false;
                SetBlocker(false);
            });
        }
    }

    // ★ [New] Resume 전용 닫기 (이벤트 발생 X)
    private void CloseTitleOnly()
    {
        _isTransitioning = true;
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

    public void HowToPlay()
    {
        if (_isTransitioning || _howToPlayPage == null) return;
        _isTransitioning = true;

        // 타이틀이 나가고(SlideOut) -> 완료되면 -> 설명창이 들어옴(SlideIn)
        SlideOutTitlePage(onComplete: () =>
        {
            _howToPlayPage.SlideIn(onComplete: () =>
            {
                _isTransitioning = false;
            });
        });
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