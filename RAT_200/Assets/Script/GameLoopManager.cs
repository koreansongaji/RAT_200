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
    public AudioClip scientistCatchClip;

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

        // 연구원 자동 찾기 (혹시 인스펙터 누락 대비)
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

    // ★ [핵심] 누군가(플레이어 혹은 동료) 잡혔을 때 호출되는 공용 함수
    public void TriggerDeath(GameObject victim)
    {
        StartCoroutine(Routine_DeathSequence(victim));
    }

    IEnumerator Routine_DeathSequence(GameObject victim)
    {
        Debug.Log($"Death Sequence Start: {victim.name}");

        // 0. 플레이어 조작 차단 (만약 피해자가 플레이어라면)
        if (victim == playerObject)
        {
            var pi = victim.GetComponent<PlayerInteractor>();
            if (pi) pi.enabled = false;
        }

        // 1. 숨소리/바람소리 재생
        if (suspenseBreathClip)
        {
            AudioManager.Instance.Play(suspenseBreathClip, AudioManager.Sound.Effect);
            yield return new WaitForSeconds(3.0f);
        }
        else
        {
            yield return new WaitForSeconds(3.0f);
        }

        // 2. 완전 암전 (방 조명 OFF + 연구원 손전등 OFF)
        // 연구원 손전등은 연구원 스크립트에서 제어하거나 여기서 강제로 끕니다.
        ToggleRoomLights(false);
        if (researcher && researcher.spotLight) researcher.spotLight.enabled = false;

        // 3. 암전 공포 대기
        yield return new WaitForSeconds(lightsOutDelay);

        AudioManager.Instance.StopSFX();
        // 4. 으직! (사망)
        AudioManager.Instance.Play(crunchDeathClip, AudioManager.Sound.Effect, 1f, 2f);

        // 5. 피해자 처리 (숨기기 & 핏자국)
        Vector3 deathPos = victim.transform.position;
        victim.SetActive(false); // 쥐 사라짐

        if (bloodStainPrefab)
        {
            Vector3 bloodPos = deathPos;
            bloodPos.y += 0.01f;
            Instantiate(bloodStainPrefab, bloodPos, Quaternion.Euler(90, 0, 0));
        }

        // 6. ★ [핵심] 연구원 강제 퇴근 (ForceLeave)
        // 이 함수가 문을 닫고, "방 불을 깜빡이며 켜주는 역할"을 수행합니다.
        if (researcher)
        {
            researcher.ForceLeave();
        }
        else
        {
            // 만약 연구원 스크립트가 없다면 비상용으로 불만 켬
            ToggleRoomLights(true);
        }

        // 7. 현장 유지 (참혹한 광경 확인)
        yield return new WaitForSeconds(aftermathDuration);

        // 8. 분기 처리: 플레이어면 재시작, 아니면(동료면) 게임 계속
        if (victim == playerObject)
        {
            // 플레이어 사망 -> 재시작 시퀀스
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
            // 동료 사망 -> 그냥 진행 (연구원은 이미 떠났음)
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