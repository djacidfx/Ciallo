using System;
using System.Collections.Generic;
using System.Numerics;
using InkStrokeModeler.Internal.Prediction.KalmanFilter;

namespace InkStrokeModeler.Internal.Prediction;

internal sealed class KalmanPredictor : IInputPredictor
{
    private readonly PredictionParams.Kalman _predictorParams;
    private readonly SamplingParams _samplingParams;
    private readonly AxisPredictor _xPredictor;
    private readonly AxisPredictor _yPredictor;
    private readonly Queue<TimeSpan> _sampleTimes = [];
    private Vector2? _lastPositionReceived;

    public KalmanPredictor(PredictionParams.Kalman predictorParams, SamplingParams samplingParams)
    {
        _predictorParams = predictorParams;
        _samplingParams = samplingParams;
        _xPredictor = new AxisPredictor(predictorParams.ProcessNoise, predictorParams.MeasurementNoise, predictorParams.MinStableIteration);
        _yPredictor = new AxisPredictor(predictorParams.ProcessNoise, predictorParams.MeasurementNoise, predictorParams.MinStableIteration);
    }

    private KalmanPredictor(
        PredictionParams.Kalman predictorParams,
        SamplingParams samplingParams,
        AxisPredictor xPredictor,
        AxisPredictor yPredictor)
    {
        _predictorParams = predictorParams;
        _samplingParams = samplingParams;
        _xPredictor = xPredictor;
        _yPredictor = yPredictor;
    }

    private bool IsStable => _xPredictor.Stable && _yPredictor.Stable;

    public void Reset()
    {
        _xPredictor.Reset();
        _yPredictor.Reset();
        _sampleTimes.Clear();
        _lastPositionReceived = null;
    }

    public void Update(Vector2 position, TimeSpan time)
    {
        _lastPositionReceived = position;
        _sampleTimes.Enqueue(time);
        if (_predictorParams.MaxTimeSamples < 0 || _sampleTimes.Count > _predictorParams.MaxTimeSamples)
            _sampleTimes.Dequeue();

        _xPredictor.Update(position.X);
        _yPredictor.Update(position.Y);
    }

    public State? GetEstimatedState()
    {
        if (!IsStable || _sampleTimes.Count == 0) return null;

        State estimated = new(
            new Vector2((float)_xPredictor.Position, (float)_yPredictor.Position),
            new Vector2((float)_xPredictor.Velocity, (float)_yPredictor.Velocity),
            new Vector2((float)_xPredictor.Acceleration, (float)_yPredictor.Acceleration),
            new Vector2((float)_xPredictor.Jerk, (float)_yPredictor.Jerk));

        TimeSpan first = _sampleTimes.Peek();
        TimeSpan last = first;
        foreach (TimeSpan time in _sampleTimes) last = time;

        float dt = (float)((last - first).TotalSeconds / _sampleTimes.Count);
        float dtSquared = dt * dt;
        float dtCubed = dtSquared * dt;
        estimated = estimated with
        {
            Velocity = estimated.Velocity / dt,
            Acceleration = estimated.Acceleration / dtSquared * _predictorParams.AccelerationWeight,
            Jerk = estimated.Jerk / dtCubed * _predictorParams.JerkWeight,
        };
        return estimated;
    }

    public void ConstructPrediction(TipState lastState, List<TipState> prediction)
    {
        prediction.Clear();
        State? estimated = GetEstimatedState();
        if (!estimated.HasValue || !_lastPositionReceived.HasValue) return;

        TimeSpan sampleDt = TimeSpan.FromSeconds(1.0 / _samplingParams.MinOutputRate);
        ConstructCubicConnector(lastState, estimated.Value, _predictorParams, sampleDt, prediction);
        TimeSpan startTime = prediction.Count == 0 ? lastState.Time : prediction[^1].Time;
        ConstructCubicPrediction(
            estimated.Value,
            startTime,
            sampleDt,
            NumberOfPointsToPredict(estimated.Value),
            prediction);
    }

    public IInputPredictor Clone()
    {
        KalmanPredictor copy = new(_predictorParams, _samplingParams, _xPredictor.Clone(), _yPredictor.Clone());
        foreach (TimeSpan time in _sampleTimes) copy._sampleTimes.Enqueue(time);
        copy._lastPositionReceived = _lastPositionReceived;
        return copy;
    }

    public readonly record struct State(Vector2 Position, Vector2 Velocity, Vector2 Acceleration, Vector2 Jerk);

