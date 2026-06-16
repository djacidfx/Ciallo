using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Tool;

public static class GapBridgeRepairGeometry
{
    // Body targets overpass the hit point slightly so visual fills do not leave a hairline gap.
    private const float BodyTargetOverrunDistanceWorld = 0.1f;

    // Reuse Paint Stroke Snap's solver tuning: smoothness weight is implicitly 1.0 and these
    // are relative to it. See PolylineExtension.SolveDisplacementLaplacian.
    private const double DisplacementWeight = 0.08;
    private const double FarDisplacementPenalty = 24.0;

    public static ImmutableArray<Vector2> BuildRepairedPositions(Arrangement arr, GapBridgeCandidate candidate)
    {
        var geom = candidate.FromCurve.Get<PolylineGeometry>();
        var positions = geom.Positions.Value;
        if (positions.Length < 2)
            return positions;
        var endpointInfo = arr.GetCurveEndpointInfo(candidate.FromCurve.PackedValue);
        bool repairStart = SourceEndpointIsStart(candidate.FromT, positions.Length - 1f);
        float junctionLength = repairStart
            ? endpointInfo.StartJunctionLength
            : endpointInfo.EndJunctionLength;
        var tailIndices = BuildTailIndices(positions, repairStart, junctionLength);

        // tailIndices runs anchor -> endpoint. The junction-side anchor is a fixed boundary
        // (displacement zero) and the dangling endpoint lands exactly on the resolved target.
        // Same displacement-Laplacian model as Paint Stroke Snap, but the anchor is only a
        // boundary, not a snap source: the lone penalty origin is the moving endpoint, so the
        // correction concentrates there and fades toward the anchor.
        int endpointLocal = tailIndices.Length - 1;
        var targetPoint = ResolveRepairTarget(candidate);
        var tailPositions = new Vector2[tailIndices.Length];
        for (int i = 0; i < tailIndices.Length; i++)
            tailPositions[i] = positions[tailIndices[i]];

        var fixedDisplacements = new Dictionary<int, Vector2>(2)
        {
            [0] = Vector2.Zero,
            [endpointLocal] = targetPoint - tailPositions[endpointLocal],
        };

        var solved = PolylineExtension.SolveDisplacementLaplacian(
            tailPositions,
            fixedDisplacements,
            [endpointLocal],
            DisplacementWeight,
            FarDisplacementPenalty);

        var repaired = positions.ToBuilder();
        for (int i = 0; i < tailIndices.Length; i++)
            repaired[tailIndices[i]] = solved[i];
        return repaired.ToImmutable();
    }

    private static Vector2 ResolveRepairTarget(GapBridgeCandidate candidate)
    {
        var (fromPoint, toPoint) = GapBridgeGeometry.ResolveCandidate(candidate);
        if (candidate.TargetKind != GapBridgeTargetKind.Body)
            return toPoint;

        var repairDirection = (toPoint - fromPoint).Normalized();
        return toPoint + repairDirection * BodyTargetOverrunDistanceWorld;
    }

    private static bool SourceEndpointIsStart(float sourceT, float lastT)
    {
        if (sourceT == 0f)
            return true;
        if (sourceT == lastT)
            return false;
        throw new InvalidOperationException($"Gap Bridge repair source must be an endpoint t, got {sourceT}.");
    }

    private static ImmutableArray<int> BuildTailIndices(
        ImmutableArray<Vector2> positions,
        bool repairStart,
        float junctionLength)
    {
        float endpointT = repairStart ? 0f : positions.Length - 1f;
        float junctionT = positions.MoveTByDistance(endpointT, junctionLength, forward: repairStart);
        int anchorIndex = repairStart
            ? Math.Clamp((int)MathF.Ceiling(junctionT), 1, positions.Length - 1)
            : Math.Clamp((int)MathF.Floor(junctionT), 0, positions.Length - 2);

        var builder = ImmutableArray.CreateBuilder<int>(
            repairStart ? anchorIndex + 1 : positions.Length - anchorIndex);
        if (repairStart)
        {
            for (int i = anchorIndex; i >= 0; i--)
                builder.Add(i);
        }
        else
        {
            for (int i = anchorIndex; i < positions.Length; i++)
                builder.Add(i);
        }
        return builder.ToImmutable();
    }

}
