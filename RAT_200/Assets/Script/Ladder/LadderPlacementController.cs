using UnityEngine;

[RequireComponent(typeof(Draggable3D))]
[RequireComponent(typeof(Ladder))]
public class LadderPlacementController : MonoBehaviour
{
    public float maxSnapDistance = 1.2f;

    [Tooltip("플레이어 Y좌표가 이 값보다 크면 사다리 드래그 불가 (가구 위라고 판단)")]
    public float disableDragHeight = 0.5f;

    private Draggable3D _drag;
    private Ladder _ladder;
    private Transform _playerTransform;

    // 복귀용 저장값
    private Vector3 _prevPos;
    private Quaternion _prevRot;
    private LadderPlaceSpot _prevSpot;

    // ★ [추가] 사다리 오르내리기 중인지 체크하는 플래그
    private bool _isBusy = false;

    void Awake()
    {
        _drag = GetComponent<Draggable3D>();
        _ladder = GetComponent<Ladder>();

        _drag.OnDragStarted += HandleDragStarted;
        _drag.OnDragEnded += TrySnapOrRevert;
    }

    void Start()
    {
        var player = FindObjectOfType<PlayerScriptedMover>();
        if (player) _playerTransform = player.transform;
    }

    // ★ [추가] 외부(Interactable)에서 호출할 상태 변경 함수
    public void SetLadderBusy(bool busy)
    {
        _isBusy = busy;

        // 사용 중이면 즉시 드래그 끄기 (업데이트 기다리지 않고 즉각 반응)
        if (_isBusy && _drag.enabled)
        {
            _drag.enabled = false;
        }
    }

    void Update()
    {
        if (!_playerTransform || !_drag) return;

        // 1. 플레이어가 가구 위에 있는지?
        bool isPlayerHigh = _playerTransform.position.y > disableDragHeight;

        // 2. ★ [수정] "높이 올라감" OR "지금 사다리 타는 중(_isBusy)" 이면 드래그 금지
        bool shouldDisable = isPlayerHigh || _isBusy;

        // 상태가 다를 때만 변경 (최적화)
        if (_drag.enabled == shouldDisable)
        {
            _drag.enabled = !shouldDisable;
        }
    }

    // ... (이하 OnDestroy, HandleDragStarted, TrySnapOrRevert, UpdateSpotHighlights 등 기존 코드 유지) ...
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
        _prevPos = transform.position;
        _prevRot = transform.rotation;
        _prevSpot = _ladder.currentSpot;
        _ladder.Detach();
        UpdateSpotHighlights(true);
    }

    void TrySnapOrRevert()
    {
        UpdateSpotHighlights(false);
        var allSpots = FindObjectsOfType<LadderPlaceSpot>();
        LadderPlaceSpot bestSpot = null;
        float minDistSqr = float.PositiveInfinity;
        Vector3 currentPos = transform.position;

        foreach (var spot in allSpots)
        {
            if (!spot || !spot.ladderAnchor) continue;
            if (spot.occupied) continue;
            if (spot.isLocked) continue;
            if (_ladder.lengthLevel < spot.requiredLengthLevel) continue;

            float dist = Vector3.Distance(currentPos, spot.ladderAnchor.position);
            if (maxSnapDistance > 0 && dist > maxSnapDistance) continue;

            float distSqr = (currentPos - spot.ladderAnchor.position).sqrMagnitude;
            if (distSqr < minDistSqr) { minDistSqr = distSqr; bestSpot = spot; }
        }

        if (bestSpot != null)
        {
            _ladder.AttachTo(bestSpot, true);
        }
        else
        {
            if (_prevSpot != null) _ladder.AttachTo(_prevSpot, true);
            else { transform.SetPositionAndRotation(_prevPos, _prevRot); _ladder.currentSpot = null; }
        }
    }

    void UpdateSpotHighlights(bool show)
    {
        var allSpots = FindObjectsOfType<LadderPlaceSpot>();
        foreach (var spot in allSpots)
        {
            if (!spot || !spot.validIndicator) continue;
            if (show)
            {
                bool levelOk = _ladder.lengthLevel >= spot.requiredLengthLevel;
                bool isEmpty = !spot.occupied;
                bool notLocked = !spot.isLocked;
                spot.validIndicator.SetActive(levelOk && isEmpty && notLocked);
            }
            else spot.validIndicator.SetActive(false);
        }
    }
}