using UnityEngine;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;

public class LadderClimbInteractable : BaseInteractable
{
    [Header("Target 매핑")]
    public ClimbTarget target;            // 이 사다리가 연결된 오브젝트

    [Header("State / Threshold")]
    public float yThreshold = 1.0f;
    Vector3 _preClimbPos;                 // 오르기 직전 위치 저장
    bool _hasPreClimbPos;

    [Header("Tween")]
    public float duration = 0.6f;
    public Ease ease = Ease.InOutSine;

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
            // 올라가기
            _preClimbPos = i.transform.position;
            _hasPreClimbPos = true;

            // 클로즈업 활성화
            if (target.closeVCam)
                CloseupCamManager.Activate(target.closeVCam);

            mover.MoveToWorldWithCam(
                target.climbPoint.position, duration, ease,
                target.closeVCam, CloseupCamManager.CloseOn, true
            );
        }
        else
        {
            // 내려오기 (기록된 원래 자리로)
            Vector3 downPos = _hasPreClimbPos ? _preClimbPos : i.transform.position;

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
