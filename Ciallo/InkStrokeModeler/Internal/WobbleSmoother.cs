using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal sealed class WobbleSmoother
{
    private sealed record Sample(Vector2 Position, Vector2 WeightedPosition, float Distance, TimeSpan Duration, TimeSpan Time);

    private sealed class State
    {
        public readonly LinkedList<Sample> Samples = [];
        public Vector2 WeightedPositionSum;
        public float DistanceSum;
        public float DurationSum;

        public State Clone()
        {
            State clone = new()
            {
                WeightedPositionSum = WeightedPositionSum,
                DistanceSum = DistanceSum,
                DurationSum = DurationSum,
            };
            foreach (Sample sample in Samples) clone.Samples.AddLast(sample);
            return clone;
        }
    }

    private State _state = new();
    private State? _savedState;
    private WobbleSmootherParams _params = new();

    public void Reset(WobbleSmootherParams parameters, Vector2 position, TimeSpan time)
    {
        _state = new State();
        _state.Samples.AddLast(new Sample(position, Vector2.Zero, 0, TimeSpan.Zero, time));
        _savedState = null;
        _params = parameters;
    }

    public Vector2 Update(Vector2 position, TimeSpan time)
    {
        if (!_params.IsEnabled) return position;

        Sample last = _state.Samples.Last!.Value;
        TimeSpan deltaTime = time - last.Time;
        float deltaSeconds = (float)deltaTime.TotalSeconds;
        Sample sample = new(
            position,
            position * deltaSeconds,
            Utils.Distance(position, last.Position),
            deltaTime,
            time);
        _state.Samples.AddLast(sample);
        _state.WeightedPositionSum += sample.WeightedPosition;
        _state.DistanceSum += sample.Distance;
        _state.DurationSum += (float)sample.Duration.TotalSeconds;

        while (_state.Samples.First!.Value.Time < time - _params.Timeout)
        {
            Sample removed = _state.Samples.First.Value;
            _state.WeightedPositionSum -= removed.WeightedPosition;
            _state.DistanceSum -= removed.Distance;
            _state.DurationSum -= (float)removed.Duration.TotalSeconds;
            _state.Samples.RemoveFirst();
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
