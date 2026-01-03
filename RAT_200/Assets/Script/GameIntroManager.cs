using UnityEngine;
using UnityEngine.AI;
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
    public Transform partnerRat;
    [Tooltip("들썩거리고 열릴 케이지 뚜껑")]
    public Transform cageLid;

    [Header("Lights")]
    public GameObject cageLight; // 조명 오브젝트 (Light 컴포넌트 포함 필수)

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

    // ★ [New] 조명 원래 밝기 저장용 변수
    private float _initialLightIntensity = 1.0f;
    private Light _targetLight;

    void Start()
    {
        if (cageLid)
        {
            _lidInitialLocalPos = cageLid.localPosition;
            _lidInitialLocalRot = cageLid.localRotation;
        }

        // ★ [New] 조명 컴포넌트 및 밝기 저장
        if (cageLight)
        {
            _targetLight = cageLight.GetComponent<Light>();
            if (_targetLight) _initialLightIntensity = _targetLight.intensity;
        }

        PrepareIntro();
    }

    void PrepareIntro()
    {
        if (cageIntroCam) cageIntroCam.Priority = 100;

        SetAgentState(playerRat, false);
        SetAgentState(partnerRat, false);

        if (cageInsidePos)
        {
            if (playerRat) playerRat.position = cageInsidePos.position;
            if (partnerRat) partnerRat.position = cageInsidePos.position + new Vector3(0.2f, 0, 0.2f);
        }

        // ★ [New] 게임 시작 시 조명 켜고 밝기 원상복구
        if (cageLight)
        {
            cageLight.SetActive(true);
            if (_targetLight) _targetLight.intensity = _initialLightIntensity;
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
        // 1. 뚜껑 열기
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
            playerRat.DOJump(playerLandPos.position, jumpPower, 1, jumpDuration)
                     .SetEase(Ease.OutQuad)
                     .OnComplete(() =>
                     {
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
                          SetAgentState(partnerRat, true);
                      });
        }

        // 연출이 끝날 때까지 대기
        yield return new WaitForSeconds(jumpDuration * 0.6f);

        // 카메라 전환
        if (cageIntroCam) cageIntroCam.Priority = 0;

        // ★ [New] 조명을 1초 동안 서서히 끄기 (Intensity -> 0)
        if (_targetLight)
        {
            _targetLight.DOIntensity(0f, 1.0f)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 완전히 어두워지면 비활성화 (성능 최적화)
                    if (cageLight) cageLight.SetActive(false);
                });
        }
        else if (cageLight)
        {
            // Light 컴포넌트 없으면 그냥 끔
            cageLight.SetActive(false);
        }

        Debug.Log("[Intro] 게임 시작 시퀀스 완료!");
    }

    void SetAgentState(Transform target, bool enabled)
    {
        if (!target) return;
        var agent = target.GetComponent<NavMeshAgent>();
        if (agent)
        {
            if (enabled)
            {
                agent.Warp(target.position);
                agent.enabled = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
            else
            {
                agent.enabled = false;
            }
        }
    }
}