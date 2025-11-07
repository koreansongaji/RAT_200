using UnityEngine;

[RequireComponent(typeof(Draggable3D))]
[RequireComponent(typeof(Ladder))]
public class LadderPlacementController : MonoBehaviour
{
    public float maxSnapDistance = 1.2f; // 후보 검색 최대 반경(옵션)

    Draggable3D drag;
    Ladder ladder;

    // 복귀용 저장값
    Vector3 _prevPos;
    Quaternion _prevRot;
    LadderPlaceSpot _prevSpot;

    void Awake()
    {
        drag = GetComponent<Draggable3D>();
        ladder = GetComponent<Ladder>();

        drag.OnDragStarted += HandleDragStarted;
        drag.OnDragEnded += TrySnapOrRevert;
    }

    void OnDestroy()
    {
        if (drag != null)
        {
            drag.OnDragStarted -= HandleDragStarted;
            drag.OnDragEnded -= TrySnapOrRevert;
        }
    }

    void HandleDragStarted()
    {
        // 현재 상태 저장
        _prevPos = transform.position;
        _prevRot = transform.rotation;
        _prevSpot = ladder.currentSpot;

        // 드래그로 이동 시작하니 현재 슬롯 점유 해제
        // (스냅 실패 시엔 다시 되돌릴 것)
        if (ladder.currentSpot != null)
            ladder.Detach();
    }

    void TrySnapOrRevert()
    {
        // 가장 가까운 유효 슬롯 찾기
        var all = FindObjectsOfType<LadderPlaceSpot>();
        LadderPlaceSpot best = null;
        float bestSqr = float.PositiveInfinity;

        Vector3 pos = transform.position;

        foreach (var s in all)
        {
            if (!s || !s.ladderAnchor) continue;
            if (s.occupied) continue;
            if (ladder.lengthLevel < s.requiredLengthLevel) continue;

            float d = Vector3.Distance(pos, s.ladderAnchor.position);
            if (maxSnapDistance > 0 && d > maxSnapDistance) continue;

            if (d <= s.snapRadius)
            {
                float d2 = (pos - s.ladderAnchor.position).sqrMagnitude;
                if (d2 < bestSqr) { bestSqr = d2; best = s; }
            }
        }

        if (best != null)
        {
            // 스냅 성공 → 새 슬롯에 부착
            ladder.AttachTo(best, true);
        }
        else
        {
            // 스냅 실패 → 이전 상태로 복귀
            if (_prevSpot != null)
            {
                // 예전 슬롯으로 되돌림(타겟/점유까지 복원)
                ladder.AttachTo(_prevSpot, true);
            }
            else
            {
                // 월드 자유 배치였다면 위치/회전만 되돌림
                transform.SetPositionAndRotation(_prevPos, _prevRot);
                ladder.currentSpot = null; // 확실히 슬롯 없음 표시
            }
        }
    }
}
