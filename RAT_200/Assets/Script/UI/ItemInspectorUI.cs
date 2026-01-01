using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro; // TextMeshPro를 쓴다면 추가 (없으면 UnityEngine.UI.Text 사용)

public class ItemInspectorUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] GameObject panelRoot;        // 검은 배경 포함 전체 패널
    [SerializeField] Image itemImage;             // 크게 보여줄 아이템 이미지
    [SerializeField] TextMeshProUGUI nameText;    // 아이템 이름 (옵션)
    [SerializeField] Button closeButton;          // 닫기(X) 버튼
    [SerializeField] CanvasGroup canvasGroup;     // 페이드 효과 및 클릭 방지용

    [Header("Animation")]
    [SerializeField] float animDuration = 0.3f;

    void Awake()
    {
        // 시작 시 꺼두기
        if (panelRoot) panelRoot.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(CloseInspector);
    }

    public void OpenInspector(ItemDatabase.ItemData data)
    {
        if (data == null) return;

        // 1. 데이터 세팅
        if (itemImage)
        {
            itemImage.sprite = data.icon;
            itemImage.preserveAspect = true;
        }
        if (nameText) nameText.text = data.label;

        // 2. 활성화 및 애니메이션
        panelRoot.SetActive(true);

        // 투명도 0 -> 1
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, animDuration).SetUpdate(true); // 일시정지 상태에서도 작동하게 하려면 SetUpdate(true)

        // 크기 0.8 -> 1 (팝업 효과)
        itemImage.transform.localScale = Vector3.one * 0.8f;
        itemImage.transform.DOScale(1f, animDuration).SetEase(Ease.OutBack).SetUpdate(true);

        // 뒷배경 클릭 막기 (CanvasGroup이 있으면 자동 처리되지만 확실하게)
        canvasGroup.blocksRaycasts = true;
    }

    public void CloseInspector()
    {
        // 닫기 애니메이션
        canvasGroup.DOFade(0f, animDuration * 0.8f).SetUpdate(true).OnComplete(() =>
        {
            panelRoot.SetActive(false);
        });
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        // ESC 키로도 닫기
        if (panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInspector();
        }
    }
}