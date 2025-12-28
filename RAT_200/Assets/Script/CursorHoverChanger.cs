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

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hitHoverLayer =
            Physics.Raycast(ray, out hit, Mathf.Infinity, hoverLayer);

        if (hitHoverLayer)
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
