using System;
using System.Collections.Generic;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal sealed class LoopContractionMitigationModeler
{
    private readonly List<SpeedSample> _speedSamples = [];
    private List<SpeedSample>? _savedSpeedSamples;
    private LoopContractionMitigationParams _params = new();
    private int _firstSpeedSampleIndex;

    private readonly record struct SpeedSample(float Speed, TimeSpan Time);

    public void Reset(LoopContractionMitigationParams parameters)
    {
        _speedSamples.Clear();
        _savedSpeedSamples = null;
        _firstSpeedSampleIndex = 0;
        _params = parameters;
    }

    public LoopContractionMitigationModeler CloneForPrediction()
    {
        LoopContractionMitigationModeler clone = new();
        clone._params = _params;
        clone._speedSamples.EnsureCapacity(Count);
        for (int i = _firstSpeedSampleIndex; i < _speedSamples.Count; i++)
            clone._speedSamples.Add(_speedSamples[i]);
        if (_savedSpeedSamples is not null) clone._savedSpeedSamples = new List<SpeedSample>(_savedSpeedSamples);
        return clone;
    }

    public float GetInterpolationValue()
    {
        if (Count == 0 || !_params.IsEnabled) return 1;

        float speedSum = 0;
        for (int i = _firstSpeedSampleIndex; i < _speedSamples.Count; i++)
            speedSum += _speedSamples[i].Speed;
        float averageSpeed = speedSum / Count;
        float sourceRatio = Utils.Clamp01(Utils.InverseLerp(_params.SpeedLowerBound, _params.SpeedUpperBound, averageSpeed));
        return Utils.Interp(_params.InterpolationStrengthAtSpeedLowerBound, _params.InterpolationStrengthAtSpeedUpperBound, sourceRatio);
    }

    public float Update(Vector2 velocity, TimeSpan time)
    {
        if (!_params.IsEnabled) return 1;

        _speedSamples.Add(new SpeedSample(velocity.Length(), time));
        while (Count > 0 && _speedSamples[^1].Time - _speedSamples[_firstSpeedSampleIndex].Time > _params.MinSpeedSamplingWindow)
            RemoveFirst();

        return GetInterpolationValue();
    }

    public void Save()
    {
        _savedSpeedSamples = new List<SpeedSample>(Count);
        for (int i = _firstSpeedSampleIndex; i < _speedSamples.Count; i++)
            _savedSpeedSamples.Add(_speedSamples[i]);
    }

    public void Restore()
    {
        if (_savedSpeedSamples is null) return;
        _speedSamples.Clear();
        _speedSamples.AddRange(_savedSpeedSamples);
        _firstSpeedSampleIndex = 0;
    }

    private int Count => _speedSamples.Count - _firstSpeedSampleIndex;

    private void RemoveFirst()
    {
        _firstSpeedSampleIndex++;
        if (_firstSpeedSampleIndex <= 32 || _firstSpeedSampleIndex <= _speedSamples.Count / 2) return;

        _speedSamples.RemoveRange(0, _firstSpeedSampleIndex);
        _firstSpeedSampleIndex = 0;
    }
}
