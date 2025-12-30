using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;

public class RopeInteractable : BaseInteractable
{
    [Header("��ǥ ����")]
    public Transform ropeAxis;
    public Transform topPoint;
    public Transform bottomPoint;

    [Header("����")]
    public float duration = 2.0f;
    public Ease ease = Ease.InOutQuad;

    [Header("ī�޶� (����)")]
    public CinemachineCamera ropeCamera;

    [Header("���� ���� (Scale X)")]
    public float targetScaleX = -1;
    [SerializeField] private RopeSoundController _ropeSoundController;

    void Awake()
    {
        if (!ropeAxis) ropeAxis = transform;
        if(_ropeSoundController == null) _ropeSoundController = GetComponent<RopeSoundController>();
        if(_ropeSoundController == null) _ropeSoundController = gameObject.AddComponent<RopeSoundController>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        var mover = i.GetComponent<PlayerScriptedMover>();
        return mover && !mover.IsBusy();
    }

    public override void Interact(PlayerInteractor i)
    {
        Debug.Log("Rope Interaction Start!");

        var mover = i.GetComponent<PlayerScriptedMover>();
        if (!mover || !topPoint || !bottomPoint) return;

        _ropeSoundController.PlayRopeSound();

        // 1. ������ ���� (�� �� ������)
        float distToTop = Vector3.Distance(i.transform.position, topPoint.position);
        float distToBottom = Vector3.Distance(i.transform.position, bottomPoint.position);
        bool goingUp = distToTop > distToBottom;
        Transform targetLandPoint = goingUp ? topPoint : bottomPoint;

        // 2. ��� ���
        Vector3 playerPos = i.transform.position;
        Vector3 axisPos = ropeAxis.position;

        Vector3 alignPoint = new Vector3(axisPos.x, playerPos.y, axisPos.z);
        Vector3 climbPoint = new Vector3(axisPos.x, targetLandPoint.position.y, axisPos.z);
        Vector3 landPoint = targetLandPoint.position;

        Vector3[] path = new Vector3[] { alignPoint, climbPoint, landPoint };

        // 3. �̵� ����
        int camPriority = (ropeCamera != null) ? 100 : 0;

        mover.MovePathWithCam(
            path,
            duration,
            ease,
            ropeCamera,
            camPriority,
            useRopeAnim: true,
            overrideScaleX: targetScaleX  // �� ���� ��ǥ�� ����
        );
    }
}