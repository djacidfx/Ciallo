using System;
using System.Collections.Generic;
using System.Linq;
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
/// - Zero lag
/// - Input events:
///     - Both undersampling and oversampling of input events.
///     - Only get pixel coordinate in grid (optimal we can get 1/4 subpixel accuracy, but no)
///     - World coordinate is derived from pixel coordinate, so it's grid too.
///     - First button event always has zero pressure. Must use the latest pressure user start to move his pen. 
/// - Self intersection detection
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
    public Func<float, float> RadiusSampler;

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

    private List<CursorMotionData> _previewPointCache = new() { Capacity = 128 }; // Preserve data for analyzing a better interpolation in the future.
    private List<CursorButtonData> _recordedPointCache = [];
    private List<float> _recordedRadiusCache = [];
    private CursorMotionData _lastRecordedMotion; // This is motion from previous to last recorded point

    private bool _latestPointIsFirstPoint = false;
    private bool _latestPointIsTurningPoint = false;
    private bool _segmentNeedInterpolation = false;
    private TimeSpan _intervalSinceLastRecord = TimeSpan.Zero;

    // Taper ending state
    private bool _isTaperEnding = false;
    private int _taperStartIndex = 0;
    private float _taperStartPressure = 0f;
    // Tracks the pressure just before a sudden pressure drop (>= PressureDropThreshold per recorded point).
    // Used as the taper start pressure so taper always begins from the last "real" pressure.
    private const float PressureDropThreshold = 0.1f;

    // Thresholds about when to process and save sampled points.
    private readonly float _underForwardThreshold = 3f; // in screen pixel
    private readonly float _windingOffsetThreshold = 5f; // pixel threshold on the offset consider pen is not moving straight.
    private readonly float _overForwardThreshold = 25f;
    private readonly float _pressureDeltaThreshold = 0.08f;
    private readonly float _overTimeThreshold = 100f;
    private readonly int _maxInterpolatedPointNumber = 1;

    private readonly float _interpolationAngleTolerance = Mathf.DegToRad(15f);

    public void Start(CursorButtonData data)
    {
        _intervalSinceLastRecord = TimeSpan.Zero;

        // Add initial point
        _positions.Add(data.WorldPosition);
        _pressures.Add(data.Pressure); // This always gives 0, since logically, pen is not pressing before a down event. Deal with this in the Update function.
        _tilts.Add(data.Tilt);
        _radii.Add(CalculateRadius(data.Pressure));

        _lastRecordedMotion = (CursorMotionData)data;
        _latestPointIsFirstPoint = true;
        _latestPointIsTurningPoint = true;
        _segmentNeedInterpolation = false;
    }

    // Always add current motion point as a preview point, then check whether to process and save preview points according to serval thresholds.
    // For introducing zero lag.
    public void Update(CursorMotionData data)
    {
        _intervalSinceLastRecord += data.TimeDelta;

        if (!AllowIntersection && CheckSelfIntersection(data.WorldPosition)) return;

        _positions.Add(data.WorldPosition);
        _radii.Add(CalculateRadius(data.Pressure));
        _pressures.Add(data.Pressure);
        _tilts.Add(data.Tilt);

        _previewPointCache.Add(data);

        // Since we can only detect pixel coordinate in grid, forward distance less than 3 or 4 pixels gives invalid moving direction and speed.
        // However, one pixel distance is enough to determine if cursor is turning back.
        // So we use `_underForwardThreshold` for the minimum distance to detect forward movement. and use one pixel distance to detect turning back.
        bool isTurningBack = data.WorldDelta.Normalized().Dot(_lastRecordedMotion.WorldDirection) < -1e-5; // When direction is zero vector, Normalized gives zero too.
        if (isTurningBack && !_latestPointIsTurningPoint)
        {
            // Directly process turning back case.
            RemoveLatestPoints(_previewPointCache.Count);
            _previewPointCache.Clear();
            // Save the previous event point as the last point.
            float r = CalculateRadius(data.Pressure);
            Record(data.PrevScreenPosition, data.PrevWorldPosition, data.PrevPressure, data.PrevTilt, r);
            _latestPointIsTurningPoint = true;
            _segmentNeedInterpolation = false;

            return;
        }

        // Return if not reach distance threshold to determine direction.
        if (_lastRecordedMotion.ScreenPosition.DistanceTo(data.ScreenPosition) < _underForwardThreshold) return;

        bool isLarger = _lastRecordedMotion.ScreenPosition.DistanceTo(data.ScreenPosition) > _overForwardThreshold;
        bool isPressureChanging = Mathf.Abs(data.Pressure - _lastRecordedMotion.Pressure) > _pressureDeltaThreshold;
        bool isWinding = data.ScreenPosition.DistanceToLine(_lastRecordedMotion.ScreenPosition, _lastRecordedMotion.ScreenDirection) > _windingOffsetThreshold;
        bool isOvertime = _intervalSinceLastRecord.TotalMilliseconds > _overTimeThreshold;

        bool toRecord = isLarger || isWinding || isPressureChanging || isOvertime;
        if (!toRecord) return;
        RemoveLatestPoints(_previewPointCache.Count);
        _previewPointCache.Clear();

        float radius = CalculateRadius(data.Pressure);
        if (_latestPointIsFirstPoint)
        {
            _radii[0] = radius;
            _pressures[0] = data.Pressure;
            _latestPointIsFirstPoint = false;
        }

        Record(data.ScreenPosition, data.WorldPosition, data.Pressure, data.Tilt, radius);

        // Post-processing
        if (_segmentNeedInterpolation)
            InterpolateLatestSegment();
        _segmentNeedInterpolation = isWinding && !_latestPointIsTurningPoint;
        if (!isWinding) Smooth();
        _latestPointIsTurningPoint = false;
        RedistributeTaper();
    }

    // Redistribute pressure over all taper points from _taperStartPressure to 0,
    // weighted by cumulative arc length so pressure decays smoothly regardless of point spacing.
    // Radii are recalculated from the redistributed pressures.
    private void RedistributeTaper()
    {
        if (!_isTaperEnding) return;
        int start = _taperStartIndex;
        int end = _positions.Count - 1;
        if (end <= start) return;

        // Build cumulative distances from start point.
        float totalLength = 0f;

        Span<float> cumDist = stackalloc float[end - start + 1];
        cumDist[0] = 0f;
        for (int i = start + 1; i <= end; i++)
        {
            totalLength += _positions[i].DistanceTo(_positions[i - 1]);
            cumDist[i - start] = totalLength;
        }

        if (totalLength < 1e-6f) return;

        for (int i = start; i <= end; i++)
        {
            float t = cumDist[i - start] / totalLength;
            t = t * t * t;
            _pressures[i] = Mathf.Lerp(_taperStartPressure, 0f, t);
            _radii[i] = CalculateRadius(_pressures[i]);
        }
    }

    // In place Laplacian
    private void Smooth()
    {
        const float smoothingFactor = 0.1f;
        for (int i = 0; i < 4; i++)
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
        }
    }

    /// <summary>
    /// Call when the pen is lifted to begin a taper-ending phase.
    /// Pressure and radius of all subsequently recorded points decay from the
    /// second-to-last recorded point's values to 0. Taper ends when <see cref="End"/> is called.
    /// No-op if there are fewer than 2 recorded points.
    /// </summary>
    public void StartTaperEnding()
    {
        if (_recordedPointCache.Count < 2) return;
        // Remove preview points first so they don't corrupt the drop-detection scan.
        RemoveLatestPoints(_previewPointCache.Count);
        _previewPointCache.Clear();

        for (int i = _pressures.Count - 1; i >= 1; i--)
        {
            if (_pressures[i - 1] - _pressures[i] <= PressureDropThreshold)
            {
                _isTaperEnding = true;
                _taperStartIndex = i - 1;
                _taperStartPressure = _pressures[i - 1];
                if (i - 2 >= 0) // Add a point ugly
                {
                    _taperStartIndex = i - 2;
                    _taperStartPressure = _pressures[i - 2];
                }
                break;
            }
        }
    }

    public void End(CursorButtonData data)
    {
        if (_previewPointCache.Count > 0)
        {
            RemoveLatestPoints(_previewPointCache.Count);
            _positions.Add(data.WorldPosition);
            float radius = CalculateRadius(_previewPointCache[^1].Pressure);
            _radii.Add(radius);
            _pressures.Add(_previewPointCache[^1].Pressure);
            _tilts.Add(data.Tilt);
            RedistributeTaper();
        }

        _previewPointCache.Clear();
        _recordedPointCache.Clear();
        _recordedRadiusCache.Clear();
        _latestPointIsTurningPoint = false;
        _segmentNeedInterpolation = false;
        _isTaperEnding = false;
    }

    public void Clear()
    {
        _latestPointIsFirstPoint = false;
        _latestPointIsTurningPoint = false;
        _segmentNeedInterpolation = false;
        _positions.Clear();
        _radii.Clear();
        _pressures.Clear();
        _tilts.Clear();
        _intervalSinceLastRecord = TimeSpan.Zero;
        _previewPointCache.Clear();
        _recordedPointCache.Clear();
        _recordedRadiusCache.Clear();
        _isTaperEnding = false;
        _taperStartPressure = 0f;
    }

    private float CalculateRadius(float pressure)
    {
        return Mode switch
        {
            RadiusMode.Fixed => FixedRadius,
            RadiusMode.Sampled => RadiusSampler!(pressure),
            _ => throw new InvalidOperationException($"Unsupported RadiusMode: {Mode}")
        };
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

    private void Record(Vector2 screenPosition, Vector2 worldPosition, float pressure, Vector2 tilt, float r)
    {
        _positions.Add(worldPosition);
        _radii.Add(r);
        _pressures.Add(pressure);
        _tilts.Add(tilt);


        // Motion data
        _lastRecordedMotion = new()
        {
            ScreenPosition = screenPosition,
            WorldPosition = worldPosition,
            Pressure = pressure,
            Tilt = tilt,

            ScreenDelta = screenPosition - _lastRecordedMotion.ScreenPosition,
            WorldDelta = worldPosition - _lastRecordedMotion.WorldPosition,
            PressureDelta = pressure - _lastRecordedMotion.Pressure,
            TiltDelta = tilt - _lastRecordedMotion.Tilt,
            TimeDelta = _intervalSinceLastRecord,
        };

        // Recorded point cache
        _recordedPointCache.Add(new CursorButtonData { ScreenPosition = screenPosition, WorldPosition = worldPosition, Pressure = pressure, Tilt = tilt });
        _recordedRadiusCache.Add(r);
        if (_recordedPointCache.Count > 4) _recordedPointCache.RemoveAt(0);
        if (_recordedRadiusCache.Count > 4) _recordedRadiusCache.RemoveAt(0);

        // Preview cache and reset interval
        _previewPointCache.Clear();
        _intervalSinceLastRecord = TimeSpan.Zero;
    }

    private void InterpolateLatestSegment()
    {
        if (_recordedPointCache.Count < 4) return;

        var p0 = _recordedPointCache[^4].WorldPosition;
        var p1 = _recordedPointCache[^3].WorldPosition;
        var p2 = _recordedPointCache[^2].WorldPosition;
        var p3 = _recordedPointCache[^1].WorldPosition;

        // Estimate how many points are needed for smoothness
        var dir01 = p0.DirectionTo(p1);
        var dir23 = p2.DirectionTo(p3);
        var angle = Mathf.Acos(dir01.Dot(dir23));
        int nPoints = Mathf.CeilToInt(angle / _interpolationAngleTolerance);
        if (nPoints <= 0) return;
        nPoints = int.Min(nPoints, _maxInterpolatedPointNumber);
        var ts = Enumerable.Range(1, nPoints)
            .Select(i => i / (float)(nPoints + 1))
            .ToList();

        var newPositions = ts.Select(t =>
            p0.CatmullRomInterpolation(p1, p2, p3, t));
        var newRadii = ts.Select(t =>
            _recordedRadiusCache[^4].CatmullRomInterpolation(_recordedRadiusCache[^3],
                _recordedRadiusCache[^2],
                _recordedRadiusCache[^1], t));
        var newPressures = ts.Select(t =>
            _recordedPointCache[^4].Pressure.CatmullRomInterpolation(_recordedPointCache[^3].Pressure,
                _recordedPointCache[^2].Pressure,
                _recordedPointCache[^1].Pressure, t));
        var newTilts = ts.Select(t =>
            _recordedPointCache[^4].Tilt.CatmullRomInterpolation(_recordedPointCache[^3].Tilt,
                _recordedPointCache[^2].Tilt,
                _recordedPointCache[^1].Tilt, t));

        _positions.InsertRange(_positions.Count - 2, newPositions);
        _radii.InsertRange(_radii.Count - 2, newRadii);
        _pressures.InsertRange(_pressures.Count - 2, newPressures);
        _tilts.InsertRange(_tilts.Count - 2, newTilts);
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
            if (Geometry.SegmentIntersect(p0, p1, p2, p3).HasValue) return true;
        }

        return false;
    }
}