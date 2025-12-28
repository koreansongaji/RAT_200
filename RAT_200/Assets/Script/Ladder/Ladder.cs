using UnityEngine;

[RequireComponent(typeof(LadderPlacementController))]
public class Ladder : MonoBehaviour
{
    [Header("Data")]
    [Range(0, 4)] public int lengthLevel = 0; // ★ 0부터 시작!
    public LadderPlaceSpot currentSpot;

    [Header("Visuals")]
    [Tooltip("레벨 1, 2, 3, 4가 될 때 순서대로 켜질 가로대 오브젝트들 (총 4개 연결)")]
    public GameObject[] extraRungs;

    void Start()
    {
        // 시작 시 레벨 0 상태(다 꺼짐)로 초기화
        UpdateVisuals();
    }

    // 가로대 아이템(RungPickup)이 호출하는 함수
    public void AddRung()
    {
        if (lengthLevel < 4) // 최대 레벨 4 제한
        {
            lengthLevel++;
            UpdateVisuals();
            Debug.Log($"[Ladder] Level Up! Current Level: {lengthLevel}");
        }
    }

    void UpdateVisuals()
    {
        // extraRungs[0] : 레벨 1 이상일 때 켜짐
        // extraRungs[1] : 레벨 2 이상일 때 켜짐 ...
        for (int i = 0; i < extraRungs.Length; i++)
        {
            if (extraRungs[i] != null)
            {
                // ★ 조건 수정: (i + 1) 레벨부터 보임
                bool shouldActive = lengthLevel >= (i + 1);
                extraRungs[i].SetActive(shouldActive);
            }
        }
    }

    // ... (AttachTo, Detach 등 기존 코드는 그대로 유지) ...
    public void AttachTo(LadderPlaceSpot spot, bool alignRotation = true)
    {
        if (!spot || !spot.ladderAnchor) return;
        if (currentSpot) currentSpot.occupied = false;

        transform.position = spot.ladderAnchor.position;
        if (alignRotation) transform.rotation = spot.ladderAnchor.rotation;
        currentSpot = spot;
        spot.occupied = true;

        var climb = GetComponent<LadderClimbInteractable>();
        if (climb && spot.climbTarget) climb.target = spot.climbTarget;
    }

    public void Detach()
    {
        if (currentSpot) currentSpot.occupied = false;
        currentSpot = null;

        var climb = GetComponent<LadderClimbInteractable>();
        if (climb) climb.target = null;
    }
}