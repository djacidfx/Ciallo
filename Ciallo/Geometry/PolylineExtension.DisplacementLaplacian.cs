using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Geometry;

public static partial class PolylineExtension
{
    private const float DisplacementLaplacianEpsilon = 1e-5f;

    /// <summary>
    /// Solves a constrained displacement-Laplacian deformation for one polyline span.
    ///
    /// The unknown is displacement \(d_i = x'_i - x_i\), not raw position \(x'_i\). This
    /// preserves the authored curve away from hard constraints: free points move when
    /// Laplacian smoothness and the ramp target agree that they should.
    ///
    /// $$
    /// \min_d \;
    /// \sum_i \left\lVert d_{i-1} - 2d_i + d_{i+1} \right\rVert^2
    /// \;+\;
    /// \lambda_{\mathrm{ramp}} \sum_i \left\lVert d_i - q_i \right\rVert^2
    /// $$
    ///
    /// subject to \(d_i\) fixed for every <paramref name="fixedDisplacements"/> entry. The
    /// Laplacian term keeps the correction gradual; the ramp target lets snap points pull nearby
    /// samples along before the correction fades back to zero.
    ///
    /// <paramref name="rampOrigins"/> are fixed-displacement indices whose displacement should
    /// softly pull nearby samples. Fixed boundaries that should not pull the curve, such as a
    /// zero-displacement junction anchor, should be omitted.
    /// </summary>
    /// <returns>Resolved positions for the span (<c>positions[i] + d_i</c>).</returns>
    /// <remarks>
    /// Precision contract: callers only need the resolved positions accurate to about 1e-3 world
    /// units (0.1 is still visually fine), so single-precision-level accuracy is plenty and the
    /// <c>(float)</c> casts on the solution below are acceptable. The one hard requirement is that
    /// fixed points must not move: they are never variables and are written straight back as
    /// <c>positions[i] + fixedDisplacements[i]</c>, so they stay exact regardless of solver accuracy.
    /// </remarks>
    public static Vector2[] SolveDisplacementLaplacian(
        IReadOnlyList<Vector2> positions,
        IReadOnlyDictionary<int, Vector2> fixedDisplacements,
        IReadOnlyList<int> rampOrigins,
        double rampTargetWeight)
    {
        int count = positions.Count;
        var variableByPoint = new int[count];
        Array.Fill(variableByPoint, -1);

        int variableCount = 0;
        for (int i = 0; i < count; i++)
            if (!fixedDisplacements.ContainsKey(i))
                variableByPoint[i] = variableCount++;

        var result = new Vector2[count];

        // Every point is constrained: nothing to solve for.
        if (variableCount == 0)
        {
            for (int i = 0; i < count; i++)
                result[i] = positions[i] + fixedDisplacements[i];
            return result;
        }

        var rampTargets = BuildRampTargetDisplacements(positions, fixedDisplacements, rampOrigins);

        var diagonal = new double[variableCount];
        var upper1 = new double[Math.Max(variableCount - 1, 0)];
        var upper2 = new double[Math.Max(variableCount - 2, 0)];
        var rhsX = new double[variableCount];
        var rhsY = new double[variableCount];

        // Reused across rows; AddLaplacianTerm only writes the first termCount slots, so resetting
        // termCount to 0 is enough — stale tail entries are never read.
        Span<int> variables = stackalloc int[3];
        Span<double> coefficients = stackalloc double[3];
        for (int i = 1; i < count - 1; i++)
        {
            var knownDisplacement = Vector2.Zero;
            int termCount = 0;
            AddLaplacianTerm(variableByPoint, fixedDisplacements, i - 1, 1.0, variables, coefficients, ref termCount, ref knownDisplacement);
            AddLaplacianTerm(variableByPoint, fixedDisplacements, i, -2.0, variables, coefficients, ref termCount, ref knownDisplacement);
            AddLaplacianTerm(variableByPoint, fixedDisplacements, i + 1, 1.0, variables, coefficients, ref termCount, ref knownDisplacement);
            AccumulateNormalRow(diagonal, upper1, upper2, rhsX, rhsY, variables, coefficients, termCount, -knownDisplacement);
        }

        for (int i = 0; i < count; i++)
        {
            int variable = variableByPoint[i];
            if (variable < 0)
                continue;

            diagonal[variable] += rampTargetWeight;
            rhsX[variable] += rampTargetWeight * rampTargets[i].X;
            rhsY[variable] += rampTargetWeight * rampTargets[i].Y;
        }

        var solutionX = SolvePentadiagonal(diagonal, upper1, upper2, rhsX);
        var solutionY = SolvePentadiagonal(diagonal, upper1, upper2, rhsY);
        for (int i = 0; i < count; i++)
        {
            int v = variableByPoint[i];
            var displacement = v >= 0
                ? new Vector2((float)solutionX[v], (float)solutionY[v])
                : fixedDisplacements[i];
            result[i] = positions[i] + displacement;
        }

        return result;
    }

