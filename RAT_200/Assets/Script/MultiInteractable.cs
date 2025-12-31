using UnityEngine;
using System.Collections.Generic;

public class MultiInteractable : BaseInteractable
{
    [Header("실행할 인터랙션 목록")]
    [Tooltip("순서대로 실행됩니다.")]
    public List<BaseInteractable> interactables;

    [Header("설정")]
    [Tooltip("목록 중 하나라도 상호작용 가능하면 true 반환")]
    public bool checkAny = true;

    public override bool CanInteract(PlayerInteractor i)
    {
        // 목록이 비었으면 불가
        if (interactables == null || interactables.Count == 0) return false;

        // checkAny가 true면: 하나라도 가능하면 OK
        // checkAny가 false면: 전부 가능해야 OK
        foreach (var interactable in interactables)
        {
            if (interactable == null) continue;
            bool can = interactable.CanInteract(i);

            if (checkAny && can) return true;
            if (!checkAny && !can) return false;
        }
        return !checkAny; // 여기까지 왔으면 조건 충족
    }

    public override void Interact(PlayerInteractor i)
    {
        if (interactables == null) return;

        foreach (var interactable in interactables)
        {
            if (interactable != null && interactable.CanInteract(i))
            {
                interactable.Interact(i);
            }
        }
    }
}