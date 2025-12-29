using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;

public class LadderClimbInteractable : BaseInteractable
{
    [Header("Target ����")]
    public ClimbTarget target;            // �� ��ٸ��� ����� ������Ʈ

    [Header("State / Threshold")]
    public float yThreshold = 1.0f;
    Vector3 _preClimbPos;                 // ������ ���� ��ġ ����
    bool _hasPreClimbPos;

    [Header("Tween")]
    public float duration = 0.6f;
    public Ease ease = Ease.InOutSine;

    // Ladder sound controller (optional)
    [SerializeField] private LadderSoundController _ladderSoundController;

    void Awake()
    {
        if (_ladderSoundController == null) _ladderSoundController = GetComponent<LadderSoundController>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        var mover = i.GetComponent<PlayerScriptedMover>();
        return mover && !mover.IsBusy();
    }

    public override void Interact(PlayerInteractor i)
    {
        var mover = i.GetComponent<PlayerScriptedMover>();
        if (!mover || target == null || target.climbPoint == null) return;

        bool isAbove = i.transform.position.y >= yThreshold;

        if (!isAbove)
        {
            // �ö󰡱�
            _preClimbPos = i.transform.position;
            _hasPreClimbPos = true;

            // Ŭ����� Ȱ��ȭ
            if (target.closeVCam)
                CloseupCamManager.Activate(target.closeVCam);

            // 사운드: 오르기 시작 시 재생
            _ladderSoundController?.PlayClimbLadder();

            mover.MoveToWorldWithCam(
                target.climbPoint.position, duration, ease,
                target.closeVCam, CloseupCamManager.CloseOn, true
            );
        }
        else
        {
            // �������� (��ϵ� ���� �ڸ���)
            Vector3 downPos = _hasPreClimbPos ? _preClimbPos : i.transform.position;

            // 사운드: 내릴 때 재생
            _ladderSoundController?.PlayClimbLadder();

            mover.MoveToWorldWithCam(
                downPos, duration, ease,
                target.closeVCam, CloseupCamManager.CloseOff, true
            );
            i.StartCoroutine(ReleaseCamAfter(duration * 1.02f));
        }
    }

    IEnumerator ReleaseCamAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (target && target.closeVCam)
            CloseupCamManager.Deactivate(target.closeVCam);
    }

    void OnDisable()
    {
        if (target && target.closeVCam)
            CloseupCamManager.Deactivate(target.closeVCam);
    }
}
