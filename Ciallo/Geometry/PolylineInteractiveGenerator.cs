using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Geometry;

/// <summary>
/// Generate polyline geometry with stylus/mouse interaction.
/// Usage:
/// - Call Start Update End together with Interactor.
/// - Call Clear to reset state.
/// </summary>
/// <remarks>
/// Challenges to solve:
/// - Input events:
///     - both undersampling and oversampling of input events.
///     - only get pixel coordinate in grid (optimal we can get 1/4 subpixel accuracy, but no)
///     - world coordinate is derived from pixel coordinate, so it's grid too.
/// - Self intersection detection.
/// - Smoothness
/// 
/// Different devices report input events at different rates.
/// For example, shen's mouse can report at 1000Hz, while his touch screen laptop with stylus only reports around 150Hz.
/// This low rate results in undersampling even for regular usage.
/// New Wacom tablets in 2025 have 240-360Hz (DTC-141) polling rates.
/// Must deal both under and oversampling to avoid inconsistent experience.
/// </remarks>
/// <remarks>
/// Explanation about "regular usage":
/// Write small English letters at a normal writing speed,
/// you will find 150Hz cannot find enough points to represent the "turning points" of the letters, such as bottoms of "w", "v".
/// Interpolation is necessary and not a good solution.
/// I guess this undersampling/interpolation together is the reason why we feel weired when using stylus to write text.
/// </remarks>
/// Note: Tried to interpolate with quadratic Bézier curve without knowing the next point
/// but the result introduce tilde shape artifacts at corners.
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

    private List<CursorMotionData> _previewPointDatas = new() { Capacity = 32 };

    // This is not regular cursor motion, but motion from previous to last saved point
    private CursorMotionData _latestPoint;
    private bool _latestPointIsTurningPoint = false;
    private bool _latestSegmentNeedInterpolation = false;
    private float _processIntervalMs = 0f; // time interval since last saved point, in ms

    // Thresholds about when to process and save sampled points.
    private readonly float _underForwardThreshold = 5f; // in screen pixel
    private readonly float _overForwardThreshold = 15f;
    private readonly float _windingAngleThreshold = Mathf.DegToRad(12); // one pixel tolerance at 5 pixels distance
    private readonly float _pressureDeltaThreshold = 0.08f;
    private readonly float _overTimeThreshold = 100f;

    private readonly float _interpolationAngleTolerance = Mathf.DegToRad(12);

    public void Start(CursorButtonData data)
    {
        _processIntervalMs = 0f;

        // Add initial point
        _positions.Add(data.WorldPosition);
        _pressures.Add(data.Pressure);
        _tilts.Add(data.Tilt);
        _radii.Add(CalculateRadius(data));

        _latestPoint = data;
        _latestPointIsTurningPoint = true;
        _latestSegmentNeedInterpolation = false;
    }

    // Always add current motion point as a preview point, then check whether to process and save preview points according to serval thresholds.
    // For introducing zero lag.
    public void Update(CursorMotionData data)
    {
        _processIntervalMs += data.TimeDeltaMs;

        if (!AllowIntersection && CheckSelfIntersection(data.WorldPosition)) return;

        _positions.Add(data.WorldPosition);
        _radii.Add(CalculateRadius(data));
        _pressures.Add(data.Pressure);
        _tilts.Add(data.Tilt);

        _previewPointDatas.Add(data);

        // Since we can only detect pixel coordinate in grid, less than 3 or 4 pixels gives invalid forward moving direction and speed.
        // One pixel distance is enough for determine if cursor is turning back, but not for forward direction/speed.
        // So we use `_underForwardThreshold` for the minimum distance computing forward movement detection.
        bool isTurningBack = data.WorldDelta.Normalized().Dot(_latestPoint.WorldDirection) < -1e-5;
        if (isTurningBack && !_latestPointIsTurningPoint)
        {
            // Directly process turning back case.
            RemoveLatestPoints(_previewPointDatas.Count);
            // Save the previous event point as the last point.
            float r = CalculateRadius(data);
            _positions.Add(data.PrevWorldPosition);
            _radii.Add(r);
            _pressures.Add(data.PrevPressure);
            _tilts.Add(data.PrevTilt);

            if (_latestSegmentNeedInterpolation) InterpolateSegment();
            UpdateLastestPoint(new CursorButtonData()
            {
                WorldPosition = data.PrevWorldPosition,
                ScreenPosition = data.PrevScreenPosition,
                Pressure = data.PrevPressure,
                Tilt = data.PrevTilt,
            });
            _latestPointIsTurningPoint = true;
            _latestSegmentNeedInterpolation = false;

            return;
        }

        // return if not reach distance threshold to determine direction.
        if (_latestPoint.ScreenPosition.DistanceTo(data.ScreenPosition) < _underForwardThreshold) return;

        float cosWindingAngle = _latestPoint.WorldPosition.DirectionTo(data.WorldPosition).Dot(_latestPoint.WorldDirection);

        bool isLarger = _latestPoint.ScreenPosition.DistanceTo(data.ScreenPosition) > _overForwardThreshold;
        bool isPressureChanging = Mathf.Abs(data.Pressure - _latestPoint.Pressure) > _pressureDeltaThreshold;
        bool isWinding = cosWindingAngle < Mathf.Cos(_windingAngleThreshold);
        bool isOvertime = _processIntervalMs > _overTimeThreshold;

        bool toProcessPoints = isLarger || isWinding || isPressureChanging || isOvertime;
        if (!toProcessPoints) return;

        RemoveLatestPoints(_previewPointDatas.Count);

        // Place the point
        _positions.Add(data.WorldPosition);
        _radii.Add(CalculateRadius(data));
        _pressures.Add(data.Pressure);
        _tilts.Add(data.Tilt);

        if (_latestSegmentNeedInterpolation) InterpolateSegment();
        UpdateLastestPoint(data);
        _latestSegmentNeedInterpolation = isWinding && !_latestPointIsTurningPoint;
        _latestPointIsTurningPoint = false;
    }

    public void End(CursorButtonData data)
    {
        _previewPointDatas.Clear();
        _latestPointIsTurningPoint = false;
        _latestSegmentNeedInterpolation = false;
    }

    public void Clear()
    {
        _latestPointIsTurningPoint = false;
        _latestSegmentNeedInterpolation = false;
        _positions.Clear();
        _radii.Clear();
        _pressures.Clear();
        _tilts.Clear();
        _processIntervalMs = 0;
        _previewPointDatas.Clear();
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

    private void RemoveLatestPoints(int n)
    {
        for (int i = 0; i < n; i++)
        {
            int lastIndex = _positions.Count - 1;
            _positions.RemoveAt(lastIndex);
            _radii.RemoveAt(lastIndex);
            _pressures.RemoveAt(lastIndex);
            _tilts.RemoveAt(lastIndex);
        }
    }

    private void UpdateLastestPoint(CursorButtonData data)
    {
        // Update last point data
        _latestPoint = new()
        {
            ScreenPosition = data.ScreenPosition,
            WorldPosition = data.WorldPosition,
            Pressure = data.Pressure,
            Tilt = data.Tilt,

            ScreenDelta = data.ScreenPosition - _latestPoint.ScreenPosition,
            WorldDelta = data.WorldPosition - _latestPoint.WorldPosition,
            PressureDelta = data.Pressure - _latestPoint.Pressure,
            TiltDelta = data.Tilt - _latestPoint.Tilt,
            TimeDeltaMs = _processIntervalMs,
        };

        _previewPointDatas.Clear();
        _processIntervalMs = 0;
    }

    private void InterpolateSegment()
    {
        if (_positions.Count <= 3) return;
        var newPosition = Geometry.CatmullRomInterpolation(
            _positions[^4],
            _positions[^3],
            _positions[^2],
            _positions[^1],
            0.5f);
        var newRadius = (_radii[^3] + _radii[^2]) * 0.5f;
        var newPressure = (_pressures[^3] + _pressures[^2]) * 0.5f;
        var newTilt = (_tilts[^3] + _tilts[^2]) * 0.5f;

        _positions.Insert(_positions.Count - 2, newPosition);
        _radii.Insert(_radii.Count - 2, newRadius);
        _pressures.Insert(_pressures.Count - 2, newPressure);
        _tilts.Insert(_tilts.Count - 2, newTilt);
    }

    // Warning: Brutal algorithm, only suitable for short polyline.
    private bool CheckSelfIntersection(Vector2 p)
    {
        if (_positions.Count < 3) return false;

        var p3 = p;
        var p2 = _positions[^1];

        for (var i = 0; i < _positions.Count - 2; i++)
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
}