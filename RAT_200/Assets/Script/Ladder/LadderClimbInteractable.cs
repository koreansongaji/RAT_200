using UnityEngine;
using DG.Tweening;

public class LadderClimbInteractable : BaseInteractable
{
    [Header("Settings")]
    public float yThreshold = 1.0f;       // 높이 기준 (위/아래 판정)
    public float duration = 0.6f;
    public Ease ease = Ease.InOutSine;

    // Ladder.cs에서 AttachTo 할 때 여기다 목적지를 넣어줌
    [HideInInspector] public Transform climbDestination;

    [Header("Sound")]
    [SerializeField] private LadderSoundController _ladderSoundController;

    private Vector3 _bottomPos; // 올라가기 전 바닥 위치 기억
    private bool _hasBottomPos = false;

    void Awake()
    {
        if (!_ladderSoundController) _ladderSoundController = GetComponent<LadderSoundController>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        // ★ [수정] 마이크로 줌 상태라면 사다리 타기 상호작용 불가!
        if (CloseupCamManager.InMicro) return false;

        // 설치가 안 되어 있으면(목적지가 없으면) 상호작용 불가
        if (climbDestination == null) return false;

        var mover = i.GetComponent<PlayerScriptedMover>();
        return mover && !mover.IsBusy();
    }

    public override void Interact(PlayerInteractor i)
    {
        var mover = i.GetComponent<PlayerScriptedMover>();
        if (!mover || !climbDestination) return;

        // ★ [수정] 혹시 모르니 실행 시점에도 한 번 더 차단
        if (CloseupCamManager.InMicro) return;

        bool isAtTop = i.transform.position.y >= yThreshold;

        if (!isAtTop)
        {
            // [올라가기]
            _bottomPos = i.transform.position;
            _hasBottomPos = true;

            _ladderSoundController?.PlayClimbLadder();
            mover.MoveToWorldWithCam(climbDestination.position, duration, ease, null, 0);
        }
        else
        {
            // [내려오기]
            Vector3 targetPos = _hasBottomPos ? _bottomPos : (i.transform.position - Vector3.up * 2f);

            _ladderSoundController?.PlayClimbLadder();
            mover.MoveToWorldWithCam(targetPos, duration, ease, null, 0);
        }
    }
}