    private static State EvaluateCubic(State startState, TimeSpan deltaTime)
    {
        float dt = (float)deltaTime.TotalSeconds;
        float dtSquared = dt * dt;
        float dtCubed = dtSquared * dt;
        return new State(
            startState.Position + startState.Velocity * dt + startState.Acceleration * dtSquared / 2f + startState.Jerk * dtCubed / 6f,
            startState.Velocity + startState.Acceleration * dt + startState.Jerk * dtSquared / 2f,
            startState.Acceleration + startState.Jerk * dt,
            startState.Jerk);
    }

    private static void ConstructCubicPrediction(State estimatedState, TimeSpan startTime, TimeSpan sampleDt, int nSamples, List<TipState> output)
    {
        State currentState = estimatedState;
        TimeSpan currentTime = startTime;
        for (int i = 0; i < nSamples; i++)
        {
            State nextState = EvaluateCubic(currentState, sampleDt);
            currentTime += sampleDt;
            output.Add(new TipState(nextState.Position, nextState.Velocity, nextState.Acceleration, currentTime));
            currentState = nextState;
        }
    }

    private static void ConstructCubicConnector(TipState lastTipState, State estimatedState, PredictionParams.Kalman parameters, TimeSpan sampleDt, List<TipState> output)
    {
        float distanceTraveled = Math.Min(Utils.Distance(lastTipState.Position, estimatedState.Position), float.MaxValue);
        float maxVelocityAtEnds = Math.Max(lastTipState.Velocity.Length(), estimatedState.Velocity.Length());
        TimeSpan targetDuration = TimeSpan.FromSeconds(distanceTraveled / Math.Max(maxVelocityAtEnds, parameters.MinCatchupVelocity));

        int nPoints = Math.Max((int)Math.Ceiling(targetDuration.TotalSeconds / sampleDt.TotalSeconds), 1);
        TimeSpan duration = TimeSpan.FromSeconds(nPoints * sampleDt.TotalSeconds);
        float floatDuration = (float)duration.TotalSeconds;

        Vector2 a = 2f * lastTipState.Position - 2f * estimatedState.Position +
                 (lastTipState.Velocity + estimatedState.Velocity) * floatDuration;
        Vector2 b = -3f * lastTipState.Position + 3f * estimatedState.Position -
                 (2f * lastTipState.Velocity + estimatedState.Velocity) * floatDuration;
        Vector2 c = lastTipState.Velocity * floatDuration;
        Vector2 d = lastTipState.Position;

        output.EnsureCapacity(output.Count + nPoints);
        for (int i = 1; i <= nPoints; i++)
        {
            float t = (float)i / nPoints;
            float tSquared = t * t;
            float tCubed = tSquared * t;
            Vector2 position = a * tCubed + b * tSquared + c * t + d;
            Vector2 velocity = 3f * a * tSquared + 2f * b * t + c;
            Vector2 acceleration = 6f * a * t + 2f * b;
            TimeSpan time = lastTipState.Time + TimeSpan.FromSeconds(duration.TotalSeconds * t);
            output.Add(new TipState(position, velocity / floatDuration, acceleration / (floatDuration * floatDuration), time));
        }
    }

    private int NumberOfPointsToPredict(State estimatedState)
    {
        PredictionParams.Kalman.Confidence confidenceParams = _predictorParams.ConfidenceParams;
        float targetNumber = (float)(_predictorParams.PredictionInterval.TotalSeconds * _samplingParams.MinOutputRate);
        float sampleRatio = Math.Min(1f, (float)_xPredictor.IterationCount / confidenceParams.DesiredNumberOfSamples);
        float estimatedError = Utils.Distance(_lastPositionReceived!.Value, estimatedState.Position);
        float normalizedError = 1f - Utils.Normalize01(0f, confidenceParams.MaxEstimationDistance, estimatedError);
        State endState = EvaluateCubic(estimatedState, _predictorParams.PredictionInterval);
        float travelSpeed = Utils.Distance(estimatedState.Position, endState.Position) / (float)_predictorParams.PredictionInterval.TotalSeconds;
        float normalizedDistance = Utils.Normalize01(confidenceParams.MinTravelSpeed, confidenceParams.MaxTravelSpeed, travelSpeed);
        float deviationFromLinearPrediction = Utils.Distance(
            endState.Position,
            estimatedState.Position + (float)_predictorParams.PredictionInterval.TotalSeconds * estimatedState.Velocity);
        float linearity = Utils.Interp(
            confidenceParams.BaselineLinearityConfidence,
            1f,
            1f - Utils.Normalize01(0f, confidenceParams.MaxLinearDeviation, deviationFromLinearPrediction));
        float confidence = sampleRatio * normalizedError * normalizedDistance * linearity;
        return Math.Max(0, (int)Math.Ceiling(targetNumber * confidence));
    }
}
