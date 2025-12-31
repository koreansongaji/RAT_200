using UnityEngine;

public class PickupInteractable : BaseInteractable
{
    [SerializeField] string itemId = "Sodium"; // "Sodium" | "Gel" | "Flask" | "Recipe" ��
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
        i.AddItem(itemId); // ���� �÷��׸� ON (�Ҹ� X)
        
        Debug.Log($"[PickupInteractable] �÷��̾ ������ '{itemId}' ȹ��");

        _picked = true;
        // ���� ǥ��: ������ų� ��¦�̰ų�
        gameObject.SetActive(!oneTime);
        // (����) SFX / �佺Ʈ ��
    }
}
