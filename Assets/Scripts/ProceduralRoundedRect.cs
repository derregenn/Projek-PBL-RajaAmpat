using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ProceduralRoundedRect : MaskableGraphic
{
    [Range(0, 100)] public float cornerRadius = 25f;
    [Range(4, 32)] public int cornerSegments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float width = rect.width;
        float height = rect.height;

        if (width <= 0 || height <= 0) return;

        // Clamp radius to prevent overlapping corners
        float radius = Mathf.Min(cornerRadius, Mathf.Min(width, height) * 0.5f);

        if (radius <= 0)
        {
            DrawQuad(vh, rect);
            return;
        }

        // Add Center Vertex (with UV mapping)
        int centerIndex = vh.currentVertCount;
        Vector2 centerUV = GetUV(rect.center, rect);
        vh.AddVert(new Vector3(rect.center.x, rect.center.y, 0), color, centerUV);

        // Corrected counter-clockwise corner order matching 0 to 2*PI rotation
        Vector2[] cornerCenters = new Vector2[]
        {
            new Vector2(rect.xMax - radius, rect.yMax - radius), // 1. Top Right (0 to 90°)
            new Vector2(rect.xMin + radius, rect.yMax - radius), // 2. Top Left (90° to 180°)
            new Vector2(rect.xMin + radius, rect.yMin + radius), // 3. Bottom Left (180° to 270°)
            new Vector2(rect.xMax - radius, rect.yMin + radius)  // 4. Bottom Right (270° to 360°)
        };

        float angleStep = (Mathf.PI * 0.5f) / cornerSegments;

        // Generate Outer Perimeter Vertices
        for (int i = 0; i < 4; i++)
        {
            float startAngle = i * (Mathf.PI * 0.5f);
            for (int j = 0; j <= cornerSegments; j++)
            {
                float angle = startAngle + (j * angleStep);
                Vector2 pos = cornerCenters[i] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(new Vector3(pos.x, pos.y, 0), color, GetUV(pos, rect));
            }
        }

        // Build Triangles connecting Center to Outer Perimeter
        int totalOuterVerts = 4 * (cornerSegments + 1);
        for (int i = 1; i <= totalOuterVerts; i++)
        {
            int nextIndex = (i % totalOuterVerts) + 1;
            vh.AddTriangle(centerIndex, i, nextIndex);
        }
    }

    private Vector2 GetUV(Vector2 position, Rect rect)
    {
        return new Vector2(
            (position.x - rect.xMin) / rect.width,
            (position.y - rect.yMin) / rect.height
        );
    }

    private void DrawQuad(VertexHelper vh, Rect rect)
    {
        vh.AddVert(new Vector3(rect.xMin, rect.yMin, 0), color, new Vector2(0, 0));
        vh.AddVert(new Vector3(rect.xMin, rect.yMax, 0), color, new Vector2(0, 1));
        vh.AddVert(new Vector3(rect.xMax, rect.yMax, 0), color, new Vector2(1, 1));
        vh.AddVert(new Vector3(rect.xMax, rect.yMin, 0), color, new Vector2(1, 0));

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
}