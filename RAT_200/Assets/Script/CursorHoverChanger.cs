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
    private PlayerInteractor _cachedPlayer;

    void Start()
    {
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
            // 상호작용 컴포넌트가 하나라도 붙어있는지 체크
            bool hasAnyComponent = false;
            bool canInteractAny = false;

            // 1. BaseInteractable (사다리 오르기, 문, 아이템 등)
            var interactable = hit.collider.GetComponentInParent<BaseInteractable>();
            if (interactable != null)
            {
                hasAnyComponent = true;
                if (interactable.CanInteract(_cachedPlayer)) canInteractAny = true;
            }

            // 2. PressableButton3D (버튼)
            var btn3D = hit.collider.GetComponentInParent<PressableButton3D>();
            if (btn3D != null)
            {
                hasAnyComponent = true;
                if (btn3D.interactable) canInteractAny = true;
            }

            // 3. [New] Draggable3D (사다리 이동, 가구 이동 등)
            // 이걸 추가해야 사다리가 땅에 있을 때(오르기 불가)도 드래그 가능(이동 가능)으로 인식됨
            var draggable = hit.collider.GetComponentInParent<Draggable3D>();
            if (draggable != null)
            {
                hasAnyComponent = true;
                if (draggable.isActiveAndEnabled) canInteractAny = true;
            }

            // [판단 로직]
            if (hasAnyComponent)
            {
                // 컴포넌트가 있다면, 그 중 하나라도 상호작용 가능해야 커서를 바꿈
                // (사다리의 경우: 오르기는 False여도 드래그가 True면 커서가 바뀜)
                showHoverCursor = canInteractAny;
            }
            else
            {
                // 스크립트 없는 단순 배경 오브젝트 등이 레이어에 포함된 경우
                // (기획에 따라 false로 바꿔도 됨)
                showHoverCursor = true;
            }
        }

        // 커서 적용
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