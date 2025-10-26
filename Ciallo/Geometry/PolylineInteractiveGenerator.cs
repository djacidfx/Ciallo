using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private readonly List<Vector2> _positions = new(2048);
    public IReadOnlyList<Vector2> Positions => _positions;
    private readonly List<float> _radii = new(2048);
    public IReadOnlyList<float> Radii => _radii;

    private readonly List<float> _pressures = new(2048);
    public IReadOnlyList<float> Pressures => _pressures;
    private readonly List<Vector2> _tilts = new(2048);
    public IReadOnlyList<Vector2> Tilts => _tilts;

    private bool _saveLatestPoint = false;
    private Vector2 _lastScreenPoint;
    private Vector2 _lastDirection;
    private float _lastPressure = -1.0f;

    private readonly float _minDistance = 3f; // in pixel
    private readonly float _maxDistance = 15f; // in pixel
    private readonly float _minCosWindingAngle = Mathf.Cos(Mathf.DegToRad(8f));
    private bool _previewPointAlreadyRemoved = false;

    private Stopwatch _interactStopwatch;

    public void Start(CursorButtonData data)
    {
        _interactStopwatch = Stopwatch.StartNew();

        _lastScreenPoint = data.ScreenPosition;
        _lastDirection = Vector2.FromAngle(0);
        _saveLatestPoint = true;

        _lastPressure = 0;
        _positions.Add(data.WorldPosition);
        _pressures.Add(_lastPressure);
        _tilts.Add(data.Tilt);
        _radii.Add(CalculateRadius(data));
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
    // This method introduces zero lag.
    public void Update(CursorMotionData data)
    {
        long deltaMs = _interactStopwatch.ElapsedMilliseconds;
        // GD.Print($"[PaintInteractor] Interacting delta: {deltaMs} ms");
        _interactStopwatch.Restart();

        if (!_saveLatestPoint && !_previewPointAlreadyRemoved)
        {
            RemoveLatestPoint();
        }
        _previewPointAlreadyRemoved = false;
        _saveLatestPoint = false;
        float pressure = data.Pressure;
        float radius = CalculateRadius(data);
        var position = data.WorldPosition;
        var tilt = data.Tilt;

        _positions.Add(position);
        _radii.Add(radius);
        _pressures.Add(pressure);
        _tilts.Add(tilt);

        if (!AllowIntersection && CheckSelfIntersection())
        {
            RemoveLatestPoint();
            _previewPointAlreadyRemoved = true;
            return;
        }

        bool isSmaller = data.ScreenPosition.DistanceTo(_lastScreenPoint) < _minDistance;
        bool isLarger = data.ScreenPosition.DistanceTo(_lastScreenPoint) > _maxDistance;
        bool isPressureChange = Mathf.Abs(data.Pressure - _lastPressure) > 0.08f;
        float cosWindingAngle = data.ScreenPosition.DirectionTo(_lastScreenPoint).Dot(_lastDirection);
        bool isWinding = cosWindingAngle < _minCosWindingAngle;
        bool saveThisPoint = !isSmaller && (isLarger || isWinding || isPressureChange);

        if (saveThisPoint)
        {
            // Basic smoothing
            const float smoothingFactor = 0.15f;
            for (int i = 0; i < 5; i++)
            {
                int idx = _positions.Count - 1 - i;
                if (idx < 2) break;

                // Don't smooth if two segments have large angle
                var dir1 = (_positions[idx] - _positions[idx - 1]).Normalized();
                var dir2 = (_positions[idx - 1] - _positions[idx - 2]).Normalized();
                if (dir1.Dot(dir2) < Mathf.Cos(Mathf.DegToRad(30f)))
                    break;

                _radii[idx] = Mathf.Lerp(_radii[idx], _radii[idx - 1], smoothingFactor);
                _positions[idx] = _positions[idx].Lerp(_positions[idx - 1], smoothingFactor);
                // no need to smooth pressure and tilt
            }

            _lastDirection = data.ScreenPosition.DirectionTo(_lastScreenPoint).Normalized();
            _lastScreenPoint = data.ScreenPosition;
            _lastPressure = pressure;
            _saveLatestPoint = true;
        }

        void RemoveLatestPoint()
        {
            _positions.RemoveAt(_positions.Count - 1);
            _radii.RemoveAt(_radii.Count - 1);
            _pressures.RemoveAt(_pressures.Count - 1);
            _tilts.RemoveAt(_tilts.Count - 1);
        }
    }


    public PolylineGeometry Collect()
    {
        return new PolylineGeometry
        {
            Positions = [.._positions],
            Radii = [.._radii],
            Pressures = [.._pressures],
            Tilts = [.._tilts],
        };
    }

    public void Clear()
    {
        _saveLatestPoint = false;
        _previewPointAlreadyRemoved = false;
        _lastPressure = -1.0f;
        _positions.Clear();
        _radii.Clear();
        _pressures.Clear();
        _tilts.Clear();
    }

    // Warning: Brutal algorithm, only suitable for short polyline.
    private bool CheckSelfIntersection()
    {
        if (_positions.Count < 4) return false;

        var p3 = _positions[^1];
        var p2 = _positions[^2];

        for (var i = 0; i < _positions.Count - 3; i++)
        {
            var p0 = _positions[i];
            var p1 = _positions[i + 1];
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