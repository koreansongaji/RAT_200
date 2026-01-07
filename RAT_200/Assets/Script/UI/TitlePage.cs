using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[DefaultExecutionOrder(-100)]
public class TitlePage : MonoBehaviour
{
    [SerializeField] private Transform _slideOutPosition;
    [SerializeField] private Transform _slideInPosition;
    [SerializeField] private OptionPage _optionPage;
    [SerializeField] private HowToPlayPage _howToPlayPage;
    [SerializeField] private GameObject _uiBlocker;

    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI _startButtonText;

    [Header("Events")]
    public UnityEvent onGameStart;

    private Tween _currentTween;
    private bool _isTransitioning;
    private bool _isPageHidden;

    public bool IsMenuOpen => !_isPageHidden;

    // ★ [수정] 서브 메뉴(옵션, 설명)가 열려있는지 체크하는 플래그
    private bool _isSubMenuOpen = false;

    private bool _hasGameStarted = false;

    [SerializeField] private AudioClip _titleBGM;
    [SerializeField] private BgmController _bgmController;


    private InventoryUI _inventoryUI;
    private ItemInspectorUI _itemInspectorUI;

    private void Awake()
    {
        // (기존 코드 동일)
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

        _inventoryUI = FindFirstObjectByType<InventoryUI>();
        _itemInspectorUI = FindFirstObjectByType<ItemInspectorUI>();
    }

    private void Start()
    {
        _isPageHidden = false;
        SetBlocker(true);

        _hasGameStarted = false;
        UpdateStartButtonText();

        // 시작할 때는 서브 메뉴 닫힘 상태
        _isSubMenuOpen = false;

        SlideInTitlePage();
        if (_optionPage) _optionPage.SlideOutOptionPage();
        if (_howToPlayPage) _howToPlayPage.SlideOut();

        if (_titleBGM == null) _titleBGM = Resources.Load<AudioClip>("Sounds/BGM/bgm_title");
        AudioManager.Instance.Play(_titleBGM, AudioManager.Sound.BGM);
    }

    private void Update()
    {
        // ★ [핵심 로직 변경]
        // 1. 다른 팝업 UI가 열려있는지 검사
        bool isPopupOpen = false;

        if (_itemInspectorUI != null && _itemInspectorUI.IsOpen) isPopupOpen = true;
        else if (_inventoryUI != null && _inventoryUI.IsOpen) isPopupOpen = true;

        // 팝업이 하나라도 열려있으면 타이틀의 ESC 동작은 무시합니다.
        // (각 팝업 스크립트에서 스스로 닫는 처리를 함)
        if (isPopupOpen) return;

        // --- 이하 기존 타이틀 ESC 로직 ---

        // 1. 게임 플레이 중일 때 -> ESC 누르면 메뉴 열기
        if (_isPageHidden)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsAnyMicroZoomActive()) return;
                OpenMenu();
            }
        }
        // 2. 메뉴(타이틀)가 열려 있을 때
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 서브 메뉴가 열려있다면 타이틀의 ESC 무시
                if (_isSubMenuOpen) return;

                if (_hasGameStarted && !_isTransitioning)
                {
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
        _isSubMenuOpen = false; // 메뉴 열릴 땐 서브 메뉴 닫힌 상태
        SetBlocker(true);
        UpdateStartButtonText();
        SlideInTitlePage();
    }

    private void UpdateStartButtonText()
    {
        if (_startButtonText == null) return;
        _startButtonText.text = _hasGameStarted ? "RESUME" : "START";
    }

    public void StartGame()
    {
        if (_isTransitioning) return;

        if (_hasGameStarted)
        {
            CloseTitleOnly();
        }
        else
        {
            _isTransitioning = true;
            _hasGameStarted = true;

            if (_bgmController != null)
            {
                try { _bgmController.PlayFirstStep(); }
                catch (Exception e) { Debug.LogWarning($"BGM Error: {e.Message}"); }
            }

            onGameStart?.Invoke();

            SlideOutTitlePage(onComplete: () =>
            {
                _isPageHidden = true;
                _isTransitioning = false;
                SetBlocker(false);
            });
        }
    }

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

        // ★ [수정] 옵션 창이 열림을 표시
        _isSubMenuOpen = true;

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
            .OnComplete(() =>
            {
                // ★ [수정] 타이틀이 다시 들어왔다면 서브 메뉴는 닫힌 것임
                _isSubMenuOpen = false;
                onComplete?.Invoke();
            })
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

        // ★ [수정] 설명 창이 열림을 표시
        _isSubMenuOpen = true;

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