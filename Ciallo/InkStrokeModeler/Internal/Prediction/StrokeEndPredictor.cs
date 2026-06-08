using System;
using System.Collections.Generic;
using System.Numerics;

namespace InkStrokeModeler.Internal.Prediction;

internal sealed class StrokeEndPredictor(PositionModelerParams positionModelerParams, SamplingParams samplingParams) : IInputPredictor
{
    private Vector2? _lastPosition;

    public void Reset() => _lastPosition = null;

    public void Update(Vector2 position, TimeSpan time) => _lastPosition = position;

    public void ConstructPrediction(TipState lastState, List<TipState> prediction)
    {
        prediction.Clear();
        if (!_lastPosition.HasValue) return;

        prediction.EnsureCapacity(samplingParams.EndOfStrokeMaxIterations);
        PositionModeler modeler = new();
        modeler.Reset(lastState, positionModelerParams);
        modeler.ModelEndOfStroke(
            _lastPosition.Value,
            TimeSpan.FromSeconds(1.0 / samplingParams.MinOutputRate),
            samplingParams.EndOfStrokeMaxIterations,
            samplingParams.EndOfStrokeStoppingDistance,
            prediction);
    }

    public IInputPredictor Clone() => new StrokeEndPredictor(positionModelerParams, samplingParams)
    {
        _lastPosition = _lastPosition,
    };
}
