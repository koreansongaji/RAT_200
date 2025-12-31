using UnityEngine;

public class FridgeCrossbarPickup : BaseInteractable
{
    [Header("Target")]
    public Ladder playerLadder;

    [Header("Event Triggers")]
    [Tooltip("이 아이템을 먹었을 때 활성화될 '책 무너짐' 트리거 오브젝트")]
    public GameObject bridgeCollapseTrigger;

    [SerializeField] private AudioClip _rungPickupSound;

    private void Awake()
    {
        if (_rungPickupSound == null) _rungPickupSound = Resources.Load<AudioClip>("Sounds/Effect/Rat/ladder_piece");
    }

    public override void Interact(PlayerInteractor i)
    {
        if (playerLadder)
        {
            AudioManager.Instance.Play(_rungPickupSound);
            playerLadder.AddRung();

            gameObject.SetActive(false);
        }
        bridgeCollapseTrigger.SetActive(true);
    }
}