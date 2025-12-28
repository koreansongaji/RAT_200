using UnityEngine;

public class RungPickup : BaseInteractable
{
    [Header("Target")]
    [Tooltip("씬에 있는 플레이어의 사다리 오브젝트 연결")]
    public Ladder playerLadder;

    public override void Interact(PlayerInteractor i)
    {
        Debug.Log("가로대와 상호작용");
        if (playerLadder)
        {
            // 1. 사다리 레벨업 & 비주얼 갱신
            playerLadder.AddRung();

            // 2. 획득 효과음 등 (필요하면 추가)
            // if (SoundManager) SoundManager.Play("ItemGet");

            // 3. 이 가로대 오브젝트는 제거
            gameObject.SetActive(false);
        }
    }
}