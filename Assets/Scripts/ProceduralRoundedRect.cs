using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class ProceduralRoundedRect : MaskableGraphic
{
    [Range(0, 100)] public float cornerRadius = 25f;
    [Range(4, 32)] public int cornerSegments = 8;

    [Header("Outline Feedback")]
    [Min(0f)] public float outlineThickness = 0f;
    public Color outlineColor = Color.clear;

    public void SetOutline(bool visible, Color targetColor, float thickness = 14f)
    {
        outlineColor = visible ? targetColor : Color.clear;
        outlineThickness = visible ? Mathf.Max(0f, thickness) : 0f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float width = rect.width;
        float height = rect.height;

        if (width <= 0 || height <= 0) return;

        // Clamp radius to prevent overlapping corners
        float radius = Mathf.Min(cornerRadius, Mathf.Min(width, height) * 0.5f);

        if (outlineThickness > 0.01f && outlineColor.a > 0.001f)
        {
            DrawOutlineRing(vh, rect, radius, outlineThickness, outlineColor);
        }

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
        int ringStart = vh.currentVertCount;
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
        for (int i = 0; i < totalOuterVerts; i++)
        {
            int cur = ringStart + i;
            int next = ringStart + ((i + 1) % totalOuterVerts);
            vh.AddTriangle(centerIndex, cur, next);
        }
    }

    private void DrawOutlineRing(VertexHelper vh, Rect innerRect, float innerRadius, float thickness, Color ringColor)
    {
        if (innerRadius <= 0f)
        {
            Rect outerRect = new Rect(
                innerRect.xMin - thickness,
                innerRect.yMin - thickness,
                innerRect.width + thickness * 2f,
                innerRect.height + thickness * 2f);

            int baseIdx = vh.currentVertCount;
            vh.AddVert(new Vector3(outerRect.xMin, outerRect.yMin, 0), ringColor, new Vector2(0, 0));
            vh.AddVert(new Vector3(outerRect.xMin, outerRect.yMax, 0), ringColor, new Vector2(0, 1));
            vh.AddVert(new Vector3(outerRect.xMax, outerRect.yMax, 0), ringColor, new Vector2(1, 1));
            vh.AddVert(new Vector3(outerRect.xMax, outerRect.yMin, 0), ringColor, new Vector2(1, 0));

            vh.AddVert(new Vector3(innerRect.xMin, innerRect.yMin, 0), ringColor, new Vector2(0, 0));
            vh.AddVert(new Vector3(innerRect.xMin, innerRect.yMax, 0), ringColor, new Vector2(0, 1));
            vh.AddVert(new Vector3(innerRect.xMax, innerRect.yMax, 0), ringColor, new Vector2(1, 1));
            vh.AddVert(new Vector3(innerRect.xMax, innerRect.yMin, 0), ringColor, new Vector2(1, 0));

            for (int i = 0; i < 4; i++)
            {
                int o0 = baseIdx + i;
                int o1 = baseIdx + ((i + 1) % 4);
                int in0 = baseIdx + 4 + i;
                int in1 = baseIdx + 4 + ((i + 1) % 4);
                vh.AddTriangle(o0, o1, in1);
                vh.AddTriangle(in1, in0, o0);
            }
            return;
        }

        Vector2[] cornerCenters = new Vector2[]
        {
            new Vector2(innerRect.xMax - innerRadius, innerRect.yMax - innerRadius),
            new Vector2(innerRect.xMin + innerRadius, innerRect.yMax - innerRadius),
            new Vector2(innerRect.xMin + innerRadius, innerRect.yMin + innerRadius),
            new Vector2(innerRect.xMax - innerRadius, innerRect.yMin + innerRadius)
        };

        float outerRadius = innerRadius + thickness;
        float angleStep = (Mathf.PI * 0.5f) / cornerSegments;
        int totalPerim = 4 * (cornerSegments + 1);
        int startIdx = vh.currentVertCount;

        for (int i = 0; i < 4; i++)
        {
            float startAngle = i * (Mathf.PI * 0.5f);
            for (int j = 0; j <= cornerSegments; j++)
            {
                float angle = startAngle + (j * angleStep);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 innerPos = cornerCenters[i] + dir * innerRadius;
                Vector2 outerPos = cornerCenters[i] + dir * outerRadius;
                vh.AddVert(new Vector3(innerPos.x, innerPos.y, 0), ringColor, GetUV(innerPos, innerRect));
                vh.AddVert(new Vector3(outerPos.x, outerPos.y, 0), ringColor, GetUV(outerPos, innerRect));
            }
        }

        for (int i = 0; i < totalPerim; i++)
        {
            int next = (i + 1) % totalPerim;
            int in0 = startIdx + i * 2;
            int out0 = in0 + 1;
            int in1 = startIdx + next * 2;
            int out1 = in1 + 1;

            vh.AddTriangle(in0, out0, out1);
            vh.AddTriangle(out1, in1, in0);
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
        int baseIdx = vh.currentVertCount;
        vh.AddVert(new Vector3(rect.xMin, rect.yMin, 0), color, new Vector2(0, 0));
        vh.AddVert(new Vector3(rect.xMin, rect.yMax, 0), color, new Vector2(0, 1));
        vh.AddVert(new Vector3(rect.xMax, rect.yMax, 0), color, new Vector2(1, 1));
        vh.AddVert(new Vector3(rect.xMax, rect.yMin, 0), color, new Vector2(1, 0));

        vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
        vh.AddTriangle(baseIdx + 2, baseIdx + 3, baseIdx);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
}