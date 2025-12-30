using UnityEngine;

[RequireComponent(typeof(Draggable3D))]
[RequireComponent(typeof(Ladder))]
public class LadderPlacementController : MonoBehaviour
{
    public float maxSnapDistance = 1.2f;

    // ★ [추가] 이 높이보다 플레이어가 높이 있으면 드래그 금지
    [Tooltip("플레이어 Y좌표가 이 값보다 크면 사다리 드래그 불가 (가구 위라고 판단)")]
    public float disableDragHeight = 0.5f;

    private Draggable3D _drag;
    private Ladder _ladder;
    private Transform _playerTransform; // 플레이어 위치 확인용

    // 복귀용 저장값
    private Vector3 _prevPos;
    private Quaternion _prevRot;
    private LadderPlaceSpot _prevSpot;

    void Awake()
    {
        _drag = GetComponent<Draggable3D>();
        _ladder = GetComponent<Ladder>();

        _drag.OnDragStarted += HandleDragStarted;
        _drag.OnDragEnded += TrySnapOrRevert;
    }

    void Start()
    {
        // 씬에서 플레이어 찾기 (PlayerScriptedMover 또는 PlayerInteractor)
        var player = FindObjectOfType<PlayerScriptedMover>();
        if (player)
        {
            _playerTransform = player.transform;
            Debug.Log("Player detect!");
        }
    }

    // ★ [추가] 매 프레임 플레이어 높이 체크
    void Update()
    {
        if (!_playerTransform || !_drag) return;

        // 플레이어가 일정 높이(가구 위)에 있다면 드래그 컴포넌트를 꺼버림
        // (드래그만 안 되고, 클릭 상호작용은 다른 컴포넌트라 정상 작동함)
        bool isPlayerHigh = _playerTransform.position.y > disableDragHeight;

        // 상태가 다를 때만 변경 (최적화)
        if (_drag.enabled == isPlayerHigh)
        {
            _drag.enabled = !isPlayerHigh;
        }
    }

    void OnDestroy()
    {
        if (_drag)
        {
            _drag.OnDragStarted -= HandleDragStarted;
            _drag.OnDragEnded -= TrySnapOrRevert;
        }
    }

    void HandleDragStarted()
    {
        // 상태 저장
        _prevPos = transform.position;
        _prevRot = transform.rotation;
        _prevSpot = _ladder.currentSpot;

        // 떼어내기
        _ladder.Detach();
    }

    void TrySnapOrRevert()
    {
        var allSpots = FindObjectsOfType<LadderPlaceSpot>();
        LadderPlaceSpot bestSpot = null;
        float minDistSqr = float.PositiveInfinity;
        Vector3 currentPos = transform.position;

        foreach (var spot in allSpots)
        {
            if (!spot || !spot.ladderAnchor) continue;
            if (spot.occupied) continue;
            if (_ladder.lengthLevel < spot.requiredLengthLevel) continue;

            float dist = Vector3.Distance(currentPos, spot.ladderAnchor.position);
            if (maxSnapDistance > 0 && dist > maxSnapDistance) continue;

            float distSqr = (currentPos - spot.ladderAnchor.position).sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                bestSpot = spot;
            }
        }

        if (bestSpot != null)
        {
            _ladder.AttachTo(bestSpot, true);
        }
        else
        {
            // 복귀
            if (_prevSpot != null)
            {
                _ladder.AttachTo(_prevSpot, true);
            }
            else
            {
                transform.SetPositionAndRotation(_prevPos, _prevRot);
                _ladder.currentSpot = null;
            }
        }
    }
}