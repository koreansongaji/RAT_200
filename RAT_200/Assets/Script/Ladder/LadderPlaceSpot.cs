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

    [Header("잠금 설정 (퓨즈 퍼즐 연동)")]
    [Tooltip("true면 사다리를 놓을 수 없음 (퓨즈 퍼즐 등으로 풀어야 함)")]
    public bool isLocked = false;

    [Header("Visuals")]
    [Tooltip("드래그 중, 설치 조건이 맞으면 켜질 오브젝트 (Spotlight 등)")]
    public GameObject validIndicator;

    [HideInInspector] public bool occupied;

    void Start()
    {
        if (validIndicator) validIndicator.SetActive(false);
    }

    // 외부(퍼즐)에서 호출해서 잠금 해제
    public void UnlockSpot()
    {
        isLocked = false;
        Debug.Log($"[LadderSpot] {name} is now UNLOCKED!");
    }

    void OnDrawGizmos()
    {
        // 잠겨있으면 빨간색, 아니면 노란색
        Gizmos.color = isLocked ? Color.red : Color.yellow;
        if (ladderAnchor)
        {
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