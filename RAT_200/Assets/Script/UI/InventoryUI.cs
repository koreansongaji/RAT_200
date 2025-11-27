using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// E 키로 화면 하단 인벤토리 바를 토글 (DOTween 애니메이션 + 아이템 순차 등장).
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerInteractor player;
    
    // (변경) 두 개의 RectTransform으로 분리
    [SerializeField] RectTransform backgroundRect; // 배경 이미지
    [SerializeField] RectTransform slotPanelRect;  // 슬롯들이 담긴 패널
    
    [SerializeField] Image[] itemSlots;
    [SerializeField] CanvasGroup canvasGroup;      // 투명도 및 입력 제어용 (필수)

    [Header("Toggle")]
    [SerializeField] KeyCode toggleKey = KeyCode.E;
    [SerializeField] bool startHidden = true;

    [Header("Animation Settings")]
    [SerializeField] float animDuration = 0.4f;
    [SerializeField] float slotAnimDuration = 0.3f;
    [SerializeField] float slotAnimInterval = 0.05f;
    [SerializeField] Ease openEase = Ease.OutBack;
    [SerializeField] Ease closeEase = Ease.InBack;

    [Header("데이터 매핑")]
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
    
    // 각각의 원래 위치 저장용
    private Vector2 _bgDefaultPos;
    private Vector2 _panelDefaultPos;

    [System.Serializable]
    public struct ItemLabel
    {
        public string id;
        public string label;
        public Sprite icon;

        public ItemLabel(string id, string label, Sprite icon = null)
        {
            this.id = id;
            this.label = label;
            this.icon = icon;
        }
    }

    void Awake()
    {
        // 초기 위치 저장
        if (backgroundRect != null) _bgDefaultPos = backgroundRect.anchoredPosition;
        if (slotPanelRect != null) _panelDefaultPos = slotPanelRect.anchoredPosition;

        // 시작 상태 설정
        _visible = !startHidden;
        
        // *** SetActive(false) 대신 투명도와 크기로 숨김 처리 ***
        if (canvasGroup != null)
        {
            canvasGroup.alpha = _visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = _visible;
            canvasGroup.interactable = _visible;
        }
        
        // 배경과 패널 모두 크기 조절로 숨김/보임 처리
        if (backgroundRect != null) backgroundRect.localScale = _visible ? Vector3.one : Vector3.zero;
        if (slotPanelRect != null) slotPanelRect.localScale = _visible ? Vector3.one : Vector3.zero;

        if (_visible) RefreshUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
            ApplyVisibility();
        }

        if (_visible)
        {
            RefreshUI();
        }
    }

    void ApplyVisibility()
    {
        if (player == null) return;

        // 애니메이션 중복 실행 방지 (모든 트윈 킬)
        if (backgroundRect != null) backgroundRect.DOKill();
        if (slotPanelRect != null) slotPanelRect.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
        foreach (var slot in itemSlots) if (slot != null) slot.transform.DOKill();

        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);

        if (_visible)
        {
            // [OPEN]
            RefreshUI();

            if (canvasGroup != null) 
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
                canvasGroup.DOFade(1f, animDuration * 0.8f);
            }

            // 1. 배경 애니메이션
            if (backgroundRect != null)
            {
                backgroundRect.position = playerScreenPos; // 플레이어 위치로
                backgroundRect.localScale = Vector3.zero;  // 작게 시작
                
                backgroundRect.DOAnchorPos(_bgDefaultPos, animDuration).SetEase(openEase);
                backgroundRect.DOScale(1f, animDuration).SetEase(openEase);
            }

            // 2. 슬롯 패널 애니메이션
            if (slotPanelRect != null)
            {
                slotPanelRect.position = playerScreenPos; // 플레이어 위치로
                slotPanelRect.localScale = Vector3.zero;  // 작게 시작

                slotPanelRect.DOAnchorPos(_panelDefaultPos, animDuration).SetEase(openEase);
                slotPanelRect.DOScale(1f, animDuration).SetEase(openEase);
            }

            // 3. 내부 슬롯들 순차 팝업
            AnimateSlots();
        }
        else
        {
            // [CLOSE]
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                canvasGroup.DOFade(0f, animDuration * 0.8f);
            }

            // 1. 배경 닫기
            if (backgroundRect != null)
            {
                backgroundRect.DOMove(playerScreenPos, animDuration).SetEase(closeEase);
                backgroundRect.DOScale(0f, animDuration).SetEase(closeEase)
                    .OnComplete(() => { backgroundRect.anchoredPosition = _bgDefaultPos; });
            }

            // 2. 슬롯 패널 닫기
            if (slotPanelRect != null)
            {
                slotPanelRect.DOMove(playerScreenPos, animDuration).SetEase(closeEase);
                slotPanelRect.DOScale(0f, animDuration).SetEase(closeEase)
                    .OnComplete(() => { slotPanelRect.anchoredPosition = _panelDefaultPos; });
            }
        }
    }

    void AnimateSlots()
    {
        int activeSlotCount = 0;
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null && itemSlots[i].enabled)
            {
                itemSlots[i].transform.localScale = Vector3.zero;
                
                float delay = activeSlotCount * slotAnimInterval;
                itemSlots[i].transform.DOScale(1f, slotAnimDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay((animDuration * 0.2f) + delay);

                activeSlotCount++;
            }
            else
            {
                if(itemSlots[i] != null) itemSlots[i].transform.localScale = Vector3.one;
            }
        }
    }

    void RefreshUI()
    {
        if (player == null || itemSlots == null || itemSlots.Length == 0) return;

        int slotIndex = 0;

        for (int i = 0; i < knownItems.Count; i++)
        {
            if (slotIndex >= itemSlots.Length) break;

            var itemInfo = knownItems[i];
            if (string.IsNullOrEmpty(itemInfo.id)) continue;

            if (player.HasItem(itemInfo.id))
            {
                if (itemSlots[slotIndex] != null)
                {
                    itemSlots[slotIndex].sprite = itemInfo.icon;
                    itemSlots[slotIndex].preserveAspect = true;
                    itemSlots[slotIndex].enabled = true;
                }
                slotIndex++;
            }
        }

        for (int i = slotIndex; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null)
            {
                itemSlots[i].enabled = false;
            }
        }
    }
}
