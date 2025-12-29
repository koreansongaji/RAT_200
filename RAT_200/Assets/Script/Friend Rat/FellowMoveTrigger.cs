using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FellowMoveTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        OnExit,       // 구역을 나갈 때 (A, B)
        OnEnter       // 구역에 들어올 때 (C)
    }

    [Header("Settings")]
    public TriggerType type = TriggerType.OnExit;
    public FellowRatController npcRat;
    public Transform destination;

    [Header("Condition")]
    [Tooltip("필요한 사다리 레벨. 0이나 1이면 무조건 통과. 2면 사다리 레벨 2 이상이어야 발동.")]
    public int requiredLadderLevel = 0;

    // 사다리 참조를 위해 (자동으로 찾거나 연결)
    private Ladder _cachedLadder;
    private bool _hasTriggered = false;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (type == TriggerType.OnEnter) TryMoveNPC(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (type == TriggerType.OnExit) TryMoveNPC(other);
    }

    void TryMoveNPC(Collider other)
    {
        if (_hasTriggered) return;

        // 플레이어 체크
        var player = other.GetComponent<PlayerInteractor>();
        if (player == null) return;

        // ★ [조건 체크] 사다리 레벨 확인
        if (requiredLadderLevel > 0)
        {
            if (_cachedLadder == null) _cachedLadder = FindAnyObjectByType<Ladder>();

            // ★ 레벨 0(시작) < 필요 레벨 1  => 발동 안 함 (Return)
            // ★ 레벨 1(획득) < 필요 레벨 1  => 발동 함 (Pass)
            if (_cachedLadder == null || _cachedLadder.lengthLevel < requiredLadderLevel)
            {
                return;
            }
        }

        // 이동 명령
        if (npcRat && destination)
        {
            npcRat.MoveTo(destination);
            _hasTriggered = true;
        }
    }
}