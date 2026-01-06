using UnityEngine;

public class FridgeCrossbarPickup : BaseInteractable
{
    [Header("Target")]
    public Ladder playerLadder;

    [Header("Event Triggers")]
    [Tooltip("이 아이템을 먹었을 때 활성화될 '책 무너짐' 트리거 오브젝트")]
    public GameObject bridgeCollapseTrigger;

    [Header("Lock Settings")]
    [Tooltip("체크하면 게임 시작 시 잠긴 상태로 시작합니다 (비밀번호 퍼즐 등으로 해제 필요).")]
    [SerializeField] private bool startLocked = true;

    // 내부 잠금 상태 변수
    private bool _isLocked;

    [SerializeField] private AudioClip _rungPickupSound;

    private void Awake()
    {
        // 시작 시 잠금 설정 적용
        _isLocked = startLocked;

        if (_rungPickupSound == null) _rungPickupSound = Resources.Load<AudioClip>("Sounds/Effect/Rat/ladder_piece");
    }

    // ★ [추가] 잠겨있으면 상호작용 불가능하다고 알림
    public override bool CanInteract(PlayerInteractor i)
    {
        if (_isLocked) return false;
        return true;
    }

    // ★ [추가] 외부에서 호출할 잠금 해제 함수
    public void Unlock()
    {
        _isLocked = false;
        Debug.Log("[FridgeCrossbarPickup] 잠금 해제됨! 이제 획득 가능합니다.");
    }

    public override void Interact(PlayerInteractor i)
    {
        // 안전 장치: 잠겨있으면 실행 안 함
        if (!CanInteract(i)) return;

        if (playerLadder)
        {
            AudioManager.Instance.Play(_rungPickupSound);
            playerLadder.AddRung();

            gameObject.SetActive(false);
        }

        if (bridgeCollapseTrigger) bridgeCollapseTrigger.SetActive(true);
    }
}