#if UNITY_EDITOR
using UnityEngine;

public class DevHotkeys : MonoBehaviour
{
    [Header("키 조합 (기본: Shift + F6)")]
    public KeyCode mainKey = KeyCode.F6;
    public bool requireShift = true;
    public bool requireCtrl = false;
    public bool requireAlt = false;

    [Header("아이템 ID (프로젝트 내 일관 ID 사용)")]
    public string sodiumId = "Sodium";
    public string gelId = "Gel";
    public string waterInFlaskId = "WaterInFlask";

    void Update()
    {
        // 안전장치: 에디터에서만 작동
        if (!Application.isEditor) return;

        if (IsComboDown())
        {
            var pi = FindAnyObjectByType<PlayerInteractor>();
            if (!pi)
            {
                Debug.LogWarning("[DevHotkeys] PlayerInteractor를 찾지 못했어요.");
                return;
            }

            Give(pi, sodiumId);
            Give(pi, gelId);
            Give(pi, waterInFlaskId);

            Debug.Log("[DevHotkeys] 지급 완료: Sodium, Gel, WaterInFlask");
        }
    }

    bool IsComboDown()
    {
        if (!Input.GetKeyDown(mainKey)) return false;
        if (requireShift && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return false;
        if (requireCtrl && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) return false;
        if (requireAlt && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) return false;
        return true;
    }

    void Give(PlayerInteractor pi, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        pi.AddItem(id);
    }
}
#endif
