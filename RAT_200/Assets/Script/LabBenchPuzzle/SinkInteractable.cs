using UnityEngine;

public class SinkInteractable : BaseInteractable
{
    [Header("IDs")]
    [SerializeField] string flaskId = "Flask";
    [SerializeField] string waterInFlaskId = "WaterInFlask";

    public override bool CanInteract(PlayerInteractor i)
    {
        return i && i.HasItem(flaskId);   // 플라스크 있어야만 물을 받을 수 있음
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;

        // 전이: Flask -> WaterInFlask
        i.RemoveItem(flaskId);
        i.AddItem(waterInFlaskId);

        Debug.Log("[SinkInteractable] 플라스크가 WaterInFlask로 전이됨");

        // (선택) SFX/이펙트/토스트 "플라스크에 물을 채웠다"
    }
}
