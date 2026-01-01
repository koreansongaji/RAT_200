using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement; // 씬 리로드를 위해 필요

public class EndingVentInteractable : BaseInteractable
{
    [Header("Ending Settings")]
    [Tooltip("엔딩 시 활성화될 방 전체를 비추는 와이드 카메라")]
    public CinemachineCamera roomWideCamera;

    [Header("UI References (Canvas Groups)")]
    [Tooltip("방 전체 컬러 이미지 (기존)")]
    public CanvasGroup overlayCanvasGroup;

    [Tooltip("'Credits' 텍스트가 포함된 UI 그룹")]
    public CanvasGroup creditsGroup;

    [Tooltip("'Thank you for playing' 텍스트가 포함된 UI 그룹")]
    public CanvasGroup thankYouGroup;

    [Tooltip("마지막에 화면을 덮을 검은색 패널")]
    public CanvasGroup blackCurtainGroup;

    [Header("Timing Settings")]
    public float overlayFadeDuration = 3.0f; // 컬러 이미지 페이드 시간
    public float textFadeDuration = 1.0f;    // 텍스트 나타남/사라짐 시간
    public float textStayDuration = 2.5f;    // 텍스트 머무르는 시간
    public float finalBlackFadeDuration = 2.0f; // 마지막 암전 시간

    private bool _isEndingStarted = false;

    void Start()
    {
        // 1. 모든 UI 요소 초기화 (안 보이게)
        InitCanvasGroup(overlayCanvasGroup);
        InitCanvasGroup(creditsGroup);
        InitCanvasGroup(thankYouGroup);

        // 블랙 커튼은 시작할 때 꺼져 있어야 함 (만약 씬 시작 페이드인이 필요하면 별도 처리)
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
        StartCoroutine(Routine_EndingSequence(i.gameObject));
    }

    IEnumerator Routine_EndingSequence(GameObject playerObj)
    {
        _isEndingStarted = true;

        // 1. 플레이어 퇴장
        if (playerObj) playerObj.SetActive(false);
        yield return new WaitForSeconds(1.0f);

        // 2. 카메라 전환 (방 전체)
        if (roomWideCamera) roomWideCamera.Priority = 999;

        // 3. 컬러 이미지 페이드 인
        if (overlayCanvasGroup)
        {
            overlayCanvasGroup.gameObject.SetActive(true);
            // 이미지가 다 나올 때까지 대기
            yield return overlayCanvasGroup.DOFade(1f, overlayFadeDuration).WaitForCompletion();
        }

        // 4. Credits 등장 -> 대기 -> 퇴장
        if (creditsGroup)
        {
            creditsGroup.gameObject.SetActive(true);
            yield return creditsGroup.DOFade(1f, textFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(textStayDuration);
            yield return creditsGroup.DOFade(0f, textFadeDuration).WaitForCompletion();
            creditsGroup.gameObject.SetActive(false);
        }

        // 5. Thank You 등장 -> 대기 -> 퇴장
        if (thankYouGroup)
        {
            thankYouGroup.gameObject.SetActive(true);
            yield return thankYouGroup.DOFade(1f, textFadeDuration).WaitForCompletion();
            yield return new WaitForSeconds(textStayDuration);
            yield return thankYouGroup.DOFade(0f, textFadeDuration).WaitForCompletion();
            thankYouGroup.gameObject.SetActive(false);
        }

        // 6. Black Curtain 페이드 인 (암전)
        if (blackCurtainGroup)
        {
            blackCurtainGroup.gameObject.SetActive(true);
            yield return blackCurtainGroup.DOFade(1f, finalBlackFadeDuration).WaitForCompletion();
        }

        // 7. 암전 상태에서 컬러 이미지 끄기 (다음에 보이지 않게)
        if (overlayCanvasGroup) overlayCanvasGroup.gameObject.SetActive(false);

        // 8. 씬 리로드 (타이틀 화면으로 복귀)
        // 주의: 리로드 후 Black Curtain이 서서히 걷히는 연출은 
        // 씬 시작 스크립트(SceneFader 등)가 담당해야 자연스럽습니다.
        // 현재는 씬을 다시 로드하여 초기 상태로 돌립니다.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}