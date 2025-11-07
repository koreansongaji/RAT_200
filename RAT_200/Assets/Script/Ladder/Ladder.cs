using UnityEngine;

[RequireComponent(typeof(LadderPlacementController))]
public class Ladder : MonoBehaviour
{
    [Range(1, 5)] public int lengthLevel = 1; // 업그레이드로 증가
    public LadderPlaceSpot currentSpot;       // 현재 붙어있는 슬롯 (없으면 들고 다니는 상태)

    public void AttachTo(LadderPlaceSpot spot, bool alignRotation = true)
    {
        if (!spot || !spot.ladderAnchor) return;

        // 기존 슬롯 비우기
        if (currentSpot) currentSpot.occupied = false;

        // 스냅 & 점유
        transform.position = spot.ladderAnchor.position;
        if (alignRotation) transform.rotation = spot.ladderAnchor.rotation;
        currentSpot = spot;
        spot.occupied = true;

        // 오르기 타겟 연결
        var climb = GetComponent<LadderClimbInteractable>();
        if (climb && spot.climbTarget) climb.target = spot.climbTarget;
    }

    public void Detach()
    {
        if (currentSpot) currentSpot.occupied = false;
        currentSpot = null;

        // 오르기 비활성화(선택)
        var climb = GetComponent<LadderClimbInteractable>();
        if (climb) climb.target = null;
    }
}
