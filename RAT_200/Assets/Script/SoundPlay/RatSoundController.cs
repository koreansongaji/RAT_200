using UnityEngine;
using UnityEngine.AI;

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

    [Header("Editor Tools")]
    [SerializeField] private bool _refreshAudioClips; // 인스펙터에서 이 체크박스를 누르면 실행됩니다.

    [SerializeField] private NavMeshAgent _agent;
    private Vector3 _lastPos;
    private float _distAccum;
    [SerializeField] private float _stepDist = 0.6f; // 기존 거리 로직은 사용하지 않음
    [SerializeField] private float _runSpeedThreshold = 2f; 


    private void Awake()
    {
        // 리소스 로드 로직 (기존 유지)
        LoadDefaults();
    }

    private void LoadDefaults()
    {
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

    private void Update()
    {
        if (_agent == null || !_agent.isOnNavMesh)
        {
            AudioManager.Instance.StopLoop("RatMovement");
            return;
        }

        float speed = _agent.velocity.magnitude;

        if (speed > 0.1f)
        {
            // 속도에 따라 클립 교체 및 재생
            AudioClip targetClip = (speed > _runSpeedThreshold) ? _runClip : _walkClip;
            AudioManager.Instance.PlayLoop(targetClip, "RatMovement");
        }
        else
        {
            AudioManager.Instance.StopLoop("RatMovement");
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoop("RatMovement");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_refreshAudioClips)
        {
            _refreshAudioClips = false;
            SetupAudioClips();
        }
    }

    [ContextMenu("Setup Audio Clips")] // 컴포넌트 최상단 이름 우클릭
    public void SetupAudioClips()
    {
        string basePath = "Sounds/Effect/Rat/";
        
        _walkClip = Resources.Load<AudioClip>(basePath + "rat_walk");
        _runClip = Resources.Load<AudioClip>(basePath + "rat_run");
        _deathClip = Resources.Load<AudioClip>(basePath + "rat_death");
        _nearVisionClip = Resources.Load<AudioClip>(basePath + "rat_near_vision");
        _interactClip = Resources.Load<AudioClip>(basePath + "interact");
        _climbRopeClip = Resources.Load<AudioClip>(basePath + "climb_rope");
        _collectItemClip = Resources.Load<AudioClip>(basePath + "collect_item");
        _ladderPieceClip = Resources.Load<AudioClip>(basePath + "ladder_piece");
        _inventoryOpenClip = Resources.Load<AudioClip>(basePath + "inventory_open");
        _inventoryCloseClip = Resources.Load<AudioClip>(basePath + "inventory_close");


        Debug.Log("<color=green>쥐 효과음 자동 할당 완료!</color>");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

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
