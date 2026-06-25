using System;
using System.Collections.Generic;
using Godot;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;

namespace Ciallo.Geometry;

/// <summary>
/// Deforms a polyline to satisfy hard positional constraints (e.g. snapped endpoints) while
/// preserving its <b>shape</b>. "Shape" is represented by the sequence of real interior turning
/// angles: the stroke's bend gesture. A weak adjacent-scale smoothness term keeps segment length
/// changes from becoming locally erratic, and a weak position anchor gently stabilizes free samples.
/// </summary>
public static class PolylineShapeOptimizer
{
    private const double Epsilon = 1e-5;

    /// <summary>
    /// Returns new positions equal in count to <paramref name="positions"/>, with every entry of
    /// <paramref name="fixedPositions"/> honored exactly. Fixed vertices are not variables, so they
    /// stay exact regardless of solver accuracy.
    /// </summary>
    /// <param name="angleVarianceWeight">Weight on turning-angle delta variance. This allows a
    /// uniform bend offset while penalizing concentrated angle changes.</param>
    /// <param name="angleSmoothnessWeight">Weight on adjacent turning-angle delta equality.</param>
    /// <param name="scaleSmoothnessWeight">Weak weight on adjacent edge-scale equality.</param>
    /// <param name="positionAnchorWeight">Weak weight that prefers free samples to stay near their
    /// drawn positions.</param>
    public static Vector2[] PreserveShape(
        IReadOnlyList<Vector2> positions,
        IReadOnlyDictionary<int, Vector2> fixedPositions,
        double angleVarianceWeight = 1.0,
        double angleSmoothnessWeight = 0.5,
        double scaleSmoothnessWeight = 0.05,
        double positionAnchorWeight = 0.02,
        int maxIterations = 100)
    {
        int count = positions.Count;
        var result = new Vector2[count];
        for (int i = 0; i < count; i++)
            result[i] = fixedPositions.TryGetValue(i, out var f) ? f : positions[i];

        // Map free vertices to parameter slots (2 doubles each).
        var freeIndices = new List<int>(count);
        for (int i = 0; i < count; i++)
            if (!fixedPositions.ContainsKey(i))
                freeIndices.Add(i);

        // Nothing to solve, or too short to have an interior turning angle: constraints already written above.
        if (freeIndices.Count == 0 || count < 3)
            return result;

        // Rest shape: real interior turning angles and edge lengths from the ORIGINAL positions, so
        // the rest pose reflects the drawn stroke rather than the already-snapped endpoints.
        var restXs = new double[count];
        var restYs = new double[count];
        for (int i = 0; i < count; i++)
        {
            restXs[i] = positions[i].X;
            restYs[i] = positions[i].Y;
        }

        var restAngles = BuildTurningAngles(restXs, restYs);
        var restLengths = BuildEdgeLengths(restXs, restYs);
        int residualCount = restAngles.Count
            + Math.Max(restAngles.Count - 1, 0)
            + Math.Max(restLengths.Count - 1, 0)
            + freeIndices.Count * 2;

        // Initial guess = current free positions.
        var initial = Vector<double>.Build.Dense(freeIndices.Count * 2);
        for (int k = 0; k < freeIndices.Count; k++)
        {
            var p = positions[freeIndices[k]];
            initial[2 * k] = p.X;
            initial[2 * k + 1] = p.Y;
        }

        // The model reconstructs full positions from the parameter vector (fixed vertices held),
        // then returns turning-angle residuals plus adjacent scale-smoothness residuals.
        Vector<double> Model(Vector<double> p, Vector<double> _)
        {
            var xs = new double[count];
            var ys = new double[count];
            for (int i = 0; i < count; i++)
            {
                xs[i] = result[i].X;
                ys[i] = result[i].Y;
            }
            for (int k = 0; k < freeIndices.Count; k++)
            {
                int pointIndex = freeIndices[k];
                xs[pointIndex] = p[2 * k];
                ys[pointIndex] = p[2 * k + 1];
            }
            return BuildResiduals(xs, ys);
        }

        Vector<double> BuildResiduals(double[] xs, double[] ys)
        {
            var angles = BuildTurningAngles(xs, ys);
            var lengths = BuildEdgeLengths(xs, ys);
            var r = Vector<double>.Build.Dense(residualCount);
            int offset = 0;

            double meanAngleDelta = 0.0;
            for (int i = 0; i < angles.Count; i++)
                meanAngleDelta += WrapPi(angles[i] - restAngles[i]);
            if (angles.Count > 0)
                meanAngleDelta /= angles.Count;

            double sqrtAngleVariance = Math.Sqrt(angleVarianceWeight);
            for (int i = 0; i < angles.Count; i++)
            {
                double delta = WrapPi(angles[i] - restAngles[i]);
                r[offset++] = sqrtAngleVariance * (delta - meanAngleDelta);
            }

            double sqrtAngleSmoothness = Math.Sqrt(angleSmoothnessWeight);
            for (int i = 1; i < angles.Count; i++)
            {
                double previousDelta = WrapPi(angles[i - 1] - restAngles[i - 1]);
                double delta = WrapPi(angles[i] - restAngles[i]);
                r[offset++] = sqrtAngleSmoothness * WrapPi(delta - previousDelta);
            }

            double sqrtScaleSmoothness = Math.Sqrt(scaleSmoothnessWeight);
            for (int i = 1; i < lengths.Count; i++)
            {
                double previousScale = lengths[i - 1] / Math.Max(restLengths[i - 1], Epsilon);
                double scale = lengths[i] / Math.Max(restLengths[i], Epsilon);
                r[offset++] = sqrtScaleSmoothness * (scale - previousScale);
            }

            double sqrtAnchor = Math.Sqrt(positionAnchorWeight);
            for (int i = 0; i < count; i++)
            {
                if (fixedPositions.ContainsKey(i))
                    continue;

                r[offset++] = sqrtAnchor * (xs[i] - positions[i].X);
                r[offset++] = sqrtAnchor * (ys[i] - positions[i].Y);
            }

            return r;
        }

        try
        {
            var observedX = Vector<double>.Build.Dense(residualCount); // unused by Model
            var observedY = Vector<double>.Build.Dense(residualCount);
            var objective = ObjectiveFunction.NonlinearModel(Model, observedX, observedY);
            var solver = new LevenbergMarquardtMinimizer(maximumIterations: maxIterations);
            var solved = solver.FindMinimum(objective, initial).MinimizingPoint;

            for (int k = 0; k < freeIndices.Count; k++)
                result[freeIndices[k]] = new Vector2((float)solved[2 * k], (float)solved[2 * k + 1]);
        }
        catch (Exception e)
        {
            // A degenerate input can make LM fail to converge; fall back to the un-deformed free
            // positions (constraints still honored). Better a slightly-off curve than a crash.
            GD.PushWarning($"PolylineShapeOptimizer fell back: {e.Message}");
        }

        return result;
    }

    private static Vector<double> BuildTurningAngles(double[] xs, double[] ys)
    {
        int count = xs.Length;
        int angleCount = Math.Max(count - 2, 0);
        var r = Vector<double>.Build.Dense(angleCount);

        for (int i = 1; i < count - 1; i++)
        {
            double previousX = xs[i] - xs[i - 1];
            double previousY = ys[i] - ys[i - 1];
            double nextX = xs[i + 1] - xs[i];
            double nextY = ys[i + 1] - ys[i];
            r[i - 1] = TurningAngle(previousX, previousY, nextX, nextY);
        }

        return r;
    }

    private static Vector<double> BuildEdgeLengths(double[] xs, double[] ys)
    {
        int edgeCount = Math.Max(xs.Length - 1, 0);
        var r = Vector<double>.Build.Dense(edgeCount);

        for (int i = 0; i < edgeCount; i++)
        {
            double dx = xs[i + 1] - xs[i];
            double dy = ys[i + 1] - ys[i];
            r[i] = Math.Sqrt(dx * dx + dy * dy);
        }

        return r;
    }

    private static double TurningAngle(double previousX, double previousY, double nextX, double nextY)
    {
        double previousLengthSquared = previousX * previousX + previousY * previousY;
        double nextLengthSquared = nextX * nextX + nextY * nextY;
        if (previousLengthSquared <= Epsilon || nextLengthSquared <= Epsilon)
            return 0.0;

        double cross = previousX * nextY - previousY * nextX;
        double dot = previousX * nextX + previousY * nextY;
        return Math.Atan2(cross, dot);
    }

    private static double WrapPi(double angle)
    {
        while (angle <= -Math.PI)
            angle += 2.0 * Math.PI;
        while (angle > Math.PI)
            angle -= 2.0 * Math.PI;
        return angle;
    }
}
