using System;
using System.Collections.Generic;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal sealed class PositionModeler
{
    private PositionModelerParams _params = new();
    private TipState _state;
    private TipState? _savedState;

    public TipState CurrentState => _state;

    public void Reset(TipState state, PositionModelerParams parameters)
    {
        _params = parameters;
        _state = state;
        _savedState = null;
    }

    public TipState Update(Vector2 anchorPosition, TimeSpan time)
    {
        TimeSpan deltaTime = time - _state.Time;
        float seconds = (float)deltaTime.TotalSeconds;
        Vector2 acceleration = SpringAcceleration(_state, anchorPosition, _params);
        Vector2 velocity = _state.Velocity + acceleration * seconds;
        Vector2 position = _state.Position + velocity * seconds;
        _state = new TipState(position, velocity, acceleration, time);
        return _state;
    }

    public void UpdateAlongLinearPath(Vector2 startAnchorPosition, TimeSpan startTime, Vector2 endAnchorPosition, TimeSpan endTime, int nSamples, List<TipState> output)
    {
        for (int i = 1; i <= nSamples; i++)
        {
            float amount = (float)i / nSamples;
            Vector2 position = Utils.Interp(startAnchorPosition, endAnchorPosition, amount);
            TimeSpan time = Utils.Interp(startTime, endTime, amount);
            output.Add(Update(position, time));
        }
    }

    public void ModelEndOfStroke(Vector2 anchorPosition, TimeSpan deltaTime, int maxIterations, float stopDistance, List<TipState> output)
    {
        for (int i = 0; i < maxIterations; i++)
        {
            TipState previousState = _state;
            TipState candidate = Update(anchorPosition, previousState.Time + deltaTime);
            if (Utils.Distance(previousState.Position, candidate.Position) < stopDistance) return;

            float closestT = Utils.NearestPointOnSegment(previousState.Position, candidate.Position, anchorPosition);
            if (closestT < 1)
            {
                deltaTime *= .5;
                _state = previousState;
                continue;
            }

            output.Add(candidate);
            if (Utils.Distance(candidate.Position, anchorPosition) < stopDistance) return;
        }
    }

    public void Save() => _savedState = _state;

    public void Restore()
    {
        if (_savedState.HasValue) _state = _savedState.Value;
    }

    public static int NumberOfStepsBetweenInputs(TipState tipState, ModelerInput start, ModelerInput end, SamplingParams samplingParams, PositionModelerParams positionModelerParams)
    {
        TimeSpan deltaT = end.Time - start.Time;
        float floatDelta = (float)deltaT.TotalSeconds;
        int nSteps = (int)Math.Min(Math.Ceiling(floatDelta * samplingParams.MinOutputRate), int.MaxValue);
        Vector2 estimatedDeltaV = SpringAcceleration(tipState, end.Position, positionModelerParams) * floatDelta;
        Vector2 estimatedEndV = tipState.Velocity + estimatedDeltaV;
        float estimatedAngle;
        try
        {
            estimatedAngle = tipState.Velocity.AbsoluteAngleTo(estimatedEndV);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Non-finite or enormous inputs.", ex);
        }

        if (samplingParams.MaxEstimatedAngleToTraversePerInput > 0)
        {
            int stepsForAngle = (int)Math.Min(
                Math.Ceiling(estimatedAngle / samplingParams.MaxEstimatedAngleToTraversePerInput),
                int.MaxValue);
            if (stepsForAngle > nSteps) nSteps = stepsForAngle;
        }

        return Math.Min(nSteps, samplingParams.MaxOutputsPerCall);
    }

    private static Vector2 SpringAcceleration(TipState tipState, Vector2 anchorPosition, PositionModelerParams parameters) =>
        (anchorPosition - tipState.Position) / parameters.SpringMassConstant - parameters.DragConstant * tipState.Velocity;
}
