using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance;

    [Header("UI Refs")]
    public CanvasGroup blackCurtain; // 화면 전체를 덮는 검은 이미지 (Raycast Block 체크)
    public GameObject titlePanel;    // 타이틀 화면 UI 패널
    public GameObject gameUI;        // 인게임 UI (체력바 등)

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip ratDeathClip;   // 쥐 죽는 소리

    [Header("Settings")]
    public float deathDelay = 2.0f;  // 죽는 소리 듣는 시간
    public float fadeDuration = 1.5f;// 암전되는 시간

    void Awake()
    {
        Instance = this;

        Time.timeScale = 1.0f; // 0으로 멈췄던 시간을 다시 흐르게 함
        DOTween.KillAll();     // 이전 씬에서 남은 트윈이 있다면 제거 (충돌 방지)
    }

    void Start()
    {
        // 씬 시작 시: 검은 화면 -> 투명하게 (Fade In)
        if (blackCurtain)
        {
            blackCurtain.gameObject.SetActive(true);
            blackCurtain.alpha = 1f;
            blackCurtain.DOFade(0f, fadeDuration).OnComplete(() => {
                blackCurtain.gameObject.SetActive(false); // 다 걷히면 끄기
            });
        }

        // 타이틀 패널 켜기 (리로드 되었으므로 타이틀부터 시작)
        if (titlePanel) titlePanel.SetActive(true);
        if (gameUI) gameUI.SetActive(false);
    }

    // ★ 연구원에게 잡혔을 때 호출할 함수
    public void TriggerGameOver()
    {
        StartCoroutine(Routine_GameOverSequence());
    }

    IEnumerator Routine_GameOverSequence()
    {
        Debug.Log("GAME OVER Sequence Start");

        // 1. 쥐 죽는 소리 재생
        if (sfxSource && ratDeathClip)
        {
            sfxSource.PlayOneShot(ratDeathClip);
        }

        // 2. 비명 지르고 잠시 대기 (화면은 그대로)
        yield return new WaitForSeconds(deathDelay);

        // 3. 암전 시작 (Fade Out)
        if (blackCurtain)
        {
            blackCurtain.gameObject.SetActive(true);
            // 1초 동안 검게 변함
            yield return blackCurtain.DOFade(1f, fadeDuration).WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // 4. 씬 리로드 (완전 초기화)
        // 현재 씬의 이름을 가져와서 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 타이틀에서 'Game Start' 버튼 누르면 호출
    public void GameStart()
    {
        if (titlePanel) titlePanel.SetActive(false);
        if (gameUI) gameUI.SetActive(true);
        // 필요한 경우 플레이어 조작 잠금 해제 등 수행
    }
}