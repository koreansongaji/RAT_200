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

    // 1. 플레이어 높이에 따른 드래그 잠금 기능 (유지)
    void Update()
    {
        if (!_playerTransform || !_drag) return;

        bool isPlayerHigh = _playerTransform.position.y > disableDragHeight;

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
        _prevPos = transform.position;
        _prevRot = transform.rotation;
        _prevSpot = _ladder.currentSpot;

        _ladder.Detach();

        // ★ [복구] 드래그 시작 시 가능한 자리에 하이라이트 켜기
        UpdateSpotHighlights(true);
    }

    void TrySnapOrRevert()
    {
        // ★ [복구] 드래그 종료 시 하이라이트 끄기
        UpdateSpotHighlights(false);

        var allSpots = FindObjectsOfType<LadderPlaceSpot>();
        LadderPlaceSpot bestSpot = null;
        float minDistSqr = float.PositiveInfinity;
        Vector3 currentPos = transform.position;

        foreach (var spot in allSpots)
        {
            if (!spot || !spot.ladderAnchor) continue;
            if (spot.occupied) continue;

            // ★ [복구] 잠겨 있으면(퓨즈 퍼즐 등) 설치 불가
            if (spot.isLocked) continue;

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

    // ★ [복구] 하이라이트(스포트라이트) 갱신 함수
    void UpdateSpotHighlights(bool show)
    {
        var allSpots = FindObjectsOfType<LadderPlaceSpot>();

        foreach (var spot in allSpots)
        {
            if (!spot || !spot.validIndicator) continue;

            if (show)
            {
                // 1. 레벨 충족 여부
                bool levelOk = _ladder.lengthLevel >= spot.requiredLengthLevel;
                // 2. 빈 자리 여부
                bool isEmpty = !spot.occupied;
                // 3. 잠금 해제 여부
                bool notLocked = !spot.isLocked;

                // 모든 조건 만족 시 켜기
                spot.validIndicator.SetActive(levelOk && isEmpty && notLocked);
            }
            else
            {
                spot.validIndicator.SetActive(false);
            }
        }
    }
}