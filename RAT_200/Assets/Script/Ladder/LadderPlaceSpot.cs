using UnityEngine;

public class LadderPlaceSpot : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("사다리가 스냅될 위치와 회전")]
    public Transform ladderAnchor;

    [Tooltip("사다리를 타고 올라갔을 때 도착할 위치 (빈 오브젝트)")]
    public Transform climbPoint;

    [Tooltip("이 자리에 설치하기 위해 필요한 사다리 레벨")]
    public int requiredLengthLevel = 1;

    [Tooltip("스냅 감지 반경")]
    public float snapRadius = 0.6f;

    // 현재 이 자리에 사다리가 있는지 여부
    [HideInInspector] public bool occupied;

    void OnDrawGizmos()
    {
        if (ladderAnchor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ladderAnchor.position, snapRadius);
            Gizmos.DrawLine(transform.position, ladderAnchor.position);
        }
        if (climbPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(climbPoint.position, 0.2f);
        }
    }
}