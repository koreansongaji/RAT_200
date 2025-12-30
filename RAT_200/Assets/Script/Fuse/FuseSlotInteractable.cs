using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FuseSlotInteractable : BaseInteractable
{
    [Header("Settings")]
    public string requiredItemID = "Fuse"; // 인벤토리 아이템 ID
    public FuseBoxPuzzleController controller;

    void Awake()
    {
        if (!controller) controller = GetComponentInParent<FuseBoxPuzzleController>();
    }

    // 마이크로 줌 상태일 때만 상호작용 가능
    public override bool CanInteract(PlayerInteractor i)
    {
        if (!controller) return false;
        // 컨트롤러가 아직 안 풀렸고 + 현재 줌인 상태여야 함
        var micro = controller.GetComponent<MicroZoomSession>();
        return micro && micro.InMicro;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;

        // 아이템 체크
        if (i.HasItem(requiredItemID))
        {
            // 1. 아이템 사용 (삭제)
            i.RemoveItem(requiredItemID);

            // 2. 컨트롤러에게 성공 알림
            controller.SolvePuzzle();

            // 3. 이 슬롯은 더 이상 클릭 안 되게 비활성
            GetComponent<Collider>().enabled = false;
        }
        else
        {
            Debug.Log("[FuseSlot] 퓨즈가 필요합니다.");
            // 퓨즈가 없다는 피드백 (소리, UI 등) 추가 가능
            CommonSoundController.Instance?.PlayPuzzleFail();
        }
    }
}