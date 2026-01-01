using System;
using UnityEngine;

public class SinkInteractable : BaseInteractable
{
    [Header("IDs")]
    [SerializeField] string flaskId = "Flask";
    [SerializeField] string waterInFlaskId = "WaterInFlask";

    [Header("Sound Clips")]
    [SerializeField] private AudioClip _fillWaterClip;

    private void Awake()
    {
        if (_fillWaterClip == null)  _fillWaterClip = Resources.Load<AudioClip>("Sounds/Effect/Experiment/water");
    }
    public override bool CanInteract(PlayerInteractor i)
    {
        return i && i.HasItem(flaskId);   // �ö�ũ �־�߸� ���� ���� �� ����
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;

        // ����: Flask -> WaterInFlask
        i.RemoveItem(flaskId);
        i.AddItem(waterInFlaskId, transform.position);
        
        // 소리 출력
        AudioManager.Instance.Play(_fillWaterClip);
        
        Debug.Log("[SinkInteractable] �ö�ũ�� WaterInFlask�� ���̵�");

        // (����) SFX/����Ʈ/�佺Ʈ "�ö�ũ�� ���� ä����"
    }
}
