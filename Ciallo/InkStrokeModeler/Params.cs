using System;

namespace InkStrokeModeler;

/// <summary>
/// Controls the spring-damper model that turns raw input positions into modeled tip positions.
/// Start from the default values, then tune this group only when the stroke feels too sluggish,
/// too jittery, or too prone to overshoot.
/// </summary>
public sealed record PositionModelerParams
{
    /// <summary>
    /// Spring mass constant. Smaller values pull the modeled tip toward raw input faster,
    /// which feels more responsive but keeps more jitter. Larger values add inertia,
    /// which smooths the stroke but increases lag and can make the tip feel heavy.
    /// </summary>
    public float SpringMassConstant { get; init; } = 11f / 32400f;

    /// <summary>
    /// Drag constant. Larger values damp velocity more strongly and reduce overshoot,
    /// but can make the stroke feel dull. Smaller values preserve motion and liveliness,
    /// but make overshoot easier to produce.
    /// </summary>
    public float DragConstant { get; init; } = 72f;

    /// <summary>
    /// Optional mitigation for loop contraction, where the spring model narrows small loops
    /// or sharp turns. Enable it only when that artifact is visible.
    /// </summary>
    public LoopContractionMitigationParams LoopContractionMitigation { get; init; } = new();
}

/// <summary>
/// Pulls the output back toward the raw input polyline around tight curves or small loops.
/// The mitigation strength is selected from recent average speed.
/// </summary>
public sealed record LoopContractionMitigationParams
{
    /// <summary>
    /// Enables loop contraction mitigation. This also requires
    /// <see cref="StylusStateModelerParams.UseStrokeNormalProjection"/>.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Speed where the low-speed interpolation strength starts applying.
    /// Units are input distance units per second, for example pixels/second in Ciallo.
    /// </summary>
    public float SpeedLowerBound { get; init; } = -1;

    /// <summary>
    /// Speed where the high-speed interpolation strength applies.
    /// Must be greater than or equal to <see cref="SpeedLowerBound"/>.
    /// </summary>
    public float SpeedUpperBound { get; init; } = -1;

    /// <summary>
    /// Spring-model weight at <see cref="SpeedLowerBound"/>.
    /// 1 means fully use the smoothed spring result; 0 means fully use the raw-input projection.
    /// Lower values reduce contraction in slow, tight loops.
    /// </summary>
    public float InterpolationStrengthAtSpeedLowerBound { get; init; } = -1;

    /// <summary>
    /// Spring-model weight at <see cref="SpeedUpperBound"/>.
    /// This is usually not higher than the low-speed weight. Keeping more spring result at
    /// higher speeds avoids feeding raw input noise directly into the output.
    /// </summary>
    public float InterpolationStrengthAtSpeedUpperBound { get; init; } = -1;

    /// <summary>
    /// Time window used to compute recent average speed. Longer windows make mitigation
    /// strength more stable but slower to react; shorter windows react quickly but are noisier.
    /// </summary>
    public TimeSpan MinSpeedSamplingWindow { get; init; } = TimeSpan.FromSeconds(-1);
}

/// <summary>
/// Controls output sample density and how the modeled tip catches up after pen-up.
/// </summary>
public sealed record SamplingParams
{
    /// <summary>
    /// Minimum modeled output samples per second. When input arrives more slowly than this,
    /// the model inserts intermediate samples. Higher values make previews and stored strokes
    /// smoother, but produce more points per update.
    /// </summary>
    public double MinOutputRate { get; init; } = -1;

    /// <summary>
    /// Stop distance for end-of-stroke catch-up. Smaller values make the final tip position
    /// closer to the raw endpoint but can add more tail samples. Larger values stop sooner,
    /// but may leave the modeled endpoint slightly short.
    /// </summary>
    public float EndOfStrokeStoppingDistance { get; init; } = -1;

    /// <summary>
    /// Maximum number of end-of-stroke catch-up iterations.
    /// This caps output growth when parameters or input timing are unusual.
    /// </summary>
    public int EndOfStrokeMaxIterations { get; init; } = 20;

    /// <summary>
    /// Maximum output samples allowed from one Update or Predict call.
    /// This protects callers from large bursts after a pause or a very large input time gap.
    /// </summary>
    public int MaxOutputsPerCall { get; init; } = 100000;

    /// <summary>
    /// Maximum estimated angle, in radians, that one interpolation step may traverse.
    /// -1 disables angle-based extra sampling. Smaller positive values add more samples
    /// around sharp turns; values that are too small can greatly increase output count.
    /// </summary>
    public double MaxEstimatedAngleToTraversePerInput { get; init; } = -1;
}

