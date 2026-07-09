using System.Numerics;

namespace InkStrokeModeler.Tests;

// Regression coverage for the macOS "brush cut-off" bug.
//
// Root cause: a Move input carrying a large time delta makes the explicit-Euler
// spring integrator diverge to non-finite output. nSteps is capped at
// MaxOutputsPerCall, so once dt exceeds MaxOutputsPerCall / MinOutputRate the
// per-step size grows past the stability limit and output blows up to NaN/Inf,
// silently freezing the stroke (no exception, no error log).
//
// Fix: PolylineInteractiveGenerator clamps the per-input dt handed to the modeler
// to MaxOutputsPerCall / MinOutputRate. These tests assert the modeler contract
// the clamp relies on (finite within the bound) and document the divergence past it.
public sealed class LargeTimeGapReproTests
{
    private static StrokeModelParams Params => StrokeModelParams.CreateCialloDefault();

    private static double MaxSafeDeltaSeconds =>
        Params.Sampling.MaxOutputsPerCall / Params.Sampling.MinOutputRate;

    private static StrokeModeler NewModeler()
    {
        StrokeModeler modeler = new();
        modeler.Reset(Params);
        return modeler;
    }

    // The clamp guarantees per-input dt never exceeds MaxSafeDeltaSeconds.
    // Within that bound the integrator must stay finite for any jump distance.
    [Theory]
    [InlineData(0.05)]
    [InlineData(0.2)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void MoveWithinClampBoundStaysFinite(double gapSeconds)
    {
        Assert.True(gapSeconds <= MaxSafeDeltaSeconds,
            $"test gap {gapSeconds}s must be within the clamp bound {MaxSafeDeltaSeconds}s");

        StrokeModeler modeler = NewModeler();
        List<ModelerResult> results = [];

        modeler.Update(new ModelerInput(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f), results);

        results.Clear();
        modeler.Update(new ModelerInput(
            InputEventType.Move,
            new Vector2(400, 300),
            TimeSpan.FromSeconds(gapSeconds),
            .5f), results);

        Assert.All(results, r =>
        {
            Assert.True(r.Position.IsFinite(), $"gap={gapSeconds}s produced non-finite position {r.Position}");
            Assert.True(r.Velocity.IsFinite(), $"gap={gapSeconds}s produced non-finite velocity {r.Velocity}");
        });
    }

    // Simulates the real interaction: normal-cadence moves, then a large jump.
    // With dt clamped to the safe bound, output must remain finite.
    [Fact]
    public void ClampedStallThenResumeStaysFinite()
    {
        StrokeModeler modeler = NewModeler();
        modeler.Update(new ModelerInput(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f));
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(10, 0), TimeSpan.FromSeconds(.016), .5f));

        double t = .016;
        // A 3s wall-clock stall, clamped to the safe bound before reaching the modeler.
        t += Math.Min(3.0, MaxSafeDeltaSeconds);
        List<ModelerResult> results = modeler.Update(
            new ModelerInput(InputEventType.Move, new Vector2(400, 300), TimeSpan.FromSeconds(t), .5f));

        Assert.All(results, r =>
            Assert.True(r.Position.IsFinite(), $"non-finite position {r.Position} after clamped stall"));
    }

    // Documents the divergence the clamp prevents: an unclamped large dt still
    // blows up. This pins the failure mode so a future modeler change that makes
    // the integrator unconditionally stable would surface here (and let the clamp
    // be revisited).
    [Fact]
    public void UnclampedLargeGapDiverges_DocumentsWhyClampIsNeeded()
    {
        StrokeModeler modeler = NewModeler();
        modeler.Update(new ModelerInput(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f));

        List<ModelerResult> results = modeler.Update(
            new ModelerInput(InputEventType.Move, new Vector2(200, 120), TimeSpan.FromSeconds(5.0), .5f));

        Assert.Contains(results, r => !r.Position.IsFinite());
    }
}
