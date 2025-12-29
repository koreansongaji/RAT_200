#if UNITY_EDITOR
using UnityEngine;

public class DevHotkeys : MonoBehaviour
{
    [Header("단축키 설정")]
    public KeyCode itemGiveKey = KeyCode.F6;      // 아이템 지급
    public KeyCode noiseToggleKey = KeyCode.F5;   // 소음 차단 토글 (추가됨)

    [Header("조합 키")]
    public bool requireShift = true;
    public bool requireCtrl = false;
    public bool requireAlt = false;

    [Header("아이템 ID")]
    public string sodiumId = "Sodium";
    public string gelId = "Gel";
    public string waterInFlaskId = "WaterInFlask";

    void Update()
    {
        if (!Application.isEditor) return;

        // 1. 아이템 지급 (F6)
        if (IsComboDown(itemGiveKey))
        {
            var pi = FindAnyObjectByType<PlayerInteractor>();
            if (pi)
            {
                Give(pi, sodiumId);
                Give(pi, gelId);
                Give(pi, waterInFlaskId);
                Debug.Log("[DevHotkeys] 아이템 지급 완료");
            }
            else
            {
                Debug.LogWarning("[DevHotkeys] PlayerInteractor를 찾을 수 없습니다.");
            }
        }

        // 2. 소음 시스템 토글 (F5)
        if (IsComboDown(noiseToggleKey))
        {
            ToggleNoiseSystem();
        }
    }

    // 키 입력을 확인하는 공용 함수로 변경
    bool IsComboDown(KeyCode key)
    {
        if (!Input.GetKeyDown(key)) return false;
        if (requireShift && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return false;
        if (requireCtrl && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) return false;
        if (requireAlt && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) return false;
        return true;
    }

    void ToggleNoiseSystem()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null)
        {
            // 상태 반전
            ns.isDebugPaused = !ns.isDebugPaused;

            if (ns.isDebugPaused)
                Debug.Log($"<color=cyan>[DevHotkeys] 소음 시스템 PAUSED (소음 증가 안 함)</color>");
            else
                Debug.Log($"<color=green>[DevHotkeys] 소음 시스템 RESUMED (정상 작동)</color>");
        }
        else
        {
            Debug.LogWarning("[DevHotkeys] 씬에 NoiseSystem이 없습니다.");
        }
    }

    void Give(PlayerInteractor pi, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        pi.AddItem(id);
    }
}
#endif