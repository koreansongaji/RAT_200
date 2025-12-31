using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Custom UI graphic that draws a filled triangle given three points in local space.
/// This allows us to dynamically update the vertices based on player position and preset points.
/// </summary>
public class TriangleGraphic : MaskableGraphic
{
    public Vector2 pointA;
    public Vector2 pointB;
    public Vector2 pointC;

    /// <summary>
    /// Assigns new vertex positions and marks the graphic as needing to be redrawn.
    /// Points should be specified in the local coordinate space of the parent RectTransform.
    /// </summary>
    /// <param name="a">First vertex</param>
    /// <param name="b">Second vertex</param>
    /// <param name="c">Third vertex</param>
    public void UpdateTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        pointA = a;
        pointB = b;
        pointC = c;
        SetVerticesDirty();
    }

    /// <summary>
    /// Populates the mesh with three vertices and one triangle.
    /// The vertices use the current graphic color.
    /// </summary>
    /// <param name="vh"></param>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        vh.AddVert(pointA, color, Vector2.zero);
        vh.AddVert(pointB, color, Vector2.zero);
        vh.AddVert(pointC, color, Vector2.zero);
        vh.AddTriangle(0, 1, 2);
    }
}