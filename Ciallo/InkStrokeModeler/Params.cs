using System;

namespace InkStrokeModeler;

public sealed record PositionModelerParams
{
    public float SpringMassConstant { get; init; } = 11f / 32400f;
    public float DragConstant { get; init; } = 72f;
    public LoopContractionMitigationParams LoopContractionMitigation { get; init; } = new();
}

public sealed record LoopContractionMitigationParams
{
    public bool IsEnabled { get; init; }
    public float SpeedLowerBound { get; init; } = -1;
    public float SpeedUpperBound { get; init; } = -1;
    public float InterpolationStrengthAtSpeedLowerBound { get; init; } = -1;
    public float InterpolationStrengthAtSpeedUpperBound { get; init; } = -1;
    public TimeSpan MinSpeedSamplingWindow { get; init; } = TimeSpan.FromSeconds(-1);
}

public sealed record SamplingParams
{
    public double MinOutputRate { get; init; } = -1;
    public float EndOfStrokeStoppingDistance { get; init; } = -1;
    public int EndOfStrokeMaxIterations { get; init; } = 20;
    public int MaxOutputsPerCall { get; init; } = 100000;
    public double MaxEstimatedAngleToTraversePerInput { get; init; } = -1;
}

public sealed record StylusStateModelerParams
{
    public bool UseStrokeNormalProjection { get; init; }
    public int MaxInputSamples { get; init; } = 10;
}

public sealed record WobbleSmootherParams
{
    public bool IsEnabled { get; init; } = true;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(-1);
    public float SpeedFloor { get; init; } = -1;
    public float SpeedCeiling { get; init; } = -1;
}

public abstract record PredictionParams
{
    public sealed record StrokeEnd : PredictionParams;
    public sealed record Disabled : PredictionParams;
    public sealed record Kalman : PredictionParams
    {
        public double ProcessNoise { get; init; } = -1;
        public double MeasurementNoise { get; init; } = -1;
        public int MinStableIteration { get; init; } = 4;
        public int MaxTimeSamples { get; init; } = 20;
        public float MinCatchupVelocity { get; init; } = -1;
        public float AccelerationWeight { get; init; } = .5f;
        public float JerkWeight { get; init; } = .1f;
        public TimeSpan PredictionInterval { get; init; } = TimeSpan.FromSeconds(-1);
        public Confidence ConfidenceParams { get; init; } = new();

        public sealed record Confidence
        {
            public int DesiredNumberOfSamples { get; init; } = 20;
            public float MaxEstimationDistance { get; init; } = -1;
            public float MinTravelSpeed { get; init; } = -1;
            public float MaxTravelSpeed { get; init; } = -1;
            public float MaxLinearDeviation { get; init; } = -1;
            public float BaselineLinearityConfidence { get; init; } = .4f;
        }
    }
}

public sealed record ExperimentalParams;

public sealed record StrokeModelParams
{
    public WobbleSmootherParams WobbleSmoother { get; init; } = new();
    public PositionModelerParams PositionModeler { get; init; } = new();
    public SamplingParams Sampling { get; init; } = new();
    public StylusStateModelerParams StylusStateModeler { get; init; } = new();
    public PredictionParams Prediction { get; init; } = new PredictionParams.StrokeEnd();
    public ExperimentalParams Experimental { get; init; } = new();

    public static StrokeModelParams CreateSuggested() => new()
    {
        WobbleSmoother = new()
        {
            Timeout = TimeSpan.FromSeconds(.04),
            SpeedFloor = 1.31f,
            SpeedCeiling = 1.44f,
        },
        PositionModeler = new()
        {
            SpringMassConstant = 11f / 32400f,
            DragConstant = 72f,
            LoopContractionMitigation = new()
            {
                IsEnabled = false,
                SpeedLowerBound = -1,
                SpeedUpperBound = -1,
                InterpolationStrengthAtSpeedLowerBound = -1,
                InterpolationStrengthAtSpeedUpperBound = -1,
                MinSpeedSamplingWindow = TimeSpan.FromSeconds(-1),
            },
        },
        Sampling = new()
        {
            MinOutputRate = 180,
            EndOfStrokeStoppingDistance = .001f,
            EndOfStrokeMaxIterations = 20,
            MaxOutputsPerCall = 20,
        },
        StylusStateModeler = new()
        {
            UseStrokeNormalProjection = false,
            MaxInputSamples = 10,
        },
        Prediction = new PredictionParams.StrokeEnd(),
    };

    public static StrokeModelParams CreateRnoteDefault()
    {
        StrokeModelParams suggested = CreateSuggested();
        return suggested with
        {
            Sampling = suggested.Sampling with
            {
                MinOutputRate = 120,
                EndOfStrokeStoppingDistance = .01f,
                EndOfStrokeMaxIterations = 20,
                MaxOutputsPerCall = 200,
            },
            StylusStateModeler = suggested.StylusStateModeler with
            {
                MaxInputSamples = 20,
            },
        };
    }

    public static StrokeModelParams CreateDefault() => CreateRnoteDefault();

    public void Validate()
    {
        if (PositionModeler.LoopContractionMitigation.IsEnabled &&
            !StylusStateModeler.UseStrokeNormalProjection)
            throw new ArgumentException("UseStrokeNormalProjection must be true when loop contraction mitigation is enabled.");

        ValidateWobble(WobbleSmoother);
        ValidatePosition(PositionModeler);
        ValidateSampling(Sampling);
        ValidateStylus(StylusStateModeler);
        ValidatePrediction(Prediction);
    }

    private static void ValidatePosition(PositionModelerParams p)
    {
        ValidateLoopContraction(p.LoopContractionMitigation);
        GreaterThanZero(p.SpringMassConstant, nameof(PositionModelerParams.SpringMassConstant));
        GreaterThanZero(p.DragConstant, nameof(PositionModelerParams.DragConstant));
    }

