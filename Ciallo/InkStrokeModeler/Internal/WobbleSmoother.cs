using System;
using System.Collections.Generic;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal sealed class WobbleSmoother
{
    private readonly record struct Sample(Vector2 Position, Vector2 WeightedPosition, float Distance, TimeSpan Duration, TimeSpan Time);

    private sealed class State
    {
        public readonly List<Sample> Samples = [];
        public int FirstSampleIndex;
        public Vector2 WeightedPositionSum;
        public float DistanceSum;
        public float DurationSum;

        public State Clone()
        {
            State clone = new()
            {
                FirstSampleIndex = 0,
                WeightedPositionSum = WeightedPositionSum,
                DistanceSum = DistanceSum,
                DurationSum = DurationSum,
            };
            clone.Samples.EnsureCapacity(Samples.Count - FirstSampleIndex);
            for (int i = FirstSampleIndex; i < Samples.Count; i++)
                clone.Samples.Add(Samples[i]);
            return clone;
        }

        public Sample First => Samples[FirstSampleIndex];

        public Sample Last => Samples[^1];

        public void Add(Sample sample) => Samples.Add(sample);

        public void RemoveFirst()
        {
            FirstSampleIndex++;
            if (FirstSampleIndex <= 32 || FirstSampleIndex <= Samples.Count / 2) return;

            Samples.RemoveRange(0, FirstSampleIndex);
            FirstSampleIndex = 0;
        }
    }

    private State _state = new();
    private State? _savedState;
    private WobbleSmootherParams _params = new();

    public void Reset(WobbleSmootherParams parameters, Vector2 position, TimeSpan time)
    {
        _state = new State();
        _state.Add(new Sample(position, Vector2.Zero, 0, TimeSpan.Zero, time));
        _savedState = null;
        _params = parameters;
    }

    public Vector2 Update(Vector2 position, TimeSpan time)
    {
        if (!_params.IsEnabled) return position;

        Sample last = _state.Last;
        TimeSpan deltaTime = time - last.Time;
        float deltaSeconds = (float)deltaTime.TotalSeconds;
        Sample sample = new(
            position,
            position * deltaSeconds,
            Utils.Distance(position, last.Position),
            deltaTime,
            time);
        _state.Add(sample);
        _state.WeightedPositionSum += sample.WeightedPosition;
        _state.DistanceSum += sample.Distance;
        _state.DurationSum += (float)sample.Duration.TotalSeconds;

        while (_state.First.Time < time - _params.Timeout)
        {
            Sample removed = _state.First;
            _state.WeightedPositionSum -= removed.WeightedPosition;
            _state.DistanceSum -= removed.Distance;
            _state.DurationSum -= (float)removed.Duration.TotalSeconds;
            _state.RemoveFirst();
        }

        if (_state.DurationSum == 0) return position;

        Vector2 averagePosition = _state.WeightedPositionSum / _state.DurationSum;
        float averageSpeed = _state.DistanceSum / _state.DurationSum;
        return Utils.Interp(averagePosition, position, Utils.Normalize01(_params.SpeedFloor, _params.SpeedCeiling, averageSpeed));
    }

    public void Save() => _savedState = _state.Clone();

    public void Restore()
    {
        if (_savedState is not null) _state = _savedState.Clone();
    }
}
