using UnityEngine;

public class InteractionBlocker : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("게임 시작 시 자동으로 잠글지 여부")]
    public bool startLocked = true;

    [Tooltip("잠겨있는 동안 적용할 레이어 이름 (보통 Default 추천)")]
    public string lockedLayerName = "Default";

    [Header("Debug Info")]
    [SerializeField] private bool _isLocked;
    private int _originalLayer;
    private int _lockedLayer;

    void Awake()
    {
        // 1. 원래 레이어(Interactable 등)를 기억해둠
        _originalLayer = gameObject.layer;

        // 2. 잠금용 레이어 인덱스 찾기
        _lockedLayer = LayerMask.NameToLayer(lockedLayerName);

        // 레이어 이름을 잘못 썼을 경우 대비
        if (_lockedLayer == -1)
        {
            Debug.LogWarning($"[InteractionBlocker] '{lockedLayerName}' 레이어를 찾을 수 없습니다. Default로 설정합니다.");
            _lockedLayer = 0; // Default
        }

        if (startLocked)
        {
            Lock();
        }
    }

    // 외부에서 호출하여 잠금 해제 (퓨즈가 떨어질 때 호출)
    public void Unlock()
    {
        if (!_isLocked) return;

        _isLocked = false;
        SetLayerRecursively(gameObject, _originalLayer);
        Debug.Log($"[InteractionBlocker] {name} Unlocked! (Interactable)");
    }

    // 다시 잠그기
    public void Lock()
    {
        _isLocked = true;
        SetLayerRecursively(gameObject, _lockedLayer);
    }

    // 자식 오브젝트들까지 싹 다 레이어 변경 (모델 구조가 복잡할 경우 대비)
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}