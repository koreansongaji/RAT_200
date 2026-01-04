using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;

/// <summary>
/// E 키로 화면 하단 인벤토리 바를 토글 (DOTween 애니메이션 + 아이템 순차 등장).
/// 플레이어 위치와 두 개의 사전 지정된 점을 이용해 동적으로 삼각형을 그립니다.
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

    [Header("Data Source")]
    [SerializeField] ItemDatabase database;

    [Header("오디오 클립")]
    [SerializeField] AudioClip _inventoryopenSound;
    [SerializeField] AudioClip _inventorycloseSound;

    // (삼각형은 제외되었습니다)

    bool _visible;
    
    // 각각의 원래 위치 저장용
    private Vector2 _bgDefaultPos;
    private Vector2 _panelDefaultPos;

    // RectTransform의 anchorMin/Max 기본값 저장용 (좌/우 전환 시 사용)
    private Vector2 _bgAnchorMin;
    private Vector2 _bgAnchorMax;
    private Vector2 _panelAnchorMin;
    private Vector2 _panelAnchorMax;

    [Header("Inspector Link")]
    [SerializeField] ItemInspectorUI inspectorUI;

    public bool IsOpen => _visible;

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
        // 소리 로드
        if (_inventoryopenSound == null) _inventoryopenSound = Resources.Load<AudioClip>("Sounds/Effect/Rat/inventory_open");
        if (_inventorycloseSound == null) _inventorycloseSound = Resources.Load<AudioClip>("Sounds/Effect/Rat/inventory_close");
    
        // 초기 위치와 앵커 저장
        if (backgroundRect != null)
        {
            _bgDefaultPos = backgroundRect.anchoredPosition;
            _bgAnchorMin = backgroundRect.anchorMin;
            _bgAnchorMax = backgroundRect.anchorMax;
        }
        if (slotPanelRect != null)
        {
            _panelDefaultPos = slotPanelRect.anchoredPosition;
            _panelAnchorMin = slotPanelRect.anchorMin;
            _panelAnchorMax = slotPanelRect.anchorMax;
        }

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

        // 삼각형 관련 초기화는 제외되었습니다.
    }

    void Update()
    {
        // 토글 키(E) 처리 (기존 유지)
        if (Input.GetKeyDown(toggleKey))
        {
            // 상세창이 열려있으면 인벤토리 토글 막기 (선택사항, UX 취향)
            if (inspectorUI != null && inspectorUI.IsOpen) return;

            _visible = !_visible;
            ApplyVisibility();
        }

        // ★ [추가] ESC 키 처리
        // 인벤토리가 열려있을 때만 검사
        if (_visible && Input.GetKeyDown(KeyCode.Escape))
        {
            // 1순위: 상세창이 열려있다면? -> 상세창이 닫히도록 둠 (여기선 아무것도 안 함)
            if (inspectorUI != null && inspectorUI.IsOpen)
            {
                return;
            }

            // 2순위: 상세창이 없다면 -> 인벤토리 닫기
            _visible = false;
            ApplyVisibility();
        }

        if (_visible)
        {
            RefreshUI();
        }
    }

    /// <summary>
    /// 인벤토리 토글 시 UI 상태를 적용합니다.
    /// </summary>
    void ApplyVisibility()
    {
        if (player == null) return;

        // 애니메이션 중복 실행 방지 (모든 트윈 킬)
        if (backgroundRect != null) backgroundRect.DOKill();
        if (slotPanelRect != null) slotPanelRect.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
        foreach (var slot in itemSlots) if (slot != null) slot.transform.DOKill();
        // 삼각형 그래픽 관련 처리는 더 이상 필요하지 않습니다.

        Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);

        if (_visible)
        {
            // [OPEN]
            AudioManager.Instance.Play(_inventoryopenSound);
            
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
                // 플레이어가 화면 오른쪽 절반에 있을 경우 UI를 왼쪽에 표시하고, 그렇지 않으면 기본 위치(오른쪽)에 표시
                bool showOnLeft = playerScreenPos.x > Screen.width * 0.5f;

                // 타겟 앵커와 위치 계산
                Vector2 targetAnchorMin = _bgAnchorMin;
                Vector2 targetAnchorMax = _bgAnchorMax;
                Vector2 targetPos = _bgDefaultPos;
                if (showOnLeft)
                {
                    // 가로 앵커를 좌우 반전 (예: 1 -> 0, 0 -> 1) 하고 위치 x값 부호를 반전
                    targetAnchorMin = new Vector2(1f - _bgAnchorMax.x, _bgAnchorMin.y);
                    targetAnchorMax = new Vector2(1f - _bgAnchorMin.x, _bgAnchorMax.y);
                    targetPos = new Vector2(-_bgDefaultPos.x, _bgDefaultPos.y);
                }

                // 앵커 설정
                backgroundRect.anchorMin = targetAnchorMin;
                backgroundRect.anchorMax = targetAnchorMax;
                // pivot은 기본값 유지

                backgroundRect.position = playerScreenPos; // 플레이어 위치로 시작
                backgroundRect.localScale = Vector3.zero;  // 작게 시작

                // 타겟 위치로 이동
                backgroundRect.DOAnchorPos(targetPos, animDuration).SetEase(openEase);
                backgroundRect.DOScale(1f, animDuration).SetEase(openEase);
            }

            // 2. 슬롯 패널 애니메이션
            if (slotPanelRect != null)
            {
                bool showOnLeft = playerScreenPos.x > Screen.width * 0.5f;
                Vector2 targetAnchorMin = _panelAnchorMin;
                Vector2 targetAnchorMax = _panelAnchorMax;
                Vector2 targetPos = _panelDefaultPos;
                if (showOnLeft)
                {
                    targetAnchorMin = new Vector2(1f - _panelAnchorMax.x, _panelAnchorMin.y);
                    targetAnchorMax = new Vector2(1f - _panelAnchorMin.x, _panelAnchorMax.y);
                    targetPos = new Vector2(-_panelDefaultPos.x, _panelDefaultPos.y);
                }

                slotPanelRect.anchorMin = targetAnchorMin;
                slotPanelRect.anchorMax = targetAnchorMax;

                slotPanelRect.position = playerScreenPos; // 플레이어 위치로 시작
                slotPanelRect.localScale = Vector3.zero;  // 작게 시작

                slotPanelRect.DOAnchorPos(targetPos, animDuration).SetEase(openEase);
                slotPanelRect.DOScale(1f, animDuration).SetEase(openEase);
            }

            // 3. 내부 슬롯들 순차 팝업
            AnimateSlots();

            // 삼각형 관련 로직은 제외되었습니다.
        }
        else
        {
            // [CLOSE]
            AudioManager.Instance.Play(_inventorycloseSound);
            
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

            // 삼각형 페이드 아웃은 제외되었습니다.
        }
    }

    // 삼각형 업데이트 메서드는 더 이상 사용되지 않습니다.

    /// <summary>
    /// 아이템 슬롯 애니메이션 처리. 활성 슬롯은 순차적으로 팝업합니다.
    /// </summary>
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

    /// <summary>
    /// 플레이어가 보유하고 있는 아이템을 기반으로 슬롯 UI를 업데이트합니다.
    /// </summary>
    void RefreshUI()
    {
        if (player == null || itemSlots == null || itemSlots.Length == 0) return;
        if (database == null) return;

        int slotIndex = 0;

        for (int i = 0; i < database.items.Count; i++)
        {
            if (slotIndex >= itemSlots.Length) break;

            var itemInfo = database.items[i];
            if (string.IsNullOrEmpty(itemInfo.id)) continue;

            if (player.HasItem(itemInfo.id))
            {
                Image slotImage = itemSlots[slotIndex];

                if (slotImage != null)
                {
                    slotImage.sprite = itemInfo.icon;
                    slotImage.preserveAspect = true;
                    slotImage.enabled = true;

                    // ★★★ [추가된 부분] 클릭 이벤트 연결 ★★★
                    // 슬롯 게임오브젝트에 Button 컴포넌트가 있어야 합니다.
                    Button btn = slotImage.GetComponent<Button>();
                    if (btn == null) btn = slotImage.gameObject.AddComponent<Button>();

                    // 기존 리스너 제거 (중복 방지) 후 새 리스너 등록
                    btn.onClick.RemoveAllListeners();

                    // 클로저 문제 방지를 위해 로컬 변수 캡처
                    var currentItemData = itemInfo;
                    btn.onClick.AddListener(() => OnSlotClick(currentItemData));
                }
                slotIndex++;
            }
        }

        // 남은 슬롯 비활성화
        for (int i = slotIndex; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] != null)
            {
                itemSlots[i].enabled = false;
                // 비활성 슬롯은 버튼도 꺼야 함
                Button btn = itemSlots[i].GetComponent<Button>();
                if (btn) btn.onClick.RemoveAllListeners();
            }
        }
    }
    void OnSlotClick(ItemDatabase.ItemData data)
    {
        if (inspectorUI != null)
        {
            inspectorUI.OpenInspector(data);
        }
    }
}