using UnityEngine;

[RequireComponent(typeof(Draggable3D))]
[RequireComponent(typeof(Ladder))]
public class LadderPlacementController : MonoBehaviour
{
    public float maxSnapDistance = 1.2f;

    private Draggable3D _drag;
    private Ladder _ladder;

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