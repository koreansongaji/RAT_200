using UnityEngine;
using DG.Tweening;
using System.Collections; // Coroutine 사용을 위해 추가
using Unity.Cinemachine;  // Cinemachine 관련 네임스페이스 (필요시)

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

    private LadderPlacementController _placementController;
    private Vector3 _bottomPos;
    private bool _hasBottomPos = false;

    void Awake()
    {
        if (!_ladderSoundController) _ladderSoundController = GetComponent<LadderSoundController>();
        _placementController = GetComponent<LadderPlacementController>();
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (CloseupCamManager.InMicro) return false;
        if (climbDestination == null) return false;

        var mover = i.GetComponent<PlayerScriptedMover>();
        return mover && !mover.IsBusy();
    }

    public override void Interact(PlayerInteractor i)
    {
        var mover = i.GetComponent<PlayerScriptedMover>();
        if (!mover || !climbDestination) return;
        if (CloseupCamManager.InMicro) return;

        // 1. [잠금] 이동 시작 전 드래그 차단
        if (_placementController) _placementController.SetLadderBusy(true);

        bool isAtTop = i.transform.position.y >= yThreshold;

        // 2. [이동] Mover 호출 (4번째 인자는 원래대로 null 유지)
        if (!isAtTop)
        {
            // [올라가기]
            _bottomPos = i.transform.position;
            _hasBottomPos = true;

            _ladderSoundController?.PlayClimbLadder();

            // ★ 수정: 4번째 인자에 Action 대신 null (원래 시그니처 준수)
            mover.MoveToWorldWithCam(climbDestination.position, duration, ease, null, 0);
        }
        else
        {
            // [내려오기]
            Vector3 targetPos = _hasBottomPos ? _bottomPos : (i.transform.position - Vector3.up * 2f);

            _ladderSoundController?.PlayClimbLadder();

            // ★ 수정: 여기도 null
            mover.MoveToWorldWithCam(targetPos, duration, ease, null, 0);
        }

        // 3. [해제 예약] 이동 시간만큼 기다렸다가 잠금 해제
        StartCoroutine(Routine_ReleaseBusy(duration));
    }

    // 이동이 끝날 때쯤 Busy 상태를 풀어주는 코루틴
    IEnumerator Routine_ReleaseBusy(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_placementController)
            _placementController.SetLadderBusy(false);
    }
}