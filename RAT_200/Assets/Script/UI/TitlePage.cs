using System;
using UnityEngine;
using DG.Tweening;

// ★ [핵심] 이 스크립트를 다른 것들보다 먼저 실행시켜서 
// 마이크로 줌이 꺼지기 전에 상태를 감지하도록 함 (-100으로 우선순위 높임)
[DefaultExecutionOrder(-100)]
public class TitlePage : MonoBehaviour
{
    [SerializeField] private Transform _slideOutPosition;
    [SerializeField] private Transform _slideInPosition;
    [SerializeField] private OptionPage _optionPage;

    // ★ [추가] 뒤쪽 클릭 방지용 투명 가림막
    [Header("UI Blocker")]
    [Tooltip("타이틀/옵션이 켜져있을 때 뒤쪽 클릭을 막는 투명 패널")]
    [SerializeField] private GameObject _uiBlocker;

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
        // 처음에 타이틀을 보여줌
        _isPageHidden = false;

        // ★ 시작 시 가림막 켜기 (뒤쪽 클릭 방지)
        SetBlocker(true);

        SlideInTitlePage();
        if (_titleBGM == null) _titleBGM = Resources.Load<AudioClip>("Sounds/BGM/bgm_title");
        AudioManager.Instance.Play(_titleBGM, AudioManager.Sound.BGM);
    }

    private void Update()
    {
        // 1. 게임 중 ESC를 누르면 타이틀을 켬
        if (_isPageHidden && Input.GetKeyDown(KeyCode.Escape))
        {
            // ★ [수정] 마이크로 줌 상태라면 타이틀 열지 않음 (줌 나가기가 우선)
            // 실행 순서가 -100이라서 줌이 꺼지기 전에 여기서 먼저 걸러짐
            if (IsAnyMicroZoomActive()) return;

            OpenMenu();
        }
    }

    // ★ [추가] 현재 활성화된 마이크로 줌 세션이 있는지 확인
    private bool IsAnyMicroZoomActive()
    {
        // 씬에 있는 모든 MicroZoomSession을 찾아서 확인
        var sessions = FindObjectsByType<MicroZoomSession>(FindObjectsSortMode.None);
        foreach (var session in sessions)
        {
            // MicroZoomSession에 'InMicro' 프로퍼티가 있다고 가정 (ChemMixingStation 참조)
            if (session != null && session.InMicro)
            {
                return true;
            }
        }
        return false;
    }

    private void OpenMenu()
    {
        _isPageHidden = false;

        // ★ 메뉴 열 때 가림막 켜기
        SetBlocker(true);

        SlideInTitlePage();
    }

    public void StartGame()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        if (_bgmController != null)
        {
            _bgmController.PlayFirstStep();
        }

        // 2. 타이틀을 치우고 게임 상태로 전환
        SlideOutTitlePage(onComplete: () =>
        {
            _isPageHidden = true;
            _isTransitioning = false;

            // ★ 게임 시작(화면 나감) 완료 시 가림막 끄기
            // (옵션 페이지로 갈 때는 끄지 않음)
            SetBlocker(false);
        });
    }

    // 옵션 페이지 열기 (위치만 이동)
    public void OnOpenOptionPage()
    {
        if (_isTransitioning || _optionPage == null) return;
        _isTransitioning = true;

        // ★ 옵션 갈 때는 가림막 유지해야 하므로 끄지 않음
        SlideOutTitlePage(onComplete: () =>
        {
            _optionPage.SlideInOptionPage(onComplete: () =>
            {
                _isTransitioning = false;
            });
        });
    }

    // 가림막 제어 헬퍼
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

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}