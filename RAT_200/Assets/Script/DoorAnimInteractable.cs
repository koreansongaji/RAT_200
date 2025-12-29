using UnityEngine;

public class DoorAnimInteractable : BaseInteractable
{
    [Header("Anim")]
    public Animator animator;
    public string boolName = "Open";

    bool _opened = false;

    public override void Interact(PlayerInteractor i)
    {
        if (!animator) return;

        _opened = !_opened;
        animator.SetBool(boolName, _opened);

        // 공용 사운드 재생
        if (_opened) CommonSoundController.Instance?.PlayDoorOpen();
        else CommonSoundController.Instance?.PlayDoorClose();
    }
}
