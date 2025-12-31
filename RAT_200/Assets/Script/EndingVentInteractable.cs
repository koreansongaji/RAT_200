using UnityEngine;
using System.Collections;
using Unity.Cinemachine; // 시네머신 사용
using UnityEngine.UI;
using DG.Tweening; // DOTween 사용

public class EndingVentInteractable : BaseInteractable
{
    [Header("Ending Settings")]
    [Tooltip("엔딩 시 활성화될 방 전체를 비추는 와이드 카메라")]
    public CinemachineCamera roomWideCamera;

    [Tooltip("페이드 인 될 컬러 이미지를 가진 캔버스 그룹")]
    public CanvasGroup overlayCanvasGroup;

    [Tooltip("이미지가 페이드 인 되는 데 걸리는 시간")]
    public float fadeDuration = 3.0f;

    private bool _isEndingStarted = false;

    void Start()
    {
        // 초기화: 오버레이 이미지는 안 보이고 투명하게 시작
        if (overlayCanvasGroup)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.gameObject.SetActive(false);
            // 클릭 방지를 위해 Raycast Target도 꺼두는 것이 좋습니다.
            overlayCanvasGroup.blocksRaycasts = false;
        }

        // 엔딩 카메라는 평소엔 우선순위를 낮게 설정
        if (roomWideCamera)
        {
            roomWideCamera.Priority = 10;
        }
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        // 엔딩이 시작되지 않았을 때만 상호작용 가능
        return !_isEndingStarted;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (_isEndingStarted) return;

        Debug.Log("=== Game Ending Sequence Started ===");
        StartCoroutine(Routine_EndingSequence(i.gameObject));
    }

    IEnumerator Routine_EndingSequence(GameObject playerObj)
    {
        _isEndingStarted = true;

        // 1. 플레이어 쥐 사라짐 (비활성화)
        if (playerObj)
        {
            playerObj.SetActive(false);
        }

        // 2. 1초 지연
        yield return new WaitForSeconds(1.0f);

        // 3. 카메라 전환 (방 전체 뷰)
        // 기존 카메라들의 Priority가 보통 10~20 내외일 테니, 확실하게 높게 설정하여 전환합니다.
        if (roomWideCamera)
        {
            roomWideCamera.Priority = 999;
        }

        // 4. 오버레이 이미지 페이드 인
        if (overlayCanvasGroup)
        {
            overlayCanvasGroup.gameObject.SetActive(true);
            // DOTween을 사용하여 alpha 값을 0에서 1로 부드럽게 변경
            overlayCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InOutQuad);
        }

        // (옵션) 페이드 인이 끝난 후 완전히 종료하거나 크레딧 씬으로 넘어가려면 여기에 추가 로직 작성
        // yield return new WaitForSeconds(fadeDuration);
        // Debug.Log("Ending Sequence Complete.");
    }
}