using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 제어를 위해 필요
using DG.Tweening;
using Unity.Cinemachine;
using System.Collections;

public class GameIntroManager : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("케이지를 비추는 오프닝용 카메라")]
    public CinemachineCamera cageIntroCam;

    [Header("Actors")]
    public Transform playerRat;
    public Transform partnerRat; // 동료 쥐
    [Tooltip("들썩거리고 열릴 케이지 뚜껑")]
    public Transform cageLid;

    [Header("Start Positions")]
    public Transform cageInsidePos;

    [Header("Landing Positions")]
    public Transform playerLandPos;
    public Transform partnerLandPos;

    [Header("Lid Settings")]
    public float lidSlideX = 0.5f;
    public float lidSlideDuration = 0.2f;
    public Vector3 lidOpenRotation = new Vector3(-110, 0, 0);
    public float lidRotateDuration = 0.4f;

    [Header("Jump Settings")]
    public float jumpPower = 1.5f;
    public float jumpDuration = 0.8f;

    [Header("Shake Settings")]
    public float shakeStrength = 5.0f;

    private Tween _shakeTween;
    private bool _isGameStarted = false;
    private Vector3 _lidInitialLocalPos;
    private Quaternion _lidInitialLocalRot;

    void Start()
    {
        if (cageLid)
        {
            _lidInitialLocalPos = cageLid.localPosition;
            _lidInitialLocalRot = cageLid.localRotation;
        }
        PrepareIntro();
    }

    void PrepareIntro()
    {
        if (cageIntroCam) cageIntroCam.Priority = 100;

        // ★ [핵심] 위치 잡기 전에 Agent 끄기 (충돌/스냅 방지)
        SetAgentState(playerRat, false);
        SetAgentState(partnerRat, false);

        if (cageInsidePos)
        {
            if (playerRat) playerRat.position = cageInsidePos.position;
            if (partnerRat) partnerRat.position = cageInsidePos.position + new Vector3(0.2f, 0, 0.2f);
        }

        StartLidShaking();
    }

    void StartLidShaking()
    {
        if (!cageLid) return;
        _shakeTween?.Kill();
        _shakeTween = cageLid.DOShakeRotation(1.0f, new Vector3(shakeStrength, 0, 0), 10, 90, true)
                             .SetLoops(-1, LoopType.Restart)
                             .SetEase(Ease.Linear);
    }

    public void OnGameStartTrigger()
    {
        if (_isGameStarted) return;
        _isGameStarted = true;

        StartCoroutine(Routine_OpenAndJump());
    }

    IEnumerator Routine_OpenAndJump()
    {
        // 1. 뚜껑 열기 연출
        _shakeTween?.Kill();
        if (cageLid)
        {
            cageLid.localPosition = _lidInitialLocalPos;
            cageLid.localRotation = _lidInitialLocalRot;

            CommonSoundController.Instance?.PlayDoorOpen();

            Sequence lidSeq = DOTween.Sequence();
            lidSeq.Append(cageLid.DOLocalMoveX(_lidInitialLocalPos.x + lidSlideX, lidSlideDuration).SetEase(Ease.OutQuad));
            lidSeq.Append(cageLid.DOLocalRotate(lidOpenRotation, lidRotateDuration).SetEase(Ease.OutBack));
        }

        yield return new WaitForSeconds(lidSlideDuration * 0.8f);

        // 2. 플레이어 점프
        if (playerRat && playerLandPos)
        {
            // ★ 점프 중에도 Agent는 꺼져 있어야 함 (PrepareIntro에서 이미 껐음)
            playerRat.DOJump(playerLandPos.position, jumpPower, 1, jumpDuration)
                     .SetEase(Ease.OutQuad)
                     .OnComplete(() =>
                     {
                         // ★ [핵심] 착지 후 Agent 다시 켜기 & 위치 동기화
                         SetAgentState(playerRat, true);
                     });
        }

        // 3. 동료 점프
        if (partnerRat && partnerLandPos)
        {
            partnerRat.DOJump(partnerLandPos.position, jumpPower, 1, jumpDuration)
                      .SetEase(Ease.OutQuad)
                      .OnComplete(() =>
                      {
                          // ★ 착지 후 Agent 다시 켜기
                          SetAgentState(partnerRat, true);
                      });
        }

        yield return new WaitForSeconds(jumpDuration * 0.6f);

        if (cageIntroCam) cageIntroCam.Priority = 0;

        Debug.Log("[Intro] 게임 시작 시퀀스 완료!");
    }

    // NavMeshAgent 끄고 켜는 헬퍼 함수
    void SetAgentState(Transform target, bool enabled)
    {
        if (!target) return;
        var agent = target.GetComponent<NavMeshAgent>();
        if (agent)
        {
            if (enabled)
            {
                // 켤 때: 위치를 강제로 맞추고 경로 초기화
                agent.Warp(target.position);
                agent.enabled = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
            else
            {
                // 껄 때
                agent.enabled = false;
            }
        }
    }
}