using UnityEngine;

/// <summary>
/// 쥐의 각종 행동 및 상태 변화에 따른 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class RatSoundController : MonoBehaviour
{
    [Header("Rat Sfx Clips")]
    [SerializeField] private AudioClip _walkClip;
    [SerializeField] private AudioClip _runClip;
    [SerializeField] private AudioClip _deathClip;
    [SerializeField] private AudioClip _nearVisionClip;
    [SerializeField] private AudioClip _interactClip;
    [SerializeField] private AudioClip _climbRopeClip;
    [SerializeField] private AudioClip _collectItemClip;
    [SerializeField] private AudioClip _ladderPieceClip;
    [SerializeField] private AudioClip _inventoryOpenClip;
    [SerializeField] private AudioClip _inventoryCloseClip;

    private void Awake()
    {
        // 리소스가 할당되지 않았을 경우 기본 경로에서 로드 시도
        if (_walkClip == null) _walkClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/rat_walk");
        if (_runClip == null) _runClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/rat_run");
        if (_deathClip == null) _deathClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/rat_death");
        if (_nearVisionClip == null) _nearVisionClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/rat_near_vision");
        if (_interactClip == null) _interactClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/interact");
        if (_climbRopeClip == null) _climbRopeClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/climb_rope");
        if (_collectItemClip == null) _collectItemClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/collect_item");
        if (_ladderPieceClip == null) _ladderPieceClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/ladder_piece");
        if (_inventoryOpenClip == null) _inventoryOpenClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/inventory_open");
        if (_inventoryCloseClip == null) _inventoryCloseClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/inventory_close");
    }

    /// <summary>
    /// 쥐가 걷는 효과음을 재생합니다.
    /// </summary>
    public void PlayWalk() => PlayEffect(_walkClip);

    /// <summary>
    /// 쥐가 달리는 효과음을 재생합니다.
    /// </summary>
    public void PlayRun() => PlayEffect(_runClip);

    /// <summary>
    /// 쥐가 죽었을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayDeath() => PlayEffect(_deathClip);

    /// <summary>
    /// 연구원의 시야에 근접했을 때 경고 효과음을 재생합니다.
    /// </summary>
    public void PlayNearVision() => PlayEffect(_nearVisionClip);

    /// <summary>
    /// 사물과 상호작용할 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayInteract() => PlayEffect(_interactClip);

    /// <summary>
    /// 밧줄을 오를 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayClimbRope() => PlayEffect(_climbRopeClip);

    /// <summary>
    /// 일반 아이템을 획득했을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayCollectItem() => PlayEffect(_collectItemClip);

    /// <summary>
    /// 사다리 가로대 조각을 획득했을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayLadderPiece() => PlayEffect(_ladderPieceClip);

    /// <summary>
    /// 인벤토리를 열 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayInventoryOpen() => PlayEffect(_inventoryOpenClip);

    /// <summary>
    /// 인벤토리를 닫을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayInventoryClose() => PlayEffect(_inventoryCloseClip);

    /// <summary>
    /// 공용 효과음 재생 로직 (AudioManager 활용)
    /// </summary>
    private void PlayEffect(AudioClip clip)
    {
        if (clip != null)
        {
            AudioManager.Instance.Play(clip, AudioManager.Sound.Effect);
        }
    }
}
