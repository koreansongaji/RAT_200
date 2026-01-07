using UnityEngine;

[DefaultExecutionOrder(-200)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [Tooltip("고정할 목표 프레임 (기본 60)")]
    [SerializeField] private int targetFrameRate = 60;

    [Tooltip("고정할 해상도 너비")]
    [SerializeField] private int resolutionWidth = 1920;

    [Tooltip("고정할 해상도 높이")]
    [SerializeField] private int resolutionHeight = 1080;

    [Tooltip("전체 화면 모드 여부")]
    [SerializeField] private bool isFullScreen = true;

    private void Awake()
    {
        // --- 1. 싱글톤 패턴 구현 ---
        if (Instance != null && Instance != this)
        {
            // 이미 다른 GameManager가 존재한다면 파괴 (중복 방지)
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 씬이 변경되어도 파괴되지 않도록 설정
        DontDestroyOnLoad(gameObject);

        // --- 2. 게임 기본 세팅 적용 ---
        ApplySettings();
    }

    private void ApplySettings()
    {
        // [프레임 속도 고정]
        // VSync(수직 동기화)가 켜져 있으면 targetFrameRate가 무시될 수 있습니다.
        // 0: VSync 끔 (코드 제어), 1: 모니터 주사율 동기화
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        // [해상도 고정]
        // FullScreenMode.FullScreenWindow: 테두리 없는 전체 창 모드 (Alt-Tab 전환이 부드러움)
        // FullScreenMode.ExclusiveFullScreen: 독점 전체 화면 (성능상 이점이 있을 수 있음)
        FullScreenMode mode = isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        Screen.SetResolution(resolutionWidth, resolutionHeight, mode);

        Debug.Log($"[GameManager] Initialized. Resolution: {resolutionWidth}x{resolutionHeight}, FPS: {targetFrameRate}");
    }

    // (참고) 나중에 게임 종료 기능 등이 필요하면 여기에 추가
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}