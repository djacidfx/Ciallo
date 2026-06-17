using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// One detected gap bridge: move <see cref="SourceCurve"/>'s start or end endpoint so it
/// lands exactly on <see cref="TargetPoint"/>, closing a visible gap.
/// </summary>
/// <remarks>
/// Detection (<see cref="GapBridgeDetector"/>) already resolves the final landing point, so
/// consumers never re-sample curve geometry: <see cref="SourcePoint"/>/<see cref="TargetPoint"/>
/// are world positions, <see cref="TargetIsBody"/> says whether the landing is on a stroke body
/// (repair must overrun it) rather than an endpoint, and <see cref="GapDistanceSquared"/> is the
/// gap length used to break ties between overlapping bridges under the cursor.
/// </remarks>
public readonly record struct GapBridge(
    Entity SourceCurve,
    bool RepairStart,
    Vector2 SourcePoint,
    Vector2 TargetPoint,
    bool TargetIsBody,
    float GapDistanceSquared)
{
    // Two-point preview segment. Only the renderer needs the array form; picking tests the
    // segment directly to stay allocation-free on every cursor move.
    public ImmutableArray<Vector2> Polyline => [SourcePoint, TargetPoint];

    /// <summary>
    /// Pick the bridge whose preview segment is nearest to <paramref name="worldPosition"/>
    /// within <paramref name="hitRadius"/>. Ties on hit distance break toward the shorter gap.
    /// </summary>
    public static bool TryPickNearest(
        IReadOnlyList<GapBridge> bridges,
        Vector2 worldPosition,
        float hitRadius,
        out GapBridge bridge)
    {
        bridge = default;
        if (bridges.Count == 0 || hitRadius <= 0f)
            return false;

        float bestDistanceSquared = hitRadius * hitRadius;
        float bestGapDistanceSquared = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < bridges.Count; i++)
        {
            var candidate = bridges[i];
            var bounds = new Rect2(candidate.SourcePoint, Vector2.Zero)
                .Expand(candidate.TargetPoint)
                .Grow(hitRadius);
            if (!bounds.HasPoint(worldPosition))
                continue;

            float distanceSquared = worldPosition.DistanceSquaredToSegment(candidate.SourcePoint, candidate.TargetPoint);
            if (distanceSquared > bestDistanceSquared)
                continue;

            if (!found ||
                distanceSquared < bestDistanceSquared - 1e-4f ||
                (Mathf.IsEqualApprox(distanceSquared, bestDistanceSquared) &&
                 candidate.GapDistanceSquared < bestGapDistanceSquared))
            {
                bridge = candidate;
                bestDistanceSquared = distanceSquared;
                bestGapDistanceSquared = candidate.GapDistanceSquared;
                found = true;
            }
        }
        return found;
    }
}
