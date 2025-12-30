using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;

public class RopeInteractable : BaseInteractable
{
    [Header("좌표 설정")]
    public Transform ropeAxis;
    public Transform topPoint;
    public Transform bottomPoint;

    [Header("설정")]
    public float duration = 2.0f;
    public Ease ease = Ease.InOutQuad;

    [Header("카메라 (선택)")]
    public CinemachineCamera ropeCamera;

    [Header("방향 설정 (Scale X)")]
    [Tooltip("캐릭터가 오른쪽을 볼 때의 Scale X 값 (보통 1)")]
    public float lookRightScale = 1f;
    [Tooltip("캐릭터가 왼쪽을 볼 때의 Scale X 값 (보통 -1)")]
    public float lookLeftScale = -1f;

    void Awake()
    {
        if (!ropeAxis) ropeAxis = transform;
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        var mover = i.GetComponent<PlayerScriptedMover>();
        return mover && !mover.IsBusy();
    }

    public override void Interact(PlayerInteractor i)
    {
        var mover = i.GetComponent<PlayerScriptedMover>();
        if (!mover || !topPoint || !bottomPoint) return;

        // 1. 목적지 결정
        float distToTop = Vector3.Distance(i.transform.position, topPoint.position);
        float distToBottom = Vector3.Distance(i.transform.position, bottomPoint.position);
        bool goingUp = distToTop > distToBottom;
        Transform targetLandPoint = goingUp ? topPoint : bottomPoint;

        // 2. 경로 계산
        Vector3 playerPos = i.transform.position;
        Vector3 axisPos = ropeAxis.position;

        Vector3 alignPoint = new Vector3(axisPos.x, playerPos.y, axisPos.z);
        Vector3 climbPoint = new Vector3(axisPos.x, targetLandPoint.position.y, axisPos.z);
        Vector3 landPoint = targetLandPoint.position;

        Vector3[] path = new Vector3[] { alignPoint, climbPoint, landPoint };

        // ★ [수정] 방향 결정 로직
        // 플레이어가 밧줄보다 오른쪽(x가 큼)에 있다면 -> 왼쪽을 봐야 함
        // 플레이어가 밧줄보다 왼쪽(x가 작음)에 있다면 -> 오른쪽을 봐야 함
        float targetScaleX = (playerPos.x > axisPos.x) ? lookLeftScale : lookRightScale;

        // 3. 이동 명령
        int camPriority = (ropeCamera != null) ? 100 : 0;

        mover.MovePathWithCam(
            path,
            duration,
            ease,
            ropeCamera,
            camPriority,
            useRopeAnim: true,
            overrideScaleX: targetScaleX  // ★ 계산된 목표값 전달
        );
    }
}