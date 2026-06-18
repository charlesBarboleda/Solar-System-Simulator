using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SimulationMapTrail : MaskableGraphic
{
    [Header("Trail Settings")]
    [SerializeField] int _maxPoints = 512;
    [SerializeField] float _minDistanceAU = 0.01f;
    [SerializeField] float _lineThickness = 2f;
    [SerializeField] Color _trailColor = new(1f, 1f, 1f, 0.35f);
    [SerializeField] bool _fadeToAlpha = true;

    [NonSerialized] public float MapScale;
    [NonSerialized] public Quaternion MapRotation = Quaternion.identity;
    [NonSerialized] public double3 PlayerPosition;

    public Func<double3, Vector2> ProjectWorldToMap;

    readonly Queue<double3> _points = new();
    double3 _lastRecordedPosition;
    bool _hasFirst;

    public void RecordPosition(double3 absoluteWorldPos)
    {
        if (!_hasFirst)
        {
            _lastRecordedPosition = absoluteWorldPos;
            _hasFirst = true;
            _points.Enqueue(absoluteWorldPos);
            return;
        }

        double dist = math.length(absoluteWorldPos - _lastRecordedPosition);
        if (dist < (double)_minDistanceAU * PhysicsConstants.UNITY_UNITS_PER_AU) return;

        _points.Enqueue(absoluteWorldPos);
        _lastRecordedPosition = absoluteWorldPos;

        while (_points.Count > _maxPoints)
            _points.Dequeue();
    }

    public void ClearTrail()
    {
        _points.Clear();
        _hasFirst = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_points.Count < 2 || ProjectWorldToMap == null) return;

        double3[] pts = _points.ToArray();
        int count = pts.Length;

        Vector2[] canvasPos = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            double3 relative = pts[i] - PlayerPosition;
            Vector2 projected = ProjectWorldToMap(relative);
            Vector2 scaled = projected * MapScale;
            canvasPos[i] = scaled;
        }

        for (int i = 0; i < count - 1; i++)
        {
            float t0 = _fadeToAlpha ? (float)i / (count - 1) : 1f;
            float t1 = _fadeToAlpha ? (float)(i + 1) / (count - 1) : 1f;

            Color c0 = new(_trailColor.r, _trailColor.g, _trailColor.b, _trailColor.a * t0);
            Color c1 = new(_trailColor.r, _trailColor.g, _trailColor.b, _trailColor.a * t1);

            DrawSegment(vh, canvasPos[i], canvasPos[i + 1], c0, c1);
        }
    }

    void DrawSegment(VertexHelper vh, Vector2 a, Vector2 b, Color ca, Color cb)
    {
        Vector2 dir = b - a;
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector2 normal = 0.5f * _lineThickness * new Vector2(-dir.normalized.y, dir.normalized.x);

        int idx = vh.currentVertCount;

        vh.AddVert(a - normal, ca, Vector2.zero);
        vh.AddVert(a + normal, ca, Vector2.zero);
        vh.AddVert(b + normal, cb, Vector2.zero);
        vh.AddVert(b - normal, cb, Vector2.zero);

        vh.AddTriangle(idx + 0, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx + 0);
    }
}