using System;
using System.Collections.Generic;
using Godot;
using InkStrokeModeler;
using NumericsVector2 = System.Numerics.Vector2;

namespace Ciallo.Geometry;

public readonly struct PolylineGeneratorGeometry(
    IReadOnlyList<Vector2> positions,
    IReadOnlyList<float> radii,
    IReadOnlyList<float> pressures,
    IReadOnlyList<Vector2> tilts)
{
    public IReadOnlyList<Vector2> Positions { get; } = positions;
    public IReadOnlyList<float> Radii { get; } = radii;
    public IReadOnlyList<float> Pressures { get; } = pressures;
    public IReadOnlyList<Vector2> Tilts { get; } = tilts;
    public int Count => Positions.Count;
}

/// <summary>
/// Generates interactive polyline geometry from cursor input.
/// </summary>
/// <remarks>
/// CurrentGeometry is a short-lived view over internal buffers. During Moving it
/// may include transient stroke prediction; after End it contains stable samples
/// only. Callers should consume it immediately and persist copies only.
/// </remarks>
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

    private readonly StrokeModeler _modeler = new();
    private readonly StrokeModelParams _modelerParams = StrokeModelParams.CreateDefault();
    private readonly List<ModelerResult> _updateResults = new(256);
    private readonly List<ModelerResult> _predictionResults = new(256);

    private readonly List<Vector2> _stablePositions = new(2048);
    private readonly List<float> _stableRadii = new(2048);
    private readonly List<float> _stablePressures = new(2048);
    private readonly List<Vector2> _stableTilts = new(2048);

    private readonly List<Vector2> _predictionPositions = new(256);
    private readonly List<float> _predictionRadii = new(256);
    private readonly List<float> _predictionPressures = new(256);
    private readonly List<Vector2> _predictionTilts = new(256);

    private readonly List<Vector2> _positions = new(2304);
    private readonly List<float> _radii = new(2304);
    private readonly List<float> _pressures = new(2304);
    private readonly List<Vector2> _tilts = new(2304);
    private readonly List<RawStylusSample> _rawSamples = new(256);

    private CursorButtonData _pendingDown;
    private ModelerInput? _lastModelerInput;
    private double _elapsedSeconds;
    private bool _strokeStarted;

    public PolylineGeneratorGeometry CurrentGeometry => new(_positions, _radii, _pressures, _tilts);

    public void Start(CursorButtonData data)
    {
        Clear();
        _modeler.Reset(_modelerParams);
        _pendingDown = data;
        ComposeCurrentGeometry();
    }

    public void Update(CursorMotionData data)
    {
        if (!_strokeStarted)
            BeginStroke(data.Pressure, data.Tilt);

        _elapsedSeconds += Math.Max(0, data.TimeDelta.TotalSeconds);
        AddRawSample(data.WorldPosition, data.Tilt, _elapsedSeconds);
        UpdateModeler(new ModelerInput(
            InputEventType.Move,
            ToNumericsVector2(data.WorldPosition),
            TimeSpan.FromSeconds(_elapsedSeconds),
            data.Pressure,
            -1,
            -1));
        RebuildPrediction();
        ComposeCurrentGeometry();
    }

    public void End(CursorButtonData data)
    {
        if (!_strokeStarted)
            BeginStroke(data.Pressure, data.Tilt);

        AddRawSample(data.WorldPosition, data.Tilt, _elapsedSeconds);
        _predictionPositions.Clear();
        _predictionRadii.Clear();
        _predictionPressures.Clear();
        _predictionTilts.Clear();
        UpdateModeler(new ModelerInput(
            InputEventType.Up,
            ToNumericsVector2(data.WorldPosition),
            TimeSpan.FromSeconds(_elapsedSeconds),
            data.Pressure,
            -1,
            -1));
        ComposeCurrentGeometry();

        _strokeStarted = false;
    }

    public void Clear()
    {
        _stablePositions.Clear();
        _stableRadii.Clear();
        _stablePressures.Clear();
        _stableTilts.Clear();
        _predictionPositions.Clear();
        _predictionRadii.Clear();
        _predictionPressures.Clear();
        _predictionTilts.Clear();
        _positions.Clear();
        _radii.Clear();
        _pressures.Clear();
        _tilts.Clear();
        _rawSamples.Clear();
        _updateResults.Clear();
        _predictionResults.Clear();
        _elapsedSeconds = 0;
        _strokeStarted = false;
        _lastModelerInput = null;
    }

    private void BeginStroke(float initialPressure, Vector2 initialTilt)
    {
        _strokeStarted = true;
        _elapsedSeconds = 0;
        _rawSamples.Clear();
        AddRawSample(_pendingDown.WorldPosition, initialTilt, _elapsedSeconds);
        UpdateModeler(new ModelerInput(
            InputEventType.Down,
            ToNumericsVector2(_pendingDown.WorldPosition),
            TimeSpan.Zero,
            initialPressure,
            -1,
            -1));
    }

    private void UpdateModeler(ModelerInput input)
    {
        if (_lastModelerInput.HasValue && _lastModelerInput.Value == input)
            return;

        _updateResults.Clear();
        _modeler.Update(input, _updateResults);
        _lastModelerInput = input;
        AppendStableResults(_updateResults);
    }

    private void RebuildPrediction()
    {
        _predictionPositions.Clear();
        _predictionRadii.Clear();
        _predictionPressures.Clear();
        _predictionTilts.Clear();
        _predictionResults.Clear();
        _modeler.Predict(_predictionResults);
        AppendResults(_predictionResults, _predictionPositions, _predictionRadii, _predictionPressures, _predictionTilts);
    }

    private void AppendStableResults(IReadOnlyList<ModelerResult> results) =>
        AppendResults(results, _stablePositions, _stableRadii, _stablePressures, _stableTilts);

    private void AppendResults(
        IReadOnlyList<ModelerResult> results,
        List<Vector2> positions,
        List<float> radii,
        List<float> pressures,
        List<Vector2> tilts)
    {
        foreach (var result in results)
        {
            float pressure = result.Pressure < 0 ? 1f : Mathf.Clamp(result.Pressure, 0f, 1f);
            positions.Add(ToVector2(result.Position));
            pressures.Add(pressure);
            radii.Add(CalculateRadius(pressure));
            tilts.Add(TiltAt(result.Time.TotalSeconds));
        }
    }

    private void ComposeCurrentGeometry()
    {
        _positions.Clear();
        _radii.Clear();
        _pressures.Clear();
        _tilts.Clear();
        _positions.AddRange(_stablePositions);
        _radii.AddRange(_stableRadii);
        _pressures.AddRange(_stablePressures);
        _tilts.AddRange(_stableTilts);
        _positions.AddRange(_predictionPositions);
        _radii.AddRange(_predictionRadii);
        _pressures.AddRange(_predictionPressures);
        _tilts.AddRange(_predictionTilts);
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

    private void AddRawSample(Vector2 position, Vector2 tilt, double time) =>
        _rawSamples.Add(new RawStylusSample(position, tilt, time));

    private Vector2 TiltAt(double time)
    {
        if (_rawSamples.Count == 0) return Vector2.Zero;
        if (time <= _rawSamples[0].Time) return _rawSamples[0].Tilt;

        for (int i = 1; i < _rawSamples.Count; i++)
        {
            var end = _rawSamples[i];
            if (time > end.Time) continue;

            var start = _rawSamples[i - 1];
            if (end.Time == start.Time) return end.Tilt;
            float t = (float)((time - start.Time) / (end.Time - start.Time));
            return start.Tilt.Lerp(end.Tilt, t);
        }

        return _rawSamples[^1].Tilt;
    }

    private static NumericsVector2 ToNumericsVector2(Vector2 value) => new(value.X, value.Y);

    private static Vector2 ToVector2(NumericsVector2 value) => new(value.X, value.Y);

    private readonly record struct RawStylusSample(Vector2 Position, Vector2 Tilt, double Time);
}
