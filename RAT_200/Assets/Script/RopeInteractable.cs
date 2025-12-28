using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;

public class RopeInteractable : BaseInteractable
{
    [Header("좌표 설정")]
    [Tooltip("밧줄의 수직 중심축(X, Z 좌표 기준). 비워두면 이 오브젝트 사용.")]
    public Transform ropeAxis;

    public Transform topPoint;    // 꼭대기 착지 지점
    public Transform bottomPoint; // 바닥 착지 지점

    [Header("설정")]
    public float duration = 2.0f; // 이동 시간
    public Ease ease = Ease.InOutQuad;

    [Header("카메라 (선택)")]
    public CinemachineCamera ropeCamera;

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

        // 1. 목적지 결정 (더 먼 쪽으로)
        float distToTop = Vector3.Distance(i.transform.position, topPoint.position);
        float distToBottom = Vector3.Distance(i.transform.position, bottomPoint.position);

        bool goingUp = distToTop > distToBottom;
        Transform targetLandPoint = goingUp ? topPoint : bottomPoint;

        // 2. 경로 웨이포인트(Waypoints) 계산 [중요!]
        // Path: [밀착 지점] -> [수직 이동 지점] -> [착지 지점]

        Vector3 playerPos = i.transform.position;
        Vector3 axisPos = ropeAxis.position; // 밧줄의 X, Z

        // W1: 현재 높이에서 밧줄 X, Z로 밀착
        Vector3 alignPoint = new Vector3(axisPos.x, playerPos.y, axisPos.z);

        // W2: 밧줄 X, Z를 유지한 채로 목표 높이까지 등반
        Vector3 climbPoint = new Vector3(axisPos.x, targetLandPoint.position.y, axisPos.z);

        // W3: 착지 지점 (Landing)
        Vector3 landPoint = targetLandPoint.position;

        Vector3[] path = new Vector3[] { alignPoint, climbPoint, landPoint };

        // 3. 이동 명령
        int camPriority = (ropeCamera != null) ? 100 : 0; // 혹은 CloseupCamManager.CloseOn

        mover.MovePathWithCam(
            path,
            duration,
            ease,
            ropeCamera,
            camPriority
        );
    }
}