    private static void ValidateLoopContraction(LoopContractionMitigationParams p)
    {
        if (!p.IsEnabled) return;
        if (p.SpeedLowerBound > p.SpeedUpperBound || p.SpeedLowerBound < 0)
            throw new ArgumentException("Loop contraction speed bounds are invalid.");
        if (p.InterpolationStrengthAtSpeedLowerBound < p.InterpolationStrengthAtSpeedUpperBound ||
            p.InterpolationStrengthAtSpeedLowerBound > 1 ||
            p.InterpolationStrengthAtSpeedUpperBound < 0)
            throw new ArgumentException("Loop contraction interpolation strengths are invalid.");
        if (p.MinSpeedSamplingWindow < TimeSpan.Zero || p.MinSpeedSamplingWindow > TimeSpan.FromSeconds(10000))
            throw new ArgumentException("Loop contraction speed sampling window is invalid.");
    }

    private static void ValidateSampling(SamplingParams p)
    {
        GreaterThanZero(p.MinOutputRate, nameof(SamplingParams.MinOutputRate));
        GreaterThanZero(p.EndOfStrokeStoppingDistance, nameof(SamplingParams.EndOfStrokeStoppingDistance));
        GreaterThanZero(p.EndOfStrokeMaxIterations, nameof(SamplingParams.EndOfStrokeMaxIterations));
        if (p.EndOfStrokeMaxIterations > 1000) throw new ArgumentException("EndOfStrokeMaxIterations must be at most 1000.");
        GreaterThanZero(p.MaxOutputsPerCall, nameof(SamplingParams.MaxOutputsPerCall));
        if (p.MaxEstimatedAngleToTraversePerInput != -1)
        {
            GreaterThanZero(p.MaxEstimatedAngleToTraversePerInput, nameof(SamplingParams.MaxEstimatedAngleToTraversePerInput));
            if (p.MaxEstimatedAngleToTraversePerInput >= Math.PI)
                throw new ArgumentException("MaxEstimatedAngleToTraversePerInput must be less than PI.");
        }
    }

    private static void ValidateStylus(StylusStateModelerParams p) =>
        GreaterThanZero(p.MaxInputSamples, nameof(StylusStateModelerParams.MaxInputSamples));

    private static void ValidateWobble(WobbleSmootherParams p)
    {
        if (!p.IsEnabled) return;
        GreaterThanOrEqualToZero(p.Timeout.TotalSeconds, nameof(WobbleSmootherParams.Timeout));
        GreaterThanOrEqualToZero(p.SpeedFloor, nameof(WobbleSmootherParams.SpeedFloor));
        Finite(p.SpeedCeiling, nameof(WobbleSmootherParams.SpeedCeiling));
        if (p.SpeedCeiling < p.SpeedFloor) throw new ArgumentException("SpeedCeiling must be greater than or equal to SpeedFloor.");
    }

    private static void ValidatePrediction(PredictionParams p)
    {
        if (p is not PredictionParams.Kalman k) return;
        GreaterThanZero(k.ProcessNoise, nameof(PredictionParams.Kalman.ProcessNoise));
        GreaterThanZero(k.MeasurementNoise, nameof(PredictionParams.Kalman.MeasurementNoise));
        GreaterThanZero(k.MinStableIteration, nameof(PredictionParams.Kalman.MinStableIteration));
        GreaterThanZero(k.MaxTimeSamples, nameof(PredictionParams.Kalman.MaxTimeSamples));
        GreaterThanZero(k.MinCatchupVelocity, nameof(PredictionParams.Kalman.MinCatchupVelocity));
        Finite(k.AccelerationWeight, nameof(PredictionParams.Kalman.AccelerationWeight));
        Finite(k.JerkWeight, nameof(PredictionParams.Kalman.JerkWeight));
        GreaterThanZero(k.PredictionInterval.TotalSeconds, nameof(PredictionParams.Kalman.PredictionInterval));
        GreaterThanZero(k.ConfidenceParams.DesiredNumberOfSamples, nameof(PredictionParams.Kalman.Confidence.DesiredNumberOfSamples));
        GreaterThanZero(k.ConfidenceParams.MaxEstimationDistance, nameof(PredictionParams.Kalman.Confidence.MaxEstimationDistance));
        GreaterThanOrEqualToZero(k.ConfidenceParams.MinTravelSpeed, nameof(PredictionParams.Kalman.Confidence.MinTravelSpeed));
        Finite(k.ConfidenceParams.MaxTravelSpeed, nameof(PredictionParams.Kalman.Confidence.MaxTravelSpeed));
        if (k.ConfidenceParams.MaxTravelSpeed < k.ConfidenceParams.MinTravelSpeed)
            throw new ArgumentException("MaxTravelSpeed must be greater than or equal to MinTravelSpeed.");
        GreaterThanZero(k.ConfidenceParams.MaxLinearDeviation, nameof(PredictionParams.Kalman.Confidence.MaxLinearDeviation));
        if (k.ConfidenceParams.BaselineLinearityConfidence < 0 || k.ConfidenceParams.BaselineLinearityConfidence > 1)
            throw new ArgumentException("BaselineLinearityConfidence must be in [0, 1].");
    }

    private static void GreaterThanZero(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0) throw new ArgumentException($"{name} must be greater than zero.");
    }

    private static void GreaterThanOrEqualToZero(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentException($"{name} must be greater than or equal to zero.");
    }

    private static void Finite(double value, string name)
    {
        if (!double.IsFinite(value)) throw new ArgumentException($"{name} must be finite.");
    }
}
