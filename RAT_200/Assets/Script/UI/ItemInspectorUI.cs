using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ItemInspectorUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Image itemImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] Button closeButton;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] float animDuration = 0.3f;

    // ★ [추가] 외부에서 열림 상태 확인용 프로퍼티
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        if (panelRoot) panelRoot.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(CloseInspector);
    }

    public void OpenInspector(ItemDatabase.ItemData data)
    {
        if (data == null) return;

        if (itemImage)
        {
            itemImage.sprite = data.icon;
            itemImage.preserveAspect = true;
        }
        if (nameText) nameText.text = data.label;

        panelRoot.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, animDuration).SetUpdate(true);

        itemImage.transform.localScale = Vector3.one * 0.8f;
        itemImage.transform.DOScale(1f, animDuration).SetEase(Ease.OutBack).SetUpdate(true);

        canvasGroup.blocksRaycasts = true;
    }

    public void CloseInspector()
    {
        canvasGroup.DOFade(0f, animDuration * 0.8f).SetUpdate(true).OnComplete(() =>
        {
            panelRoot.SetActive(false);
        });
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        // ★ [수정] 여기서 직접 ESC를 처리
        // 만약 열려있다면 ESC를 눌렀을 때 닫기 실행.
        // TitlePage는 이 IsOpen 상태를 보고 자기 동작을 멈출 것입니다.
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInspector();
        }
    }
}