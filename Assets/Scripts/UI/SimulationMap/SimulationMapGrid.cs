using UnityEngine;
using UnityEngine.UI;

public class SimulationMapGrid : MaskableGraphic
{
    [Header("Grid Settings")]
    [SerializeField] float _minorGridThickness = 1f;
    [SerializeField] float _majorGridThickness = 2f;
    [SerializeField] Color _minorGridColor = new(1f, 1f, 1f, 0.08f);
    [SerializeField] Color _majorGridColor = new(1f, 1f, 1f, 0.2f);
    [SerializeField] int _majorLineEvery = 5;

    [Header("Visual Density")]
    [SerializeField] float _pixelsPerCell = 80f;

    public float PixelsPerCell => _pixelsPerCell;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;

        float step = _pixelsPerCell;

        if (step < 4f) return;

        float left = rect.xMin;
        float right = rect.xMax;
        float bottom = rect.yMin;
        float top = rect.yMax;

        float startX = Mathf.Floor(left / step) * step;
        for (float x = startX; x <= right; x += step)
        {
            int lineIndex = Mathf.RoundToInt(x / step);
            bool major = Mathf.Abs(lineIndex % _majorLineEvery) == 0;
            DrawLine(
                vh,
                new Vector2(x, bottom),
                new Vector2(x, top),
                major ? _majorGridThickness : _minorGridThickness,
                major ? _majorGridColor : _minorGridColor);
        }

        float startY = Mathf.Floor(bottom / step) * step;
        for (float y = startY; y <= top; y += step)
        {
            int lineIndex = Mathf.RoundToInt(y / step);
            bool major = Mathf.Abs(lineIndex % _majorLineEvery) == 0;
            DrawLine(
                vh,
                new Vector2(left, y),
                new Vector2(right, y),
                major ? _majorGridThickness : _minorGridThickness,
                major ? _majorGridColor : _minorGridColor);
        }
    }

    void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = 0.5f * thickness * new Vector2(-direction.y, direction.x);

        int index = vh.currentVertCount;

        vh.AddVert(start - normal, color, Vector2.zero);
        vh.AddVert(start + normal, color, Vector2.zero);
        vh.AddVert(end + normal, color, Vector2.zero);
        vh.AddVert(end - normal, color, Vector2.zero);

        vh.AddTriangle(index + 0, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index + 0);
    }
}