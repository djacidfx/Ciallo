using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ciallo.Geometry;

/// <summary>
/// StrokeStrip-like stroke clustering with joint parameterization and center polyline fitting.
/// Takes multiple polylines and returns a single representative center polyline.
/// </summary>
/// <remarks>
/// Generate by copilot by referencing the StrokeStrip GitHub repository.
/// https://github.com/davepagurek/StrokeStrip
/// Not bad to work with.
/// </remarks>
public static partial class Geometry
{
    /// <summary>
    /// Cluster multiple polylines into a single representative polyline.
    /// </summary>
    /// <param name="polylines">List of polylines. Each polyline is a list of points in drawing space.</param>
    /// <param name="centerSampleCount">Number of samples on the output center polyline.</param>
    /// <param name="maxIterations">Max iterations for joint parameter refinement.</param>
    /// <param name="projectionStep">Step for projecting points to center curve (0~1 fraction of param domain).</param>
    /// <returns>A list of positions forming the center polyline.</returns>
    public static List<Vector2> ClusterPolylines(
        List<List<Vector2>> polylines,
        int centerSampleCount = 64,
        int maxIterations = 5,
        float projectionStep = 1f / 128f)
    {
        // Boundary cases
        if (polylines == null || polylines.Count == 0)
            return new List<Vector2>();

        polylines = polylines.Where(s => s != null && s.Count >= 2).ToList();
        if (polylines.Count == 0)
            return new List<Vector2>();

        // 1. Parameterize each stroke by arc length to get normalized params on [0,1]
        var arcParamsPerStroke = new List<float[]>();
        var lengthsPerStroke = new List<float>();
        foreach (var stroke in polylines)
        {
            var (sParams, totalLength) = ComputeArcLengthParams(stroke);
            arcParamsPerStroke.Add(sParams);
            lengthsPerStroke.Add(totalLength);
        }

        // 2. Initialize each point's joint parameter t using the arc-length params
        var jointParamsPerStroke = arcParamsPerStroke
            .Select(s => (float[])s.Clone())
            .ToList();

        // 3. Build the center curve by sampling [0,1] and averaging points with similar t
        var centerCurve = BuildCenterCurve(polylines, jointParamsPerStroke, centerSampleCount);

        // 4. Iterate: project stroke points onto the center curve and rebuild it
        for (int iter = 0; iter < maxIterations; iter++)
        {
            // 4.1 Find the best t for each point on the current center curve
            ProjectStrokesToCenterCurve(
                polylines,
                centerCurve,
                jointParamsPerStroke,
                projectionStep);

            // 4.2 Rebuild the center curve using the updated parameters
            centerCurve = BuildCenterCurve(polylines, jointParamsPerStroke, centerSampleCount);
        }

        return centerCurve;
    }

    /// <summary>
    /// Parameterize a polyline by arc length, returning normalized s ∈ [0,1] per point plus total length.
    /// </summary>
    private static (float[] sParams, float totalLength) ComputeArcLengthParams(IReadOnlyList<Vector2> polyline)
    {
        int n = polyline.Count;
        var s = new float[n];
        s[0] = 0f;

        float totalLen = 0f;
        for (int i = 1; i < n; i++)
        {
            float segLen = (polyline[i] - polyline[i - 1]).Length();
            totalLen += segLen;
            s[i] = totalLen;
        }

        if (totalLen <= 1e-6f)
        {
            // Degenerate case: all points coincide
            for (int i = 0; i < n; i++)
                s[i] = 0f;
            return (s, 0f);
        }

        float invTotal = 1.0f / totalLen;
        for (int i = 0; i < n; i++)
            s[i] *= invTotal;

        return (s, totalLen);
    }

    /// <summary>
    /// Build the center curve based on the current joint parameters from all strokes.
    /// The algorithm uniformly samples centerSampleCount values on [0,1] and averages nearby points via weights.
    /// </summary>
    private static List<Vector2> BuildCenterCurve(
        IReadOnlyList<List<Vector2>> strokes,
        IReadOnlyList<float[]> jointParamsPerStroke,
        int centerSampleCount)
    {
        var center = new List<Vector2>(centerSampleCount);
        if (centerSampleCount <= 1)
        {
            // Ensure at least two points
            centerSampleCount = 2;
        }

        float dt = 1.0f / (centerSampleCount - 1);

        for (int k = 0; k < centerSampleCount; k++)
        {
            float t0 = k * dt;
            Vector2 sum = Vector2.Zero;
            float wSum = 0f;

            for (int si = 0; si < strokes.Count; si++)
            {
                var stroke = strokes[si];
                var tArr = jointParamsPerStroke[si];

                for (int pi = 0; pi < stroke.Count; pi++)
                {
                    float ti = tArr[pi];

                    // Gaussian weight: farther from t0 contributes less
                    float d = ti - t0;
                    float w = Mathf.Exp(-(d * d) / (2 * 0.05f * 0.05f)); // sigma ≈ 0.05, adjustable

                    if (w < 1e-3f) continue;

                    sum += stroke[pi] * w;
                    wSum += w;
                }
            }

            if (wSum > 1e-6f)
                center.Add(sum / wSum);
            else
            {
                // Degenerate fallback: reuse the previous point or use zero
                if (center.Count > 0)
                    center.Add(center[^1]);
                else
                    center.Add(Vector2.Zero);
            }
        }

        return center;
    }

    /// <summary>
    /// For each stroke point, find the parameter t ∈ [0,1] on the center polyline that minimizes the geometric distance.
    /// Uses local linear parameterization plus a local search.
    /// </summary>
    private static void ProjectStrokesToCenterCurve(
        IReadOnlyList<List<Vector2>> polylines,
        IReadOnlyList<Vector2> centerCurve,
        IReadOnlyList<float[]> jointParamsPerStroke,
        float projectionStep)
    {
        if (centerCurve.Count < 2)
            return;

        // Precompute arc lengths and cumulative lengths per segment to map between [0,1] parameters.
        var (centerS, centerTotalLen) = ComputeArcLengthParams(centerCurve);

        for (int si = 0; si < polylines.Count; si++)
        {
            var stroke = polylines[si];
            var tArr = jointParamsPerStroke[si];

            for (int pi = 0; pi < stroke.Count; pi++)
            {
                Vector2 p = stroke[pi];
                float tInit = tArr[pi]; // Use the current t as the initial guess

                // Perform a local search around tInit
                float bestT = tInit;
                float bestDist2 = DistanceToCenterAtT(p, centerCurve, centerS, bestT);

                // Linear 1D search: sweep within [tInit - h, tInit + h]
                float h = 0.1f; // Search radius, can be tuned
                int steps = Mathf.CeilToInt(h / projectionStep);

                for (int step = -steps; step <= steps; step++)
                {
                    float tCandidate = tInit + step * projectionStep;
                    if (tCandidate < 0f || tCandidate > 1f) continue;

                    float d2 = DistanceToCenterAtT(p, centerCurve, centerS, tCandidate);
                    if (d2 < bestDist2)
                    {
                        bestDist2 = d2;
                        bestT = tCandidate;
                    }
                }

                tArr[pi] = bestT;
            }
        }
    }

    /// <summary>
    /// Returns the squared distance between point p and the position on the center curve at parameter t ∈ [0,1].
    /// Uses the cumulative arc-length array centerS to locate the segment and perform linear interpolation.
    /// </summary>
    private static float DistanceToCenterAtT(
        Vector2 p,
        IReadOnlyList<Vector2> centerCurve,
        IReadOnlyList<float> centerS,
        float t)
    {
        // t is in [0,1]; find the first centerS[idx] >= t
        int idx = 0;
        int n = centerCurve.Count;
        while (idx < n - 1 && centerS[idx + 1] < t)
            idx++;

        int idxNext = Mathf.Min(idx + 1, n - 1);

        float s0 = centerS[idx];
        float s1 = centerS[idxNext];
        if (Mathf.Abs(s1 - s0) < 1e-6f)
        {
            // Degenerate: fall back to the idx point
            var c = centerCurve[idx];
            return (p - c).LengthSquared();
        }

        float localT = (t - s0) / (s1 - s0);
        Vector2 cPos = centerCurve[idx].Lerp(centerCurve[idxNext], localT);
        return (p - cPos).LengthSquared();
    }
}