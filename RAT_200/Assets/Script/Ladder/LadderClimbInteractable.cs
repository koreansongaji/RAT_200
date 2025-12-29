using UnityEngine;
using DG.Tweening;

public class LadderClimbInteractable : BaseInteractable
{
    [Header("Target ����")]
    public ClimbTarget target;            // ���� ���� ����

    [Header("State / Threshold")]
    public float yThreshold = 1.0f;       // ��/�Ʒ� �Ǻ� ����
    Vector3 _preClimbPos;                 // ������ ��ġ ����
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
      
            

            // [�ö󰡱�]
            _preClimbPos = i.transform.position;
            _hasPreClimbPos = true;

            // 사운드: 오르기 시작 시 재생
            _ladderSoundController?.PlayClimbLadder();
            
            // �� ī�޶� ����(vcam)�� null�� �ֽ��ϴ�. (Ʈ���Ű� �˾Ƽ� �� ����)
            mover.MoveToWorldWithCam(
                target.climbPoint.position,
                duration,
                ease,
                null, // ī�޶� ����!
                0     // �켱���� 0!
            );
        }
        else
        {
            // �������� (��ϵ� ���� �ڸ���)
            Vector3 downPos = _hasPreClimbPos ? _preClimbPos : i.transform.position;

            // 사운드: 내릴 때 재생
            _ladderSoundController?.PlayClimbLadder();

            mover.MoveToWorldWithCam(
                downPos,
                duration,
                ease,
                null,
                0
            );

            // ���� �ִ� �ڷ�ƾ(ī�޶� ����) ������
        }
    }

    // OnDisable(ī�޶� ����) ������
}