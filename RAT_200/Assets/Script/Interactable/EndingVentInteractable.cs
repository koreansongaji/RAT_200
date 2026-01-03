using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class EndingVentInteractable : BaseInteractable
{
    [Header("Ending Settings")]
    [Tooltip("엔딩 시 활성화될 방 전체를 비추는 와이드 카메라")]
    public CinemachineCamera roomWideCamera;
    [Tooltip("카메라가 완전히 전환되는 데 걸리는 시간")]
    public float cameraTransitionDelay = 2.0f;

    [Header("Audio Assets")]
    [Tooltip("벤트 클릭 시 시작될 엔딩 BGM")]
    public AudioClip endingBgm;
    [Tooltip("컬러 이미지가 나올 때 재생될 효과음")]
    public AudioClip endingSfx;

    [Header("UI References (Canvas Groups)")]
    [Tooltip("순서대로 보여줄 컬러 이미지들 (총 4개 연결: 1, 2, 3, 4번 순서)")]
    public CanvasGroup[] endingImages; // ★ 배열로 변경됨

    public CanvasGroup whiteCurtainGroup;   // 흰색 배경
    public CanvasGroup creditsGroup;        // Credits 텍스트
    public CanvasGroup thankYouGroup;       // Thank You 텍스트
    public CanvasGroup blackCurtainGroup;   // 마지막 암전

    [Header("Timing Settings")]
    [Tooltip("각 이미지가 페이드 인 되는 시간")]
    public float overlayFadeDuration = 2.0f;

    [Tooltip("다음 이미지가 나오기 전까지 대기하는 시간")]
    public float imageInterval = 2.0f;

    [Tooltip("4번째 이미지가 깜빡! 하고 보이는 짧은 시간")]
    public float flashDuration = 0.15f;

    [Tooltip("3번째 이미지가 다시 보인 후 흰색 커튼 전까지 유지 시간")]
    public float finalImageHoldDuration = 3.0f;

    public float whiteFadeDuration = 2.0f;
    public float textFadeDuration = 1.0f;
    public float textStayDuration = 2.5f;
    public float finalBlackFadeDuration = 2.0f;

    private bool _isEndingStarted = false;
    private ResearcherController _researcher;

    void Awake()
    {
        _researcher = FindFirstObjectByType<ResearcherController>();
    }

    void Start()
    {
        // 이미지 배열 초기화
        if (endingImages != null)
        {
            foreach (var img in endingImages) InitCanvasGroup(img);
        }

        InitCanvasGroup(whiteCurtainGroup);
        InitCanvasGroup(creditsGroup);
        InitCanvasGroup(thankYouGroup);
        InitCanvasGroup(blackCurtainGroup);

        if (roomWideCamera) roomWideCamera.Priority = 10;
    }

    void InitCanvasGroup(CanvasGroup cg)
    {
        if (cg)
        {
            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
            cg.blocksRaycasts = false;
        }
    }

    public override bool CanInteract(PlayerInteractor i) => !_isEndingStarted;

    public override void Interact(PlayerInteractor i)
    {
        if (_isEndingStarted) return;

        CleanUpGameEnvironment();
        StartCoroutine(Routine_EndingSequence(i.gameObject));
    }

    void CleanUpGameEnvironment()
    {
        if (_researcher != null && _researcher.CurrentState != ResearcherController.State.Idle)
        {
            _researcher.ForceLeave();
        }

        if (AudioManager.Instance)
        {
            AudioManager.Instance.KillAllSounds();
        }
    }

    IEnumerator Routine_EndingSequence(GameObject playerObj)
    {
        _isEndingStarted = true;

        // 1. 카메라 전환 & BGM
        if (playerObj) playerObj.SetActive(false);
        if (roomWideCamera) roomWideCamera.Priority = 999;

        if (AudioManager.Instance && endingBgm)
        {
            AudioManager.Instance.PlayBgmWithFade(endingBgm, 1.0f);
        }

        yield return new WaitForSeconds(cameraTransitionDelay);

        // 2. SFX 재생
        if (AudioManager.Instance && endingSfx)
        {
            AudioManager.Instance.Play(endingSfx, AudioManager.Sound.Effect);
        }

        // 3. 이미지 연출 (1 -> 2 -> 3 -> 4(Flash) -> 3)
        // 안전 장치: 배열이 비어있으면 패스
        if (endingImages != null && endingImages.Length >= 4)
        {
            // [Image 1] Fade In
            endingImages[0].gameObject.SetActive(true);
            yield return endingImages[0].DOFade(1f, overlayFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(imageInterval);

            // [Image 2] Fade In (1번 위에 덮어씀)
            endingImages[1].gameObject.SetActive(true);
            yield return endingImages[1].DOFade(1f, overlayFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(imageInterval);

            // [Image 3] Fade In (2번 위에 덮어씀)
            endingImages[2].gameObject.SetActive(true);
            yield return endingImages[2].DOFade(1f, overlayFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(imageInterval);

            // [Image 4] Flash! (3번 위에 깜빡)
            endingImages[3].gameObject.SetActive(true);
            endingImages[3].alpha = 1f; // 페이드 없이 즉시 등장

            // 깜빡이는 찰나의 시간 대기
            yield return new WaitForSeconds(flashDuration);

            // 4번 끄기 -> 3번이 다시 보임
            endingImages[3].gameObject.SetActive(false);

            // 3번 이미지 감상 시간
            yield return new WaitForSeconds(finalImageHoldDuration);
        }
        else
        {
            Debug.LogWarning("[Ending] 이미지 배열이 4개 미만입니다! 인스펙터를 확인하세요.");
            yield return new WaitForSeconds(2.0f);
        }

        // 4. 화이트 커튼 (덮기)
        if (whiteCurtainGroup)
        {
            whiteCurtainGroup.gameObject.SetActive(true);
            yield return whiteCurtainGroup.DOFade(1f, whiteFadeDuration).WaitForCompletion();
        }

        // 뒤쪽 이미지들 모두 정리 (최적화)
        if (endingImages != null)
        {
            foreach (var img in endingImages) if (img) img.gameObject.SetActive(false);
        }

        // 5. Credits
        if (creditsGroup)
        {
            creditsGroup.gameObject.SetActive(true);
            yield return creditsGroup.DOFade(1f, textFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(textStayDuration);
            yield return creditsGroup.DOFade(0f, textFadeDuration).WaitForCompletion();
            creditsGroup.gameObject.SetActive(false);
        }

        // 6. Thank You
        if (thankYouGroup)
        {
            thankYouGroup.gameObject.SetActive(true);
            yield return thankYouGroup.DOFade(1f, textFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(textStayDuration);
            yield return thankYouGroup.DOFade(0f, textFadeDuration).WaitForCompletion();
            thankYouGroup.gameObject.SetActive(false);
        }

        // 7. Black Curtain (암전)
        if (blackCurtainGroup)
        {
            blackCurtainGroup.gameObject.SetActive(true);
            yield return blackCurtainGroup.DOFade(1f, finalBlackFadeDuration).WaitForCompletion();
        }

        // 8. 종료 및 리로드
        if (AudioManager.Instance) AudioManager.Instance.KillAllSounds();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}