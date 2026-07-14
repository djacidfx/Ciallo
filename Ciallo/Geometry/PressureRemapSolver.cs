using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace Ciallo.Geometry;

/// <summary>
/// Solves for a "straightening" remap curve.
/// <para>
/// The data pipeline is a one-way chain:
/// <code>force --[device hardware]--> reading --[remap curve]--> pressure</code>
/// where
/// <list type="bullet">
/// <item><b>force</b>: physical nib force in grams-force, the chart's X.</item>
/// <item><b>reading</b>: the device's normalized 0..1 digital output, the chart's Y. Fixed by hardware.</item>
/// <item><b>pressure</b>: the final normalized 0..1 value the app consumes, after the remap curve.</item>
/// </list>
/// The device response (a set of measured <c>(force, reading)</c> samples) is immutable; only the
/// remap curve <c>reading -> pressure</c> is under our control.
/// </para>
/// <para>
/// "Straightening" means making the composite <c>force -> pressure</c> linear. Since
/// <c>force -> reading</c> is fixed, we pick a <b>target</b> pressure for each sample that lies on the
/// straight line from <c>(activationForce, 0)</c> to <c>(maxForce, 1)</c>, then fit a curve to the pairs
/// <c>(reading, target)</c>. The fitted curve is <c>remap = target ∘ deviceResponse⁻¹</c>, so at runtime
/// <c>force -> reading -> pressure</c> collapses to the straight <c>force -> target</c> line.
/// </para>
/// </summary>
public static class PressureRemapSolver
{
    /// <summary>Anchor counts to try, cheapest first. More anchors fit better but wiggle more.</summary>
    private static readonly int[] AnchorCounts = [2, 3, 4];

    /// <summary>
    /// Per-extra-anchor penalty added to the RMS fit error, in normalized pressure units.
    /// Higher = prefer simpler curves. Tune this to taste.
    /// </summary>
    private const float AnchorPenalty = 0.007f;

    /// <summary>
    /// Fits a remap curve that straightens the given device response up to a raw-reading cutoff.
    /// </summary>
    /// <param name="deviceResponse">Measured samples as <c>(force, reading)</c>, ascending by force.</param>
    /// <param name="maxReading">
    /// Raw reading mapped to full pressure. The corresponding physical force is interpolated from
    /// <paramref name="deviceResponse"/>; readings beyond this point sample to 1.
    /// </param>
    /// <returns>
    /// The remap curve mapping <c>reading -> pressure</c>, or <c>null</c> if the response is degenerate
    /// (fewer than two samples, or no usable reading span).
    /// </returns>
    public static ImmutableArray<BezierPoint>? Straighten(
        IReadOnlyList<Vector2> deviceResponse,
        float maxReading)
    {
        if (deviceResponse.Count < 2)
            return null;

        float activationForce = deviceResponse[0].X;

        // Use the first force at which the response reaches maxReading. When the cutoff falls
        // between measurements, interpolate its force from the surrounding response samples.
        int upperIndex = 1;
        while (deviceResponse[upperIndex].Y < maxReading)
            upperIndex++;
        var lower = deviceResponse[upperIndex - 1];
        var upper = deviceResponse[upperIndex];
        float maxForce = Mathf.Lerp(lower.X, upper.X,
            Mathf.InverseLerp(lower.Y, upper.Y, maxReading));

        float forceSpan = maxForce - activationForce;
        if (forceSpan <= 1e-5f)
            return null;

        // Fit only the unsaturated response, then pin the final anchor at (maxReading, 1).
        // SampleX returns the final anchor's Y beyond its X, producing the saturated region.
        var fitInput = deviceResponse
            .TakeWhile(sample => sample.Y < maxReading)
            .Select(sample => new Vector2(
                sample.Y,
                (sample.X - activationForce) / forceSpan))
            .Append(new Vector2(maxReading, 1f))
            .ToArray();

        // Search anchor counts; keep the one with the lowest penalized, monotone fit.
        BezierPoint[] best = null;
        float bestScore = float.PositiveInfinity;
        foreach (int anchorCount in AnchorCounts)
        {
            var candidate = fitInput.FitBezier(anchorCount);
            if (!candidate.IsXMonotone())
                continue; // a non-monotone curve breaks SampleX's binary search — reject outright

            float rms = RmsError(candidate, fitInput);
            float score = rms + AnchorPenalty * (anchorCount - AnchorCounts[0]);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best is null ? null : [.. best];
    }

    /// <summary>RMS of the vertical (pressure) residual between the fitted curve and the target samples.</summary>
    private static float RmsError(IReadOnlyList<BezierPoint> curve, IReadOnlyList<Vector2> fitInput)
    {
        double sumSq = 0.0;
        foreach (var sample in fitInput)
        {
            float predicted = curve.SampleX(sample.X);
            float residual = predicted - sample.Y;
            sumSq += residual * (double)residual;
        }
        return (float)Math.Sqrt(sumSq / fitInput.Count);
    }
}
