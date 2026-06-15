using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using MathNet.Numerics.LinearAlgebra;

namespace Ciallo.Tool;

public readonly record struct PaintStrokeSnapTarget(
    Entity Curve,
    Vector2 HitPoint,
    float HitT);

public sealed class PaintStrokeSnapHintManager : IDisposable
{
    private readonly Node2D _root = new();
    private readonly MultiMeshInstance2D _dots;

    public PaintStrokeSnapHintManager(Node2D parent)
    {
        _dots = AutoloadRendering.CreateDots();
        _root.AddChild(_dots);
        parent.AddChild(_root);
        Hide();
    }

    public void Show(IReadOnlyList<Vector2> points)
    {
        _root.Visible = true;
        _dots.SetDotGeometry(points, AppPreference.StrokeDotRadius);
    }

    public void Hide()
    {
        _root.Visible = false;
    }

    public void Dispose()
    {
        _root.QueueFree();
    }

}

public static class PaintStrokeSnap
{
    private const float Epsilon = 1e-5f;
    private const float BodyTargetOverrunDistanceWorld = 0.1f;
    private const float EndpointPreferenceSnapDistanceRatio = 1f / 3;
    // Laplacian smoothness weight is implicitly 1.0; DisplacementWeight is relative to it.
    private const double DisplacementWeight = 0.08;
    private const double FarDisplacementPenalty = 24.0;

    public static bool TryFindTarget(
        Arrangement arr,
        IReadOnlySet<Entity> sourceShapes,
        Vector2 worldPosition,
        float snapDistance,
        out PaintStrokeSnapTarget target)
    {
        target = default;
        if (arr == null || snapDistance <= 0f)
            return false;

        float snapDistanceSquared = snapDistance * snapDistance;
        float endpointPreferenceDistance = snapDistance * EndpointPreferenceSnapDistanceRatio;
        float endpointPreferenceDistanceSquared = endpointPreferenceDistance * endpointPreferenceDistance;
        var endpointTarget = default(PaintStrokeSnapTarget);
        var bodyTarget = default(PaintStrokeSnapTarget);
        float bestEndpointDistanceSquared = endpointPreferenceDistanceSquared;
        float bestBodyDistanceSquared = snapDistanceSquared;
        bool foundEndpoint = false;
        bool foundBody = false;
        var seen = new HashSet<Entity>();

        foreach (long targetId in arr.PolylineQueryCurves(PolylineShapeBuilder.BuildClosedOctagon(worldPosition, snapDistance)))
        {
            var curve = targetId.ToEntity();
            if (sourceShapes.Contains(curve) && seen.Add(curve))
                KeepIfNearer(curve);
        }

        // Single-point strokes are valid snap targets, but Arrangement curve queries cannot return them.
        foreach (var curve in sourceShapes)
            if (seen.Add(curve) && curve.Get<PolylineGeometry>().Positions.Value.GetBoundingBox().Grow(snapDistance).HasPoint(worldPosition))
                KeepIfNearer(curve);

        target = foundEndpoint ? endpointTarget : bodyTarget;
        return foundEndpoint || foundBody;

        void KeepIfNearer(Entity curve)
        {
            var positions = curve.Get<PolylineGeometry>().Positions.Value;

            // Prefer curve endpoints only in the tighter endpoint-preference range.
            // Body targets can still use the full snap distance.
            float startDistSq = worldPosition.DistanceSquaredTo(positions[0]);
            float endDistSq = worldPosition.DistanceSquaredTo(positions[^1]);
            bool startInRange = startDistSq <= endpointPreferenceDistanceSquared;
            bool endInRange = endDistSq <= endpointPreferenceDistanceSquared;

            if (startInRange || endInRange)
            {
                bool useStart = startInRange && (!endInRange || startDistSq <= endDistSq);
                var endpointPoint = useStart ? positions[0] : positions[^1];
                float endpointT = useStart ? 0f : positions.Length - 1f;
                float endpointDistSq = useStart ? startDistSq : endDistSq;

                if (!foundEndpoint || endpointDistSq <= bestEndpointDistanceSquared)
                {
                    endpointTarget = new PaintStrokeSnapTarget(curve, endpointPoint, endpointT);
                    bestEndpointDistanceSquared = endpointDistSq;
                    foundEndpoint = true;
                }
                return;
            }

            var hitPoint = positions.GetClosestPoint(worldPosition, out var t);
            float distanceSquared = worldPosition.DistanceSquaredTo(hitPoint);
            if (distanceSquared > bestBodyDistanceSquared)
                return;

            bodyTarget = new PaintStrokeSnapTarget(curve, hitPoint, t);
            bestBodyDistanceSquared = distanceSquared;
            foundBody = true;
        }
    }

    public static PaintStrokeGeometry BuildRepairedGeometry(
        PolylineGeneratorGeometry geometry,
        PaintStrokeSnapTarget? startTarget,
        PaintStrokeSnapTarget? endTarget)
    {
        if (geometry.Count == 0)
            return new PaintStrokeGeometry([], [], [], []);

        if (geometry.Count == 1)
        {
            var position = startTarget.HasValue
                ? ResolveRepairPoint(geometry.Positions, startTarget.Value, repairStart: true)
                : endTarget.HasValue
                    ? ResolveRepairPoint(geometry.Positions, endTarget.Value, repairStart: false)
                    : geometry.Positions[0];
            return CreateGeometry(geometry, [position]);
        }

        if (!startTarget.HasValue && !endTarget.HasValue)
            return CreateGeometry(geometry, geometry.Positions);

        var arcLengths = new float[geometry.Count];
        for (int i = 1; i < geometry.Count; i++)
            arcLengths[i] = arcLengths[i - 1] + geometry.Positions[i - 1].DistanceTo(geometry.Positions[i]);

        float totalLength = arcLengths[^1];
        var startDelta = startTarget.HasValue
            ? ResolveRepairPoint(geometry.Positions, startTarget.Value, repairStart: true) - geometry.Positions[0]
            : Vector2.Zero;
        var endDelta = endTarget.HasValue
            ? ResolveRepairPoint(geometry.Positions, endTarget.Value, repairStart: false) - geometry.Positions[^1]
            : Vector2.Zero;

        // Paint Stroke Snap is not smoothing the drawn stroke itself. The user already
        // authored that curve; snap repair only distributes endpoint correction across
        // the new stroke without creating the hard kink produced by the old local ramp.
        //
        // Business requirement:
        // - snapped endpoints must land exactly on the repair point;
        // - nearby points may follow the endpoint so the connection looks intentional;
        // - distant points should move less, preserving the user's stroke away from the snap;
        // - "Snap distance" is only the hit-test radius, not a repair-range cutoff.
        //
        // We solve for displacement \(d_i = x'_i - x_i\), not for raw positions:
        //
        // $$
        // \min_d E(d) =
        // \lambda_{\mathrm{lap}} \sum_i
        // \left\lVert d_{i-1} - 2d_i + d_{i+1} \right\rVert^2
        // +
        // \lambda_{\mathrm{disp}} \sum_i
        // w_i \left\lVert d_i \right\rVert^2
        // $$
        //
        // $$
        // w_i = 1 + \alpha
        // \left(\frac{\operatorname{distAlongStroke}(i,\mathrm{nearestSnap})}{L}\right)^2
        // $$
        //
        // $$
        // d_0 = \mathrm{startRepair} - x_0
        // \quad\text{and/or}\quad
        // d_n = \mathrm{endRepair} - x_n
        // $$
        //
        // The first term makes the displacement field Laplacian-smooth, which keeps
        // local stroke shape changes gradual. The second term is the ramp-like business
        // rule: w_i grows with distance from the snapped endpoint, so far-away points
        // are increasingly expensive to move.
        var repairedPositions = SolveDisplacementLaplacian(
            geometry.Positions,
            arcLengths,
            totalLength,
            startTarget.HasValue,
            startDelta,
            endTarget.HasValue,
            endDelta);

        return CreateGeometry(geometry, repairedPositions);
    }

    private static ImmutableArray<Vector2> SolveDisplacementLaplacian(
        IReadOnlyList<Vector2> positions,
        float[] arcLengths,
        float totalLength,
        bool repairStart,
        Vector2 startDelta,
        bool repairEnd,
        Vector2 endDelta)
    {
        int count = positions.Count;
        var variableByPoint = new int[count];
        Array.Fill(variableByPoint, -1);

        int variableCount = 0;
        for (int i = 0; i < count; i++)
        {
            if ((i == 0 && repairStart) || (i == count - 1 && repairEnd))
                continue;
            variableByPoint[i] = variableCount++;
        }

        // Only the two endpoints exist and both are constrained.
        if (variableCount == 0)
            return [positions[0] + startDelta, positions[^1] + endDelta];

        // X and Y are independent; pack them as two RHS columns and solve in one QR back-substitution.
        int rowCount = (count - 2) + variableCount;
        var matrix = Matrix<double>.Build.Dense(rowCount, variableCount);
        var rhs = Matrix<double>.Build.Dense(rowCount, 2);

        int row = 0;
        for (int i = 1; i < count - 1; i++)
        {
            EmitLaplacianTerm(matrix, rhs, row, variableByPoint, i - 1, 1.0, startDelta, endDelta);
            EmitLaplacianTerm(matrix, rhs, row, variableByPoint, i, -2.0, startDelta, endDelta);
            EmitLaplacianTerm(matrix, rhs, row, variableByPoint, i + 1, 1.0, startDelta, endDelta);
            row++;
        }

        double invStrokeLength = 1.0 / Math.Max(totalLength, Epsilon);
        for (int i = 0; i < count; i++)
        {
            int variable = variableByPoint[i];
            if (variable < 0)
                continue;

            double normalizedDistance = DistanceToNearestSnap(arcLengths, totalLength, repairStart, repairEnd, i) * invStrokeLength;
            matrix[row, variable] = Math.Sqrt(DisplacementWeight * (1.0 + FarDisplacementPenalty * normalizedDistance * normalizedDistance));
            row++;
        }

        var solution = matrix.QR().Solve(rhs);
        var repaired = ImmutableArray.CreateBuilder<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            int v = variableByPoint[i];
            var displacement = v >= 0
                ? new Vector2((float)solution[v, 0], (float)solution[v, 1])
                : i == 0 ? startDelta : endDelta;
            repaired.Add(positions[i] + displacement);
        }

        return repaired.ToImmutable();
    }

    // Writes one (row, point) entry of the Laplacian stencil:
    // - if the point is a free variable, accumulate the coefficient into the matrix;
    // - if the point is a constrained endpoint, fold its known displacement into the RHS.
    private static void EmitLaplacianTerm(
        Matrix<double> matrix,
        Matrix<double> rhs,
        int row,
        int[] variableByPoint,
        int point,
        double coefficient,
        Vector2 startDelta,
        Vector2 endDelta)
    {
        int variable = variableByPoint[point];
        if (variable >= 0)
        {
            matrix[row, variable] += coefficient;
            return;
        }

        var knownDisplacement = point == 0 ? startDelta : endDelta;
        rhs[row, 0] -= coefficient * knownDisplacement.X;
        rhs[row, 1] -= coefficient * knownDisplacement.Y;
    }

    private static double DistanceToNearestSnap(
        float[] arcLengths,
        float totalLength,
        bool repairStart,
        bool repairEnd,
        int index)
    {
        if (repairStart && repairEnd)
            return Math.Min(arcLengths[index], totalLength - arcLengths[index]);
        if (repairStart)
            return arcLengths[index];
        return totalLength - arcLengths[index];
    }

    private static PaintStrokeGeometry CreateGeometry(
        PolylineGeneratorGeometry geometry,
        IReadOnlyList<Vector2> positions)
    {
        var repairedPositions = ImmutableArray.CreateBuilder<Vector2>(geometry.Count);
        var radii = ImmutableArray.CreateBuilder<float>(geometry.Count);
        var pressures = ImmutableArray.CreateBuilder<float>(geometry.Count);
        var tilts = ImmutableArray.CreateBuilder<Vector2>(geometry.Count);

        for (int i = 0; i < geometry.Count; i++)
        {
            repairedPositions.Add(positions[i]);
            radii.Add(geometry.Radii[i]);
            pressures.Add(geometry.Pressures[i]);
            tilts.Add(geometry.Tilts[i]);
        }

        return new PaintStrokeGeometry(
            repairedPositions.ToImmutable(),
            radii.ToImmutable(),
            pressures.ToImmutable(),
            tilts.ToImmutable());
    }

    private static Vector2 ResolveRepairPoint(
        IReadOnlyList<Vector2> strokePositions,
        PaintStrokeSnapTarget target,
        bool repairStart)
    {
        var targetPositions = target.Curve.Get<PolylineGeometry>().Positions.Value;
        var hitPoint = targetPositions.Sample(target.HitT);
        var t = target.HitT;
        if (t <= Epsilon)
            return targetPositions[0];
        if (t >= targetPositions.Length - 1f - Epsilon)
            return targetPositions[^1];

        var outwardTangent = GetEndpointOutwardTangent(strokePositions, repairStart);
        return hitPoint + outwardTangent * BodyTargetOverrunDistanceWorld;
    }

    private static Vector2 GetEndpointOutwardTangent(IReadOnlyList<Vector2> strokePositions, bool fromStart)
    {
        var endpoint = fromStart ? strokePositions[0] : strokePositions[^1];
        int step = fromStart ? 1 : -1;
        for (int i = fromStart ? 1 : strokePositions.Count - 2;
             i >= 0 && i < strokePositions.Count;
             i += step)
        {
            var tangent = endpoint - strokePositions[i];
            if (tangent.LengthSquared() > Epsilon * Epsilon)
                return tangent.Normalized();
        }

        return Vector2.Zero;
    }

}

public readonly record struct PaintStrokeGeometry(
    ImmutableArray<Vector2> Positions,
    ImmutableArray<float> Radii,
    ImmutableArray<float> Pressures,
    ImmutableArray<Vector2> Tilts);