/// <summary>
/// Controls modeling of non-position stylus state such as pressure, tilt, and orientation.
/// Position is modeled first; stylus state is then interpolated from recent raw-input samples.
/// </summary>
public sealed record StylusStateModelerParams
{
    /// <summary>
    /// Uses stroke-normal projection to find the raw input sample corresponding to a modeled tip.
    /// This is often more accurate around sharp turns and is required by loop contraction mitigation.
    /// When false, the model uses closest-point projection.
    /// </summary>
    public bool UseStrokeNormalProjection { get; init; }

    /// <summary>
    /// Number of recent raw input samples retained for pressure, tilt, and orientation interpolation.
    /// Larger values give projection more history but cost more to search. Values that are too small
    /// can make stylus state jump during fast strokes or low-frequency input.
    /// </summary>
    public int MaxInputSamples { get; init; } = 10;
}

/// <summary>
/// Smooths raw input positions before they enter the spring position model.
/// It averages low-speed input more strongly and fades out as speed increases.
/// </summary>
public sealed record WobbleSmootherParams
{
    /// <summary>
    /// Enables raw input wobble smoothing. Disabling it reduces latency but preserves more hand jitter.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Moving-average time window for wobble smoothing. Longer windows stabilize slow strokes but
    /// react more slowly; shorter windows reduce lag but smooth less.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(-1);

    /// <summary>
    /// Speed where wobble smoothing is strongest. Below this speed the output leans toward
    /// the averaged position.
    /// </summary>
    public float SpeedFloor { get; init; } = -1;

    /// <summary>
    /// Speed where wobble smoothing is effectively disabled. Above this speed the output leans
    /// toward the current input to avoid visible drag on fast strokes.
    /// </summary>
    public float SpeedCeiling { get; init; } = -1;
}

/// <summary>
/// Selects the stroke prediction strategy. Prediction only affects transient in-progress stroke preview;
/// committed geometry should come from output produced after End.
/// </summary>
public abstract record PredictionParams
{
    /// <summary>
    /// Treats the last raw input as the expected stroke endpoint and lets the modeled tip catch up.
    /// This is conservative and is the default preview behavior.
    /// </summary>
    public sealed record StrokeEnd : PredictionParams;

    /// <summary>
    /// Disables prediction. Calling Predict with this setting throws.
    /// </summary>
    public sealed record Disabled : PredictionParams;

    /// <summary>
    /// Uses a Kalman filter to estimate future motion beyond the latest input sample.
    /// This can produce a longer predictive preview, but it needs device- and feel-specific tuning.
    /// </summary>
    public sealed record Kalman : PredictionParams
    {
        /// <summary>
        /// Estimated variance of real stroke motion. Higher values let the predictor adapt faster
        /// to changing movement; lower values make prediction steadier but slower to follow turns.
        /// </summary>
        public double ProcessNoise { get; init; } = -1;

        /// <summary>
        /// Estimated variance of input measurement error. Higher values distrust raw samples and
        /// smooth prediction more; lower values follow input more closely but amplify noise.
        /// </summary>
        public double MeasurementNoise { get; init; } = -1;

        /// <summary>
        /// Minimum number of input updates before the Kalman state is considered stable.
        /// Larger values delay prediction startup but make early prediction less jumpy.
        /// </summary>
        public int MinStableIteration { get; init; } = 4;

        /// <summary>
        /// Number of recent timestamps used to estimate the real input interval.
        /// Larger values handle irregular input timing more smoothly; smaller values react faster
        /// to changes in input frequency.
        /// </summary>
        public int MaxTimeSamples { get; init; } = 20;

        /// <summary>
        /// Minimum velocity used for the catch-up segment that connects the current modeled tip
        /// to the estimated Kalman state. Too low can create a long catch-up tail; too high can jump.
        /// </summary>
        public float MinCatchupVelocity { get; init; } = -1;

        /// <summary>
        /// Weight applied to the acceleration term in cubic future prediction.
        /// Lower values make prediction closer to a straight line; higher values preserve more curve trend.
        /// </summary>
        public float AccelerationWeight { get; init; } = .5f;

        /// <summary>
        /// Weight applied to the jerk term in cubic future prediction.
        /// Lower values are more conservative; higher values react to curvature changes but can destabilize preview.
        /// </summary>
        public float JerkWeight { get; init; } = .1f;

        /// <summary>
        /// Maximum time horizon for future prediction. The final number of predicted points is also
        /// multiplied by confidence, so unstable input shortens the actual preview.
        /// </summary>
        public TimeSpan PredictionInterval { get; init; } = TimeSpan.FromSeconds(-1);

        /// <summary>
        /// Confidence parameters for Kalman prediction. Overall confidence is a product of several
        /// heuristics, so any weak dimension can shorten the predicted preview.
        /// </summary>
        public Confidence ConfidenceParams { get; init; } = new();

        /// <summary>
        /// Heuristic confidence controls for Kalman prediction. The goal is not to always predict farther;
        /// it is to predict farther only when the input is stable, fast enough, and plausibly linear.
        /// </summary>
        public sealed record Confidence
        {
            /// <summary>
            /// Number of input samples needed to reach full sample-count confidence.
            /// Larger values make prediction length ramp up more slowly.
            /// </summary>
            public int DesiredNumberOfSamples { get; init; } = 20;

            /// <summary>
            /// Maximum allowed distance between the latest input point and the estimated position.
            /// Beyond this distance, estimation confidence goes to zero.
            /// </summary>
            public float MaxEstimationDistance { get; init; } = -1;

            /// <summary>
            /// Travel speed below which speed confidence approaches zero.
            /// This prevents extension when the stylus is nearly stationary.
            /// </summary>
            public float MinTravelSpeed { get; init; } = -1;

            /// <summary>
            /// Travel speed at which speed confidence approaches one.
            /// Must be greater than or equal to <see cref="MinTravelSpeed"/>.
            /// </summary>
            public float MaxTravelSpeed { get; init; } = -1;

            /// <summary>
            /// Maximum deviation between the cubic predicted endpoint and the linear predicted endpoint.
            /// Smaller values force prediction closer to a straight line; larger values allow curvier prediction.
            /// </summary>
            public float MaxLinearDeviation { get; init; } = -1;

            /// <summary>
            /// Minimum linearity confidence kept at <see cref="MaxLinearDeviation"/>.
            /// 0 strictly suppresses nonlinear prediction; higher values preserve more curved prediction.
            /// </summary>
            public float BaselineLinearityConfidence { get; init; } = .4f;
        }
    }
}

/// <summary>
/// Reserved for experimental modeler settings. There are no active options yet.
/// </summary>
public sealed record ExperimentalParams;

/// <summary>
/// Complete parameter set for StrokeModeler.
/// Tune from <see cref="CreateCialloDefault"/>. Confirm the input distance and time units first, then change
/// one group at a time. In Ciallo, distance-related speeds are Godot world units per second.
/// A practical order is: tune WobbleSmoother for low-speed jitter, tune PositionModeler for
/// responsiveness versus smoothness, then tune Prediction only if preview behavior still needs work.
/// </summary>
public sealed record StrokeModelParams
{
    /// <summary>
    /// Raw input wobble smoothing. Mainly affects low-speed jitter and drag during fast movement.
    /// </summary>
    public WobbleSmootherParams WobbleSmoother { get; init; } = new();

    /// <summary>
    /// Spring-damper tip position model. Mainly affects responsiveness, smoothness, and overshoot.
    /// </summary>
    public PositionModelerParams PositionModeler { get; init; } = new();

    /// <summary>
    /// Output sampling and end-of-stroke catch-up. Mainly affects point density, tail completion,
    /// and per-call output limits.
    /// </summary>
    public SamplingParams Sampling { get; init; } = new();

    /// <summary>
    /// Interpolation strategy for pressure, tilt, orientation, and other non-position stylus state.
    /// </summary>
    public StylusStateModelerParams StylusStateModeler { get; init; } = new();

    /// <summary>
    /// Stroke prediction strategy. This is only for in-progress stroke preview.
    /// </summary>
    public PredictionParams Prediction { get; init; } = new PredictionParams.StrokeEnd();

    /// <summary>
    /// Reserved experimental settings.
    /// </summary>
    public ExperimentalParams Experimental { get; init; } = new();

    /// <summary>
    /// Suggested general-purpose settings adapted from google/ink-stroke-modeler.
    /// </summary>
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

    /// <summary>
    /// Rnote-style defaults: lower output rate than suggested settings, looser endpoint distance,
    /// and a larger per-call output budget. Ciallo currently uses this as its default preset.
    /// </summary>
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

    /// <summary>
    /// Default parameter entry point for Ciallo. Ciallo uses disabled modeler prediction
    /// and adds the latest raw input as a preview-only endpoint in PolylineInteractiveGenerator.
    /// </summary>
    public static StrokeModelParams CreateCialloDefault() => new()
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
        Prediction = new PredictionParams.Disabled(),
    };


    /// <summary>
    /// Validates the parameter set. Call before Reset after tuning. Invalid values throw ArgumentException.
    /// </summary>
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
