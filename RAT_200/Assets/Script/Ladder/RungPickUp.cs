using System;
using UnityEngine;

public class RungPickup : BaseInteractable
{
    [Header("Target")]
    [Tooltip("���� �ִ� �÷��̾��� ��ٸ� ������Ʈ ����")]
    public Ladder playerLadder;
    
    [SerializeField] private AudioClip _rungPickupSound;

    private void Awake()
    {
        if(_rungPickupSound == null) _rungPickupSound = Resources.Load<AudioClip>("Sounds/Effect/Rat/ladder_piece");
    }

    public override void Interact(PlayerInteractor i)
    {
        Debug.Log("���δ�� ��ȣ�ۿ�");
        if (playerLadder)
        {
            AudioManager.Instance.Play(_rungPickupSound);
            // 1. ��ٸ� ������ & ���־� ����
            playerLadder.AddRung();

            // 3. �� ���δ� ������Ʈ�� ����
            gameObject.SetActive(false);
        }
    }
}