using System;
using System.Collections.Generic;
using Ciallo.Data;
using Godot;

namespace Ciallo.Geometry;

/// <summary>
/// Generate polyline geometry with stylus/mouse interaction.
/// Usage:
/// - Call Start Update End together with Interactor, can call Collect any time to get current geometry.
/// - Call Clear to reset state.
///
/// Design:
/// - Support both with-radii and without-radii workflows via RadiusMode.
/// - When RadiusMode.None, radii are generated as zeros (same length as points) for compatibility.
/// - When RadiusMode.Fixed, use a constant radius for all points.
/// - When RadiusMode.Sampled, compute radius per motion using a provided sampler (e.g., brush pressure curve).
/// </summary>
public class PolylineInteractiveGenerator
{
    public enum RadiusMode
    {
        Fixed,
        Sampled,
    }

    public RadiusMode Mode = RadiusMode.Fixed;
    public float FixedRadius = 1f;
    public Func<CursorMotionData, float> RadiusSampler;
    // Controls if the new points can intersect with existing already generated polyline.
    public bool AllowIntersection = true;

    private readonly List<Vector2> _points = new(2048);
    private readonly List<float> _radii = new(2048);
    public IReadOnlyList<Vector2> Points => _points;
    public IReadOnlyList<float> Radii => _radii;

    private bool _saveLatestPoint = false;
    private Vector2 _lastScreenPoint;
    private Vector2 _lastDirection;
    private float _lastPressure = -1.0f;

    private readonly float _minDistance = 3f; // in pixel
    private readonly float _maxDistance = 15f; // in pixel
    private readonly float _minCosWindingAngle = Mathf.Cos(Mathf.DegToRad(5f));
    private bool _previewPointAlreadyRemoved = false;

    public void Start(CursorButtonData data)
    {
        _lastScreenPoint = data.ScreenPosition;
        _lastDirection = Vector2.FromAngle(0);
        _lastPressure = -1.0f;

        _points.Add(data.WorldPosition);
        switch (Mode)
        {
            case RadiusMode.Fixed:
                _radii.Add(FixedRadius);
                break;
            case RadiusMode.Sampled:
                _radii.Add(RadiusSampler!(default));
                break;
        }
        _saveLatestPoint = true;
    }

    private float CalculateRadius(CursorMotionData data)
    {
        switch (Mode)
        {
            case RadiusMode.Fixed:
                return FixedRadius;
            case RadiusMode.Sampled:
                return RadiusSampler!(data);
            default:
                throw new InvalidOperationException($"Unsupported RadiusMode: {Mode}");
        }
    }

    // Always add current motion point, then check whether to save current point. If not, remove it on next update.
    public void Update(CursorMotionData data)
    {
        if (!_saveLatestPoint && !_previewPointAlreadyRemoved)
        {
            _points.RemoveAt(_points.Count - 1);
            _radii.RemoveAt(_radii.Count - 1);
        }
        _previewPointAlreadyRemoved = false;
        _saveLatestPoint = false;
        float pressure = data.Pressure;
        float radius = CalculateRadius(data);
        var position = data.WorldPosition;
        _points.Add(position);
        _radii.Add(radius);

        if (!AllowIntersection && CheckSelfIntersection())
        {
            _points.RemoveAt(_points.Count - 1);
            _radii.RemoveAt(_radii.Count - 1);
            _previewPointAlreadyRemoved = true;
            return;
        }

        bool isSmaller = data.ScreenPosition.DistanceTo(_lastScreenPoint) < _minDistance;
        bool isLarger = data.ScreenPosition.DistanceTo(_lastScreenPoint) > _maxDistance;
        bool isPressureChange = Mathf.Abs(data.Pressure - _lastPressure) > 0.08f;
        bool isWinding = data.ScreenPosition.DirectionTo(_lastScreenPoint).Dot(_lastDirection) < _minCosWindingAngle;
        bool saveThisPoint = !isSmaller && (isLarger || isWinding || isPressureChange);

        if (saveThisPoint)
        {
            // Basic smoothing
            const float smoothingFactor = 0.15f;
            for (int i = 0; i < 5; i++)
            {
                int idx = _points.Count - 1 - i;
                if (idx < 2) break;

                // Don't smooth if two segments have large angle
                var dir1 = (_points[idx] - _points[idx - 1]).Normalized();
                var dir2 = (_points[idx - 1] - _points[idx - 2]).Normalized();
                if (dir1.Dot(dir2) < Mathf.Cos(Mathf.DegToRad(30f)))
                    break;

                _radii[idx] = Mathf.Lerp(_radii[idx], _radii[idx - 1], smoothingFactor);
                _points[idx] = _points[idx].Lerp(_points[idx - 1], smoothingFactor);
            }

            _lastDirection = data.ScreenPosition.DirectionTo(_lastScreenPoint).Normalized();
            _lastScreenPoint = data.ScreenPosition;
            _lastPressure = pressure;
            _saveLatestPoint = true;
        }
    }


    public PolylineGeometry Collect()
    {
        return new PolylineGeometry
        {
            Points = [.._points],
            Radii = [.._radii],
        };
    }

    public void Clear()
    {
        _saveLatestPoint = false;
        _points.Clear();
        _radii.Clear();
    }

    // Warning: Brutal algorithm, only suitable for short polylines.
    private bool CheckSelfIntersection()
    {
        if (_points.Count < 4) return false;

        var p3 = _points[^1];
        var p2 = _points[^2];

        for (var i = 0; i < _points.Count - 3; i++)
        {
            var p0 = _points[i];
            var p1 = _points[i + 1];
            if (SegmentIntersection(p0, p1, p2, p3))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SegmentIntersection(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var s1 = p1 - p0;
        var s2 = p3 - p2;

        var s = (-s1.Y * (p0.X - p2.X) + s1.X * (p0.Y - p2.Y)) / (-s2.X * s1.Y + s1.X * s2.Y);
        var t = (s2.X * (p0.Y - p2.Y) - s2.Y * (p0.X - p2.X)) / (-s2.X * s1.Y + s1.X * s2.Y);

        return s is >= 0 and <= 1 && t is >= 0 and <= 1;
    }

    public static Func<CursorMotionData, float> BrushToRadiusSampler(BrushSetting brush)
    {
        var baseRadius = brush.BaseRadius.Value;
        var curve = brush.Pressure2RadiusRatioCurve;
        return data => baseRadius * curve.SampleX(data.Pressure);
    }
}