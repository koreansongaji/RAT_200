using UnityEngine;
using DG.Tweening;

public class LadderClimbInteractable : BaseInteractable
{
    [Header("Target 매핑")]
    public ClimbTarget target;            // 도착 지점 정보

    [Header("State / Threshold")]
    public float yThreshold = 1.0f;       // 위/아래 판별 높이
    Vector3 _preClimbPos;                 // 내려올 위치 기억용
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
            // [올라가기]
            _preClimbPos = i.transform.position;
            _hasPreClimbPos = true;

            // ★ 카메라 인자(vcam)에 null을 넣습니다. (트리거가 알아서 할 것임)
            mover.MoveToWorldWithCam(
                target.climbPoint.position,
                duration,
                ease,
                null, // 카메라 없음!
                0     // 우선순위 0!
            );
        }
        else
        {
            // [내려오기]
            Vector3 downPos = _hasPreClimbPos ? _preClimbPos : i.transform.position;

            // ★ 여기도 카메라는 null입니다.
            mover.MoveToWorldWithCam(
                downPos,
                duration,
                ease,
                null,
                0
            );

            // 원래 있던 코루틴(카메라 끄기) 삭제됨
        }
    }

    // OnDisable(카메라 끄기) 삭제됨
}