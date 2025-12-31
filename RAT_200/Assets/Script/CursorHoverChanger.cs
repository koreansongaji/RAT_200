using UnityEngine;

public class CursorHoverChanger : MonoBehaviour
{
    [Header("Cursor Textures")]
    public Texture2D defaultCursor;
    public Texture2D hoverCursor;

    [Header("Layer")]
    public LayerMask hoverLayer;

    [Header("Cursor Settings")]
    public Vector2 hotspot = Vector2.zero;

    private bool isHovering = false;

    // CanInteract 확인용 플레이어 참조
    private PlayerInteractor _cachedPlayer;

    void Start()
    {
        // 씬에 있는 플레이어를 미리 찾아둡니다.
        _cachedPlayer = FindFirstObjectByType<PlayerInteractor>();
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hitHoverLayer = Physics.Raycast(ray, out hit, Mathf.Infinity, hoverLayer);
        bool showHoverCursor = false;

        if (hitHoverLayer)
        {
            // 1. 기본적으로 레이어가 맞으면 커서를 바꿀 준비를 함
            showHoverCursor = true;

            // 2. [추가] 물체가 '상호작용 불가능' 상태인지 정밀 검사

            // Case A: BaseInteractable (실험대, 문, 아이템 등)
            // 콜라이더 본체 혹은 부모에서 스크립트를 찾음
            var interactable = hit.collider.GetComponentInParent<BaseInteractable>();
            if (interactable != null)
            {
                // CanInteract가 false라면(잠김, 이미 깸 등) 커서 안 바꿈
                if (!interactable.CanInteract(_cachedPlayer))
                {
                    showHoverCursor = false;
                }
            }

            // Case B: PressableButton3D (실험대 위의 버튼들)
            var btn3D = hit.collider.GetComponentInParent<PressableButton3D>();
            if (btn3D != null)
            {
                // 버튼 자체가 비활성화(interactable == false)라면 커서 안 바꿈
                if (!btn3D.interactable)
                {
                    showHoverCursor = false;
                }
            }
        }

        // 3. 최종 결정에 따라 커서 변경
        if (showHoverCursor)
        {
            if (!isHovering)
            {
                Cursor.SetCursor(hoverCursor, hotspot, CursorMode.Auto);
                isHovering = true;
            }
        }
        else
        {
            if (isHovering)
            {
                Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
                isHovering = false;
            }
        }
    }
}