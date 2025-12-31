using UnityEngine;

[RequireComponent(typeof(LadderPlacementController))]
[RequireComponent(typeof(LadderClimbInteractable))]
public class Ladder : MonoBehaviour
{
    [Header("초기화")]
    [Tooltip("게임 시작 시 사다리가 강제로 이동할 위치 (빈 오브젝트 또는 LadderPlaceSpot)")]
    public Transform startPoint;

    [Header("Data")]
    [Range(0, 4)] public int lengthLevel = 0;
    public LadderPlaceSpot currentSpot;

    [Header("Visuals")]
    [Tooltip("레벨별 추가 발판 오브젝트들")]
    public GameObject[] extraRungs;

    [Header("Sound")]
    [SerializeField] private LadderSoundController _ladderSoundController;

    private LadderClimbInteractable _climbInteractable;

    void Awake()
    {
        _climbInteractable = GetComponent<LadderClimbInteractable>();
        if (!_ladderSoundController) _ladderSoundController = GetComponent<LadderSoundController>();
    }

    void Start()
    {
        // 1. 시작 위치로 이동 로직 (여기서 처리!)
        if (startPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;

            // 만약 시작 위치가 '설치 가능한 슬롯'이라면 설치 상태로 만들기
            var spot = startPoint.GetComponent<LadderPlaceSpot>();
            if (spot)
            {
                AttachTo(spot, true);
            }
        }

        UpdateVisuals();
    }

    // 아이템(발판) 획득
    public void AddRung()
    {
        if (lengthLevel < 4)
        {
            _ladderSoundController.PlayFixLadder();
            lengthLevel++;
            UpdateVisuals();
            Debug.Log($"[Ladder] Level Up! Current: {lengthLevel}");
        }
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < extraRungs.Length; i++)
        {
            if (extraRungs[i])
            {
                extraRungs[i].SetActive(lengthLevel >= (i + 1));
            }
        }
    }

    // 슬롯에 부착
    public void AttachTo(LadderPlaceSpot spot, bool alignRotation = true)
    {
        if (!spot || !spot.ladderAnchor) return;

        // 기존 자리 비우기
        if (currentSpot) currentSpot.occupied = false;

        // 물리적 이동
        transform.position = spot.ladderAnchor.position;
        if (alignRotation) transform.rotation = spot.ladderAnchor.rotation;

        // 데이터 갱신
        currentSpot = spot;
        spot.occupied = true;

        // ★ 상호작용 컴포넌트에 "어디로 올라갈지" 주입
        if (_climbInteractable && spot.climbPoint)
        {
            _climbInteractable.climbDestination = spot.climbPoint;
        }

        _ladderSoundController?.PlayPlaceLadder();
    }

    // 떼어내기 (드래그 시작 시)
    public void Detach()
    {
        if (currentSpot)
        {
            currentSpot.occupied = false;
            currentSpot = null;
        }

        // 목적지 제거 (올라가기 불가능)
        if (_climbInteractable)
        {
            _climbInteractable.climbDestination = null;
        }

        _ladderSoundController?.PlayPlaceLadder();
    }
}