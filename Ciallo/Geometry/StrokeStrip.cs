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
/// 2026 April, Repeatedly gacha with Sonnet 4.6 to get current result.
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
        IReadOnlyList<IReadOnlyList<Vector2>> polylines,
        int centerSampleCount = 64,
        int maxIterations = 5,
        float projectionStep = 1f / 128f)
    {
        if (polylines == null || polylines.Count == 0)
            return new List<Vector2>();

        IReadOnlyList<IReadOnlyList<Vector2>> filtered =
            polylines.Where(s => s != null && s.Count >= 2).ToArray();
        if (filtered.Count == 0)
            return new List<Vector2>();

        // 0. Orient all polylines so they run in the same general direction.
        //    The internal point order within each polyline is trusted; only the
        //    per-stroke direction (flip) is adjusted.
        var oriented = OrientPolylines(filtered);

        // Compute the consensus axis once from the now-consistent orientations.
        var axis = ComputeAxis(oriented);

        // 1. Initialise joint parameters by projecting every point onto the cluster's
        //    mean direction axis and normalising globally to [0, 1].
        //    Unlike per-stroke arc-length params this correctly assigns sub-ranges to
        //    sequential (end-to-end) strokes as well as spanning [0,1] for parallel ones.
        var jointParamsPerStroke = InitJointParamsFromAxisProjection(oriented, axis);

        // 2. Build the initial center curve.
        var centerCurve = BuildCenterCurve(oriented, jointParamsPerStroke, centerSampleCount);

        // 3. Iterate: snap endpoints → reproject → rebuild.
        //    Snapping BEFORE each projection prevents the inward-drift cascade: the
        //    Gaussian-smoothed BuildCenterCurve pulls center[0]/center[^1] slightly
        //    inward; without correction ProjectStrokesToCenterCurve then moves stroke
        //    endpoint parameters slightly away from 0/1, making the next build worse.
        for (int iter = 0; iter < maxIterations; iter++)
        {
            SnapCenterCurveEndpoints(oriented, centerCurve, axis);
            ProjectStrokesToCenterCurve(oriented, centerCurve, jointParamsPerStroke, projectionStep);
            centerCurve = BuildCenterCurve(oriented, jointParamsPerStroke, centerSampleCount);
        }

        // Final snap on the last rebuilt curve.
        SnapCenterCurveEndpoints(oriented, centerCurve, axis);

        return centerCurve;
    }

    /// <summary>
    /// Return a copy of <paramref name="polylines"/> with individual strokes flipped as
    /// needed so all strokes run in the same general direction.
    /// Uses iterative length-weighted voting (longer strokes dominate the consensus).
    /// </summary>
    private static IReadOnlyList<IReadOnlyList<Vector2>> OrientPolylines(
        IReadOnlyList<IReadOnlyList<Vector2>> polylines)
    {
        var result = polylines.ToArray();

        // Repeat a few times because flipping a stroke changes the mean direction.
        for (int pass = 0; pass < 3; pass++)
        {
            // Length-weighted mean direction (no normalise → longer strokes vote more).
            Vector2 meanDir = Vector2.Zero;
            foreach (var s in result)
                meanDir += s[^1] - s[0];

            if (meanDir.LengthSquared() < 1e-6f) break; // no dominant direction

            bool anyFlipped = false;
            for (int i = 0; i < result.Length; i++)
            {
                if ((result[i][^1] - result[i][0]).Dot(meanDir) < 0f)
                {
                    // Build a reversed copy; do not mutate the original list.
                    var arr = new Vector2[result[i].Count];
                    for (int k = 0; k < arr.Length; k++)
                        arr[k] = result[i][result[i].Count - 1 - k];
                    result[i] = arr;
                    anyFlipped = true;
                }
            }

            if (!anyFlipped) break;
        }

        return result;
    }

    /// <summary>
    /// Compute the normalised mean direction axis from a set of consistently-oriented strokes.
    /// </summary>
    private static Vector2 ComputeAxis(IReadOnlyList<IReadOnlyList<Vector2>> strokes)
    {
        Vector2 axis = Vector2.Zero;
        foreach (var s in strokes)
            axis += s[^1] - s[0];
        if (axis.LengthSquared() < 1e-6f)
            axis = Vector2.Right;
        return axis.Normalized();
    }

    /// <summary>
    /// Snap center[0] to the stroke endpoint with the minimum projection onto <paramref name="axis"/>,
    /// and center[^1] to the endpoint with the maximum projection.
    /// After <see cref="OrientPolylines"/> every stroke satisfies s[^1].Dot(axis) ≥ s[0].Dot(axis),
    /// so min comes from a stroke's first point and max from a stroke's last point.
    /// Called before every projection step and after the final rebuild to prevent the
    /// Gaussian boundary from causing an inward-drift cascade across iterations.
    /// </summary>
    private static void SnapCenterCurveEndpoints(
        IReadOnlyList<IReadOnlyList<Vector2>> strokes,
        List<Vector2> center,
        Vector2 axis)
    {
        if (center.Count < 2) return;

        Vector2 startSnap = strokes[0][0], endSnap = strokes[0][^1];
        float minProj = float.MaxValue, maxProj = float.MinValue;

        foreach (var s in strokes)
        {
            float projStart = s[0].Dot(axis);
            float projEnd = s[^1].Dot(axis);
            if (projStart < minProj)
            {
                minProj = projStart;
                startSnap = s[0];
            }
            if (projEnd > maxProj)
            {
                maxProj = projEnd;
                endSnap = s[^1];
            }
        }

        center[0] = startSnap;
        center[^1] = endSnap;
    }

    /// <summary>
    /// Initialise joint parameters by projecting every stroke point onto the cluster's
    /// mean direction axis, then normalising the full range to [0, 1].
    /// </summary>
    private static List<float[]> InitJointParamsFromAxisProjection(
        IReadOnlyList<IReadOnlyList<Vector2>> strokes, Vector2 axis)
    {
        // Global projection extent across ALL points of ALL strokes.
        float globalMin = float.MaxValue, globalMax = float.MinValue;
        foreach (var s in strokes)
        foreach (var p in s)
        {
            float proj = p.Dot(axis);
            if (proj < globalMin) globalMin = proj;
            if (proj > globalMax) globalMax = proj;
        }

        float range = globalMax - globalMin;

        var jointParams = new List<float[]>(strokes.Count);
        foreach (var s in strokes)
        {
            var tArr = new float[s.Count];
            if (range < 1e-6f)
            {
                // Degenerate: all points coincide on the axis – uniform fallback.
                for (int i = 0; i < tArr.Length; i++)
                    tArr[i] = (float)i / Mathf.Max(tArr.Length - 1, 1);
            }
            else
            {
                for (int i = 0; i < tArr.Length; i++)
                    tArr[i] = (s[i].Dot(axis) - globalMin) / range;
            }
            jointParams.Add(tArr);
        }

        return jointParams;
    }

    /// <summary>
    /// Parameterize a polyline by arc length, returning normalized s ∈ [0,1] per point.
    /// </summary>
    private static float[] ComputeArcLengthParams(IReadOnlyList<Vector2> polyline)
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
            return s;
        }

        float invTotal = 1.0f / totalLen;
        for (int i = 0; i < n; i++)
            s[i] *= invTotal;

        return s;
    }

    /// <summary>
    /// Build the center curve based on the current joint parameters from all strokes.
    /// The algorithm uniformly samples centerSampleCount values on [0,1] and averages nearby points via weights.
    /// </summary>
    private static List<Vector2> BuildCenterCurve(
        IReadOnlyList<IReadOnlyList<Vector2>> strokes,
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
        IReadOnlyList<IReadOnlyList<Vector2>> polylines,
        IReadOnlyList<Vector2> centerCurve,
        IReadOnlyList<float[]> jointParamsPerStroke,
        float projectionStep)
    {
        if (centerCurve.Count < 2)
            return;

        // Precompute arc lengths and cumulative lengths per segment to map between [0,1] parameters.
        var centerS = ComputeArcLengthParams(centerCurve);

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
        // Binary search for the largest idx where centerS[idx] <= t
        int n = centerCurve.Count;
        int lo = 0, hi = n - 2;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (centerS[mid] <= t) lo = mid;
            else hi = mid - 1;
        }
        int idx = lo;

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