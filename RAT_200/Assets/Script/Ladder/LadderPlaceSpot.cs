using UnityEngine;

public class LadderPlaceSpot : MonoBehaviour
{
    [Header("사다리 앵커 (이 위치/회전에 스냅)")]
    public Transform ladderAnchor;

    [Header("오를 대상")]
    public ClimbTarget climbTarget;   // 이 가구의 올라갈 포인트(+ 클로즈업 카메라)

    [Header("요구 길이 레벨 (1=짧음, 2=중간, 3=김 등)")]
    public int requiredLengthLevel = 1;

    [Header("슬롯 반경 (이 반경 내로 드롭해야 스냅)")]
    public float snapRadius = 0.6f;

    [HideInInspector] public bool occupied;

    void OnDrawGizmos()
    {
        if (ladderAnchor)
        {
            Gizmos.DrawWireSphere(ladderAnchor.position, snapRadius);
            Gizmos.DrawLine(transform.position, ladderAnchor.position);
        }
    }
}
