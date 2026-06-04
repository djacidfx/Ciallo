using System;
using System.Collections.Generic;
using System.Numerics;
using InkStrokeModeler.Internal;
using InkStrokeModeler.Internal.Prediction;

namespace InkStrokeModeler;

public sealed class StrokeModeler
{
    private readonly WobbleSmoother _wobbleSmoother = new();
    private readonly PositionModeler _positionModeler = new();
    private readonly StylusStateModeler _stylusStateModeler = new();
    private readonly LoopContractionMitigationModeler _loopContractionMitigationModeler = new();
    private readonly List<TipState> _tipStateBuffer = [];

    private StrokeModelParams? _params;
    private IInputPredictor? _predictor;
    private InputAndCorrectedPosition? _lastInput;
    private IInputPredictor? _savedPredictor;
    private InputAndCorrectedPosition? _savedLastInput;
    private bool _saveActive;

    public void Reset(StrokeModelParams parameters)
    {
        parameters.Validate();
        _params = parameters;
        ResetInternal();

        _predictor = parameters.Prediction switch
        {
            PredictionParams.Kalman kalman => new KalmanPredictor(kalman, parameters.Sampling),
            PredictionParams.StrokeEnd => new StrokeEndPredictor(parameters.PositionModeler, parameters.Sampling),
            PredictionParams.Disabled => null,
            _ => throw new InvalidOperationException($"Unsupported prediction params: {parameters.Prediction.GetType().Name}"),
        };
        _loopContractionMitigationModeler.Reset(parameters.PositionModeler.LoopContractionMitigation);
    }

    public void Reset()
    {
        if (_params is null) throw new InvalidOperationException("Initial call to Reset must pass StrokeModelParams.");
        ResetInternal();
    }

    public void Update(ModelerInput input, List<ModelerResult> results)
    {
        if (_params is null) throw new InvalidOperationException("Stroke model has not yet been initialized.");
        ModelerInputValidation.Validate(input);

        if (_lastInput.HasValue)
        {
            if (_lastInput.Value.Input == input) throw new ArgumentException("Received duplicate input.");
            if (input.Time < _lastInput.Value.Input.Time) throw new ArgumentException("Inputs travel backwards in time.");
        }

        switch (input.EventType)
        {
            case InputEventType.Down:
                ProcessDown(input, results);
                break;
            case InputEventType.Move:
                ProcessMove(input, results);
                break;
            case InputEventType.Up:
                ProcessUp(input, results);
                break;
            default:
                throw new ArgumentException("Invalid input event type.");
        }
    }

    public List<ModelerResult> Update(ModelerInput input)
    {
        List<ModelerResult> results = [];
        Update(input, results);
        return results;
    }

    public void Predict(List<ModelerResult> results)
    {
        results.Clear();
        if (_params is null) throw new InvalidOperationException("Stroke model has not yet been initialized.");
        if (_predictor is null) throw new InvalidOperationException("Prediction has been disabled by StrokeModelParams.");
        if (!_lastInput.HasValue) throw new InvalidOperationException("Cannot construct prediction when no stroke is in-progress.");

        _predictor.ConstructPrediction(_positionModeler.CurrentState, _tipStateBuffer);
        StylusStateModeler predictionStylusStateModeler = _stylusStateModeler.CloneForPrediction();
        LoopContractionMitigationModeler predictionLoopModeler = _loopContractionMitigationModeler.CloneForPrediction();
        ModelStylus(_tipStateBuffer, predictionStylusStateModeler, predictionLoopModeler, results, _lastInput.Value.Input.Time);
    }

    public List<ModelerResult> Predict()
    {
        List<ModelerResult> results = [];
        Predict(results);
        return results;
    }

    public void Save()
    {
        _wobbleSmoother.Save();
        _positionModeler.Save();
        _stylusStateModeler.Save();
        _loopContractionMitigationModeler.Save();
        _savedLastInput = _lastInput;
        if (_predictor is not null) _savedPredictor = _predictor.Clone();
        _saveActive = true;
    }

    public void Restore()
    {
        if (!_saveActive) return;

        _wobbleSmoother.Restore();
        _positionModeler.Restore();
        _stylusStateModeler.Restore();
        _loopContractionMitigationModeler.Restore();
        _lastInput = _savedLastInput;
        if (_savedPredictor is not null) _predictor = _savedPredictor.Clone();
    }

    private void ResetInternal()
    {
        _lastInput = null;
        _saveActive = false;
    }

