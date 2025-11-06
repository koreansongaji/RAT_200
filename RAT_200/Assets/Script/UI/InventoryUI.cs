using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// E 키로 화면 하단 인벤토리 바를 토글.
/// 클릭/드래그 등 입력은 없고, 보유 중인 아이템을 TMP로만 나열.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerInteractor player;     // 플레이어(보유 플래그 읽기)
    [SerializeField] CanvasGroup canvasGroup;     // 하단 바 루트에 붙이기
    [SerializeField] TMP_Text bodyText;           // 아이템 나열 텍스트

    [Header("Toggle")]
    [SerializeField] KeyCode toggleKey = KeyCode.E;
    [SerializeField] bool startHidden = true;
    [SerializeField] float fadeDuration = 0.12f;  // 0으로 두면 즉시 전환

    [Header("표시용 라벨 매핑")]
    [Tooltip("보유 플래그 ID → 화면에 보일 라벨. 위에서부터 순서대로 표시됨.")]
    [SerializeField]
    List<ItemLabel> knownItems = new()
    {
        new("Sodium",        "나트륨 (Na)"),
        new("Gel",           "젤"),
        new("Flask",         "빈 플라스크"),
        new("WaterInFlask",  "물 든 플라스크"),
        new("Recipe",        "레시피 종이"),
    };

    bool _visible;
    float _fadeT;

    [System.Serializable]
    public struct ItemLabel
    {
        public string id;
        public string label;
        public ItemLabel(string id, string label) { this.id = id; this.label = label; }
    }

    void Awake()
    {
        //if (!player) player = FindObjectOfType<PlayerInteractor>();
        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        _visible = !startHidden;
        ApplyVisibility(instant: true);
        RefreshText();
    }

    void Update()
    {
        // 토글
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
            _fadeT = 0f;
        }

        // 페이드 적용
        if (canvasGroup)
        {
            if (fadeDuration <= 0f) ApplyVisibility(instant: true);
            else
            {
                _fadeT += Time.unscaledDeltaTime / fadeDuration;
                float target = _visible ? 1f : 0f;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_fadeT)));
                canvasGroup.blocksRaycasts = false; // 입력 차단 없음(보기 전용)
                canvasGroup.interactable = false;
            }
        }

        // 열린 동안엔 매 프레임 갱신(간단)
        if (_visible) RefreshText();
    }

    void ApplyVisibility(bool instant)
    {
        if (!canvasGroup) return;
        float a = _visible ? 1f : 0f;
        canvasGroup.alpha = a;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        if (instant) _fadeT = 1f;
    }

    void RefreshText()
    {
        if (!player || !bodyText) return;

        // 보유한 것만 리스트업
        var sb = new StringBuilder();
        sb.Append("<b>Inventory</b>\n");

        int found = 0;
        for (int i = 0; i < knownItems.Count; i++)
        {
            var k = knownItems[i];
            if (string.IsNullOrEmpty(k.id)) continue;
            if (player.HasItem(k.id))
            {
                sb.Append("- ").Append(k.label).Append('\n');
                found++;
            }
        }

        if (found == 0) sb.Append("<i>(보유한 아이템 없음)</i>");

        bodyText.text = sb.ToString();
    }
}
