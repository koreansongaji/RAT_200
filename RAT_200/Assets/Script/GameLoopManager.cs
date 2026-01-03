using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance;

    [Header("UI Refs")]
    public CanvasGroup blackCurtain;
    public GameObject titlePanel;
    public GameObject gameUI;

    [Header("Controllers")]
    [Tooltip("연구원 제어를 위해 연결 (ForceLeave 호출용)")]
    public ResearcherController researcher;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioManager audioManager;
    [Tooltip("2. 바람/숨소리")]
    public AudioClip suspenseBreathClip;
    [Tooltip("3. 으직! 사망 사운드")]
    public AudioClip crunchDeathClip;
    // ▼ [추가됨] 문 닫히는 소리와 불 켜지는 스위치 소리
    [Tooltip("연구원이 나갈 때 문 닫히는 소리")]
    public AudioClip doorCloseClip;
    [Tooltip("불이 다시 켜질 때 스위치/전기 소리")]
    public AudioClip lightOnClip;

    [Header("Visuals for Death")]
    [Tooltip("암전 시 껏다가, 연구원 퇴근 시 다시 켜질 방 조명들")]
    public List<Light> roomLights;
    [Tooltip("사망 시 바닥에 남을 핏자국 프리팹")]
    public GameObject bloodStainPrefab;

    [Header("Player Refs")]
    [Tooltip("플레이어 오브젝트 (죽으면 재시작)")]
    public GameObject playerObject;

    [Header("Timing Settings")]
    public float lightsOutDelay = 0.5f;
    public float breathDuration = 4.0f;
    public float aftermathDuration = 3.0f;
    public float fadeDuration = 1.5f;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1.0f;
        DOTween.KillAll();

        if (blackCurtain)
        {
            blackCurtain.gameObject.SetActive(true);
            blackCurtain.alpha = 1f;
            blackCurtain.blocksRaycasts = true;
        }

        if (!researcher) researcher = FindFirstObjectByType<ResearcherController>();
    }

    void Start()
    {
        if (blackCurtain)
        {
            blackCurtain.DOFade(0f, fadeDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => {
                    blackCurtain.gameObject.SetActive(false);
                    blackCurtain.blocksRaycasts = false;
                });
        }

        if (titlePanel) titlePanel.SetActive(true);
        if (gameUI) gameUI.SetActive(false);
    }

    public void TriggerDeath(GameObject victim)
    {
        StartCoroutine(Routine_DeathSequence(victim));
    }

    IEnumerator Routine_DeathSequence(GameObject victim)
    {
        Debug.Log($"Death Sequence Start: {victim.name}");

        // 0. 플레이어 조작 차단
        if (victim == playerObject)
        {
            var pi = victim.GetComponent<PlayerInteractor>();
            if (pi) pi.enabled = false;
        }

        // 1. 숨소리/바람소리
        if (suspenseBreathClip)
        {
            AudioManager.Instance.Play(suspenseBreathClip, AudioManager.Sound.Effect);
            yield return new WaitForSeconds(breathDuration);
        }
        else
        {
            yield return new WaitForSeconds(breathDuration);
        }

        AudioManager.Instance.StopSFX();

        // 2. 완전 암전
        ToggleRoomLights(false);
        if (researcher && researcher.spotLight) researcher.spotLight.enabled = false;
        AudioManager.Instance.Play(lightOnClip);


        // 4. 으직! (사망)
        AudioManager.Instance.Play(crunchDeathClip, AudioManager.Sound.Effect, 1f, 2f);

        // 3. 암전 공포 대기
        yield return new WaitForSeconds(lightsOutDelay);


        // 5. 피해자 처리
        Vector3 deathPos = victim.transform.position;
        victim.SetActive(false);

        if (bloodStainPrefab)
        {
            Vector3 bloodPos = deathPos;
            bloodPos.y += 0.01f;
            Instantiate(bloodStainPrefab, bloodPos, Quaternion.Euler(90, 0, 0));
        }

        // 6. ★ [수정됨] 연구원 강제 퇴근 + 사운드 연출
        if (researcher)
        {
            // (1) 문 닫히는 소리 먼저 재생
            if (doorCloseClip) AudioManager.Instance.Play(doorCloseClip, AudioManager.Sound.Effect);

            // (2) 연구원 퇴근 시작 (문 닫는 애니메이션 0.5초 + 불 켜기 대기 0.5초가 내부에서 돔)
            researcher.ForceLeave();

            // (3) 문이 닫히는 시간(0.5초)만큼 대기
            yield return new WaitForSeconds(0.5f);

            // (4) 불 켜지는 소리 재생 (연구원 스크립트가 불을 켜는 타이밍과 일치)
            if (lightOnClip) AudioManager.Instance.Play(lightOnClip, AudioManager.Sound.Effect);
        }
        else
        {
            // 연구원 없을 때 예외 처리
            ToggleRoomLights(true);
            if (lightOnClip) AudioManager.Instance.Play(lightOnClip, AudioManager.Sound.Effect);
        }

        // 7. 현장 유지
        yield return new WaitForSeconds(aftermathDuration);

        // 8. 분기 처리
        if (victim == playerObject)
        {
            if (blackCurtain)
            {
                blackCurtain.gameObject.SetActive(true);
                blackCurtain.blocksRaycasts = true;
                yield return blackCurtain.DOFade(1f, fadeDuration).WaitForCompletion();
            }

            if (audioManager) audioManager.KillAllSounds();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.Log("동료 사망. 게임은 계속됩니다.");
        }
    }

    void ToggleRoomLights(bool isOn)
    {
        if (roomLights != null)
        {
            foreach (var light in roomLights)
            {
                if (light) light.enabled = isOn;
            }
        }
    }
}