using System.Collections.Generic;
using System;
using System.Numerics;

namespace InkStrokeModeler.Internal.Prediction;

internal interface IInputPredictor
{
    void Reset();
    void Update(Vector2 position, TimeSpan time);
    void ConstructPrediction(TipState lastState, List<TipState> prediction);
    IInputPredictor Clone();
}