    private static Vector2[] BuildRampTargetDisplacements(
        IReadOnlyList<Vector2> positions,
        IReadOnlyDictionary<int, Vector2> fixedDisplacements,
        IReadOnlyList<int> rampOrigins)
    {
        int count = positions.Count;
        var arcLength = new double[count];
        for (int i = 1; i < count; i++)
            arcLength[i] = arcLength[i - 1] + positions[i - 1].DistanceTo(positions[i]);

        double totalLength = arcLength[^1];
        double averageSegmentLength = totalLength / Math.Max(count - 1, 1);
        double minimumRampRadius = Math.Min(totalLength, 4.0 * Math.Max(averageSegmentLength, DisplacementLaplacianEpsilon));
        var targets = new Vector2[count];
        var influenceSums = new double[count];

        foreach (int origin in rampOrigins)
        {
            var originDisplacement = fixedDisplacements[origin];
            double radius = Math.Min(
                totalLength,
                Math.Max(2.0 * originDisplacement.Length(), minimumRampRadius));
            double invRadius = 1.0 / Math.Max(radius, DisplacementLaplacianEpsilon);

            for (int i = 0; i < count; i++)
            {
                double t = Math.Clamp(Math.Abs(arcLength[i] - arcLength[origin]) * invRadius, 0.0, 1.0);
                double influence = 1.0 - t * t * (3.0 - 2.0 * t);
                targets[i] += originDisplacement * (float)influence;
                influenceSums[i] += influence;
            }
        }

        for (int i = 0; i < count; i++)
            if (influenceSums[i] > 1.0)
                targets[i] /= (float)influenceSums[i];

        return targets;
    }

    private static void AddLaplacianTerm(
        int[] variableByPoint,
        IReadOnlyDictionary<int, Vector2> fixedDisplacements,
        int point,
        double coefficient,
        Span<int> variables,
        Span<double> coefficients,
        ref int termCount,
        ref Vector2 knownDisplacement)
    {
        int variable = variableByPoint[point];
        if (variable >= 0)
        {
            variables[termCount] = variable;
            coefficients[termCount] = coefficient;
            termCount++;
            return;
        }

        knownDisplacement += fixedDisplacements[point] * (float)coefficient;
    }

    private static void AccumulateNormalRow(
        double[] diagonal,
        double[] upper1,
        double[] upper2,
        double[] rhsX,
        double[] rhsY,
        Span<int> variables,
        Span<double> coefficients,
        int termCount,
        Vector2 rhs)
    {
        for (int a = 0; a < termCount; a++)
        {
            int variableA = variables[a];
            double coefficientA = coefficients[a];
            rhsX[variableA] += coefficientA * rhs.X;
            rhsY[variableA] += coefficientA * rhs.Y;

            for (int b = a; b < termCount; b++)
            {
                int left = variableA;
                int right = variables[b];
                double value = coefficientA * coefficients[b];
                if (left > right)
                    (left, right) = (right, left);

                int offset = right - left;
                // The normal matrix A^T A is exactly pentadiagonal: every Laplacian row touches the
                // three consecutive points {i-1, i, i+1}, so the lowest and highest free variables in
                // a row differ by at most 2 no matter which points are fixed (fixing points only
                // renumbers variables, it cannot stretch a single row past those three points). An
                // offset above 2 can only appear if the smoothness stencil itself is widened beyond
                // three points, which is a code change, not a runtime input. Guard it so that change
                // fails loudly instead of silently corrupting upper2.
                if (offset == 0)
                    diagonal[left] += value;
                else if (offset == 1)
                    upper1[left] += value;
                else if (offset == 2)
                    upper2[left] += value;
                else
                    throw new InvalidOperationException(
                        $"Normal matrix is not pentadiagonal (variable offset {offset}); the Laplacian stencil must stay 3-point.");
            }
        }
    }

    private static double[] SolvePentadiagonal(
        double[] diagonal,
        double[] upper1,
        double[] upper2,
        double[] rhs)
    {
        int count = diagonal.Length;
        var factorDiagonal = new double[count];
        var lower1 = new double[count];
        var lower2 = new double[count];

        for (int i = 0; i < count; i++)
        {
            if (i >= 2)
                lower2[i] = upper2[i - 2] / factorDiagonal[i - 2];
            if (i >= 1)
            {
                double value = upper1[i - 1];
                if (i >= 2)
                    value -= lower2[i] * factorDiagonal[i - 2] * lower1[i - 1];
                lower1[i] = value / factorDiagonal[i - 1];
            }

            factorDiagonal[i] = diagonal[i];
            if (i >= 1)
                factorDiagonal[i] -= lower1[i] * lower1[i] * factorDiagonal[i - 1];
            if (i >= 2)
                factorDiagonal[i] -= lower2[i] * lower2[i] * factorDiagonal[i - 2];

            // The normal matrix is symmetric positive definite, so every pivot should be positive.
            // A degenerate/near-zero input (e.g. a zero-length span) could still drive a pivot to ~0
            // and turn the divisions below into NaN/Inf, which would propagate into rendered point
            // coordinates. Clamp it: a slightly-off result for a degenerate input is acceptable, a
            // NaN coordinate is not.
            if (factorDiagonal[i] < DisplacementLaplacianEpsilon)
                factorDiagonal[i] = DisplacementLaplacianEpsilon;
        }

        var y = new double[count];
        for (int i = 0; i < count; i++)
        {
            y[i] = rhs[i];
            if (i >= 1)
                y[i] -= lower1[i] * y[i - 1];
            if (i >= 2)
                y[i] -= lower2[i] * y[i - 2];
        }

        for (int i = 0; i < count; i++)
            y[i] /= factorDiagonal[i];

        var result = new double[count];
        for (int i = count - 1; i >= 0; i--)
        {
            result[i] = y[i];
            if (i + 1 < count)
                result[i] -= lower1[i + 1] * result[i + 1];
            if (i + 2 < count)
                result[i] -= lower2[i + 2] * result[i + 2];
        }

        return result;
    }
}
