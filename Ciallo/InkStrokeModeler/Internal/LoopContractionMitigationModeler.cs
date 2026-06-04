using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal sealed class LoopContractionMitigationModeler
{
    private readonly LinkedList<SpeedSample> _speedSamples = [];
    private List<SpeedSample>? _savedSpeedSamples;
    private LoopContractionMitigationParams _params = new();

    private readonly record struct SpeedSample(float Speed, TimeSpan Time);

    public void Reset(LoopContractionMitigationParams parameters)
    {
        _speedSamples.Clear();
        _savedSpeedSamples = null;
        _params = parameters;
    }

    public LoopContractionMitigationModeler CloneForPrediction()
    {
        LoopContractionMitigationModeler clone = new();
        clone._params = _params;
        foreach (SpeedSample sample in _speedSamples) clone._speedSamples.AddLast(sample);
        if (_savedSpeedSamples is not null) clone._savedSpeedSamples = [.. _savedSpeedSamples];
        return clone;
    }

    public float GetInterpolationValue()
    {
        if (_speedSamples.Count == 0 || !_params.IsEnabled) return 1;

        float averageSpeed = _speedSamples.Sum(sample => sample.Speed) / _speedSamples.Count;
        float sourceRatio = Utils.Clamp01(Utils.InverseLerp(_params.SpeedLowerBound, _params.SpeedUpperBound, averageSpeed));
        return Utils.Interp(_params.InterpolationStrengthAtSpeedLowerBound, _params.InterpolationStrengthAtSpeedUpperBound, sourceRatio);
    }

    public float Update(Vector2 velocity, TimeSpan time)
    {
        if (!_params.IsEnabled) return 1;

        _speedSamples.AddLast(new SpeedSample(velocity.Length(), time));
        while (_speedSamples.Count > 0 && _speedSamples.Last!.Value.Time - _speedSamples.First!.Value.Time > _params.MinSpeedSamplingWindow)
            _speedSamples.RemoveFirst();

        return GetInterpolationValue();
    }

    public void Save() => _savedSpeedSamples = [.. _speedSamples];

    public void Restore()
    {
        if (_savedSpeedSamples is null) return;
        _speedSamples.Clear();
        foreach (SpeedSample sample in _savedSpeedSamples) _speedSamples.AddLast(sample);
    }
}
