using UnityEngine;

public class PickupInteractable : BaseInteractable
{
    [SerializeField] string itemId = "Sodium"; // "Sodium" | "Gel" | "Flask" | "Recipe" 등
    [SerializeField] bool oneTime = true;
    bool _picked;

    public override bool CanInteract(PlayerInteractor i)
    {
        if (!i) return false;
        if (oneTime && _picked) return false;
        return true;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        i.AddItem(itemId); // 보유 플래그만 ON (소모 X)

        Debug.Log($"[PickupInteractable] 플레이어가 아이템 '{itemId}' 획득");

        _picked = true;
        // 간단 표식: 사라지거나 반짝이거나
        gameObject.SetActive(!oneTime);
        // (선택) SFX / 토스트 등
    }
}
