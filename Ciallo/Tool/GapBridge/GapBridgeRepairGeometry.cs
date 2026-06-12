using System;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Tool;

public static class GapBridgeRepairGeometry
{
    // Body targets overpass the hit point slightly so visual fills do not leave a hairline gap.
    private const float BodyTargetOverrunDistanceWorld = 0.1f;

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
        var targetPoint = ResolveRepairTarget(candidate);
        var endpointDelta = targetPoint - positions[tailIndices[^1]];
        var repaired = positions.ToBuilder();

        // The accepted displacement model has fixed endpoints and penalizes
        // first/second displacement differences. Its minimum is a linear ramp.
        int segmentCount = tailIndices.Length - 1;
        for (int i = 1; i < tailIndices.Length; i++)
        {
            float u = (float)i / segmentCount;
            repaired[tailIndices[i]] = positions[tailIndices[i]] + endpointDelta * u;
        }
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