    private void ProcessDown(ModelerInput input, List<ModelerResult> results)
    {
        if (_lastInput.HasValue) throw new InvalidOperationException("Received down event while stroke is in-progress.");

        StrokeModelParams parameters = _params!;
        _wobbleSmoother.Reset(parameters.WobbleSmoother, input.Position, input.Time);
        _positionModeler.Reset(new TipState(input.Position, Vector2.Zero, Vector2.Zero, input.Time), parameters.PositionModeler);
        _stylusStateModeler.Reset(parameters.StylusStateModeler);
        _loopContractionMitigationModeler.Reset(parameters.PositionModeler.LoopContractionMitigation);
        _stylusStateModeler.Update(input.Position, input.Time, new StylusState(input.Pressure, input.Tilt, input.Orientation));

        TipState tipState = _positionModeler.CurrentState;
        _predictor?.Reset();
        _predictor?.Update(input.Position, input.Time);
        _lastInput = new InputAndCorrectedPosition(input, input.Position);
        results.Add(new ModelerResult(tipState.Position, tipState.Velocity, tipState.Acceleration, tipState.Time, input.Pressure, input.Tilt, input.Orientation));
    }

    private void ProcessMove(ModelerInput input, List<ModelerResult> results)
    {
        if (!_lastInput.HasValue) throw new InvalidOperationException("Received move event while no stroke is in-progress.");

        StrokeModelParams parameters = _params!;
        Vector2 correctedPosition = _wobbleSmoother.Update(input.Position, input.Time);
        _stylusStateModeler.Update(correctedPosition, input.Time, new StylusState(input.Pressure, input.Tilt, input.Orientation));
        int nSteps = PositionModeler.NumberOfStepsBetweenInputs(
            _positionModeler.CurrentState,
            _lastInput.Value.Input,
            input,
            parameters.Sampling,
            parameters.PositionModeler);

        _tipStateBuffer.Clear();
        _tipStateBuffer.EnsureCapacity(nSteps);
        _positionModeler.UpdateAlongLinearPath(
            _lastInput.Value.CorrectedPosition,
            _lastInput.Value.Input.Time,
            correctedPosition,
            input.Time,
            nSteps,
            _tipStateBuffer);

        _predictor?.Update(correctedPosition, input.Time);
        _lastInput = new InputAndCorrectedPosition(input, correctedPosition);
        ModelStylus(_tipStateBuffer, _stylusStateModeler, _loopContractionMitigationModeler, results, _lastInput.Value.Input.Time);
    }

    private void ProcessUp(ModelerInput input, List<ModelerResult> results)
    {
        if (!_lastInput.HasValue) throw new InvalidOperationException("Received up event while no stroke is in-progress.");

        StrokeModelParams parameters = _params!;
        int nSteps = PositionModeler.NumberOfStepsBetweenInputs(
            _positionModeler.CurrentState,
            _lastInput.Value.Input,
            input,
            parameters.Sampling,
            parameters.PositionModeler);

        _tipStateBuffer.Clear();
        _tipStateBuffer.EnsureCapacity(nSteps + parameters.Sampling.EndOfStrokeMaxIterations);
        _positionModeler.UpdateAlongLinearPath(
            _lastInput.Value.CorrectedPosition,
            _lastInput.Value.Input.Time,
            input.Position,
            input.Time,
            nSteps,
            _tipStateBuffer);
        _positionModeler.ModelEndOfStroke(
            input.Position,
            TimeSpan.FromSeconds(1.0 / parameters.Sampling.MinOutputRate),
            parameters.Sampling.EndOfStrokeMaxIterations,
            parameters.Sampling.EndOfStrokeStoppingDistance,
            _tipStateBuffer);

        if (_tipStateBuffer.Count == 0) _tipStateBuffer.Add(_positionModeler.CurrentState);

        _stylusStateModeler.Update(input.Position, input.Time, new StylusState(input.Pressure, input.Tilt, input.Orientation));
        ModelStylus(_tipStateBuffer, _stylusStateModeler, _loopContractionMitigationModeler, results, _lastInput.Value.Input.Time);
        _lastInput = null;
    }

    private static void ModelStylus(
        IReadOnlyList<TipState> tipStates,
        StylusStateModeler stylusStateModeler,
        LoopContractionMitigationModeler loopContractionMitigationModeler,
        List<ModelerResult> results,
        TimeSpan prevTime)
    {
        results.EnsureCapacity(results.Count + tipStates.Count);
        float interpolationValue = loopContractionMitigationModeler.GetInterpolationValue();
        foreach (TipState tipState in tipStates)
        {
            Vector2? strokeNormal = Utils.GetStrokeNormal(tipState, prevTime);
            ModelerResult projectedState = stylusStateModeler.Project(tipState, strokeNormal);
            ModelerResult modeledState = new(
                tipState.Position,
                tipState.Velocity,
                tipState.Acceleration,
                tipState.Time,
                projectedState.Pressure,
                projectedState.Tilt,
                projectedState.Orientation);
            results.Add(Utils.InterpResult(projectedState, modeledState, interpolationValue));
            interpolationValue = loopContractionMitigationModeler.Update(results[^1].Velocity, tipState.Time);
            prevTime = tipState.Time;
        }
    }

    private readonly record struct InputAndCorrectedPosition(ModelerInput Input, Vector2 CorrectedPosition);
}
