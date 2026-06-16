using System;
using System.Collections.Generic;
using Godot;
using MathNet.Numerics.LinearAlgebra;

namespace Ciallo.Geometry;

public static partial class PolylineExtension
{
    private const float DisplacementLaplacianEpsilon = 1e-5f;

    /// <summary>
    /// Repairs a polyline span by pulling constrained points onto target displacements while
    /// keeping the displacement field Laplacian-smooth.
    ///
    /// We solve for displacement \(d_i = x'_i - x_i\), not raw positions, so unconstrained points
    /// drift only as much as smoothness and the per-point penalty allow:
    ///
    /// $$
    /// \min_d \;
    /// \sum_i \left\lVert d_{i-1} - 2d_i + d_{i+1} \right\rVert^2
    /// \;+\;
    /// \lambda_{\mathrm{disp}} \sum_i w_i \left\lVert d_i \right\rVert^2,
    /// \qquad
    /// w_i = 1 + \alpha \left(\frac{\operatorname{dist}(i,\text{nearest origin})}{L}\right)^2
    /// $$
    ///
    /// subject to \(d_i\) fixed for every <paramref name="fixedDisplacements"/> entry. The first
    /// term keeps shape changes gradual; the second is the ramp-like rule that makes far-away
    /// points expensive to move.
    ///
    /// <paramref name="penaltyOrigins"/> are the indices the penalty distance is measured from
    /// (nearest wins). This lets callers distinguish constrained points that should "pull" the
    /// curve (snap targets) from points that are merely fixed boundaries (a junction anchor): list
    /// only the former as origins so the correction concentrates there and fades along the span.
    /// </summary>
    /// <returns>Resolved positions for the span (<c>positions[i] + d_i</c>).</returns>
    public static Vector2[] SolveDisplacementLaplacian(
        IReadOnlyList<Vector2> positions,
        IReadOnlyDictionary<int, Vector2> fixedDisplacements,
        IReadOnlyList<int> penaltyOrigins,
        double displacementWeight,
        double farDisplacementPenalty)
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

        var penaltyDistance = BuildNormalizedPenaltyDistances(positions, penaltyOrigins);

        // X and Y are independent; pack them as two RHS columns and solve in one QR back-substitution.
        int rowCount = Math.Max(count - 2, 0) + variableCount;
        var matrix = Matrix<double>.Build.Dense(rowCount, variableCount);
        var rhs = Matrix<double>.Build.Dense(rowCount, 2);

        int row = 0;
        for (int i = 1; i < count - 1; i++)
        {
            EmitLaplacianTerm(matrix, rhs, row, variableByPoint, fixedDisplacements, i - 1, 1.0);
            EmitLaplacianTerm(matrix, rhs, row, variableByPoint, fixedDisplacements, i, -2.0);
            EmitLaplacianTerm(matrix, rhs, row, variableByPoint, fixedDisplacements, i + 1, 1.0);
            row++;
        }

        for (int i = 0; i < count; i++)
        {
            int variable = variableByPoint[i];
            if (variable < 0)
                continue;

            double distance = penaltyDistance[i];
            matrix[row, variable] = Math.Sqrt(displacementWeight * (1.0 + farDisplacementPenalty * distance * distance));
            row++;
        }

        var solution = matrix.QR().Solve(rhs);
        for (int i = 0; i < count; i++)
        {
            int v = variableByPoint[i];
            var displacement = v >= 0
                ? new Vector2((float)solution[v, 0], (float)solution[v, 1])
                : fixedDisplacements[i];
            result[i] = positions[i] + displacement;
        }

        return result;
    }

    // Distance (along the span, normalized by span length) from each point to the nearest
    // penalty origin. With no origins the penalty is uniform.
    private static double[] BuildNormalizedPenaltyDistances(
        IReadOnlyList<Vector2> positions,
        IReadOnlyList<int> penaltyOrigins)
    {
        int count = positions.Count;
        var arcLength = new double[count];
        for (int i = 1; i < count; i++)
            arcLength[i] = arcLength[i - 1] + positions[i - 1].DistanceTo(positions[i]);

        double invTotalLength = 1.0 / Math.Max(arcLength[^1], DisplacementLaplacianEpsilon);
        var distance = new double[count];
        for (int i = 0; i < count; i++)
        {
            double nearest = double.PositiveInfinity;
            foreach (int origin in penaltyOrigins)
                nearest = Math.Min(nearest, Math.Abs(arcLength[i] - arcLength[origin]));
            distance[i] = double.IsPositiveInfinity(nearest) ? 0.0 : nearest * invTotalLength;
        }

        return distance;
    }

    // Writes one (row, point) entry of the Laplacian stencil:
    // - if the point is a free variable, accumulate the coefficient into the matrix;
    // - if the point is constrained, fold its known displacement into the RHS.
    private static void EmitLaplacianTerm(
        Matrix<double> matrix,
        Matrix<double> rhs,
        int row,
        int[] variableByPoint,
        IReadOnlyDictionary<int, Vector2> fixedDisplacements,
        int point,
        double coefficient)
    {
        int variable = variableByPoint[point];
        if (variable >= 0)
        {
            matrix[row, variable] += coefficient;
            return;
        }

        var knownDisplacement = fixedDisplacements[point];
        rhs[row, 0] -= coefficient * knownDisplacement.X;
        rhs[row, 1] -= coefficient * knownDisplacement.Y;
    }
}
