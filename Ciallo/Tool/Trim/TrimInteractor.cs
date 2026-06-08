using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class TrimInteractor : InteractiveSessionBase
{
    public new TrimTool Tool => (TrimTool)base.Tool;

    private const float SnapToleranceWorld = 6f; // world units; ~visual pixels at 1× zoom
    private const float MinKeptBoundsSize = 1.5f; // world units; bbox shorter side filter

    private readonly List<Vector2> _gesture = [];
    private StrokeView _gestureView;

    // Append-only preview node pool keyed by (source entity, fromT, toT) so we never
    // recompute or re-flush a doomed segment that's already been visualized.
    private readonly Dictionary<(Entity, float, float), StrokeView> _doomedPreviews = [];

    private Arrangement _arrSnapshot;
    private HashSet<Entity> _sourceSnapshot;

    public TrimInteractor()
    {
        // Throttle the per-frame work (CGAL query + preview update) to ~60Hz.
        MovingMinInterval = TimeSpan.FromMilliseconds(16.6);
    }

    public override void Start(CursorButtonData data)
    {
        _arrSnapshot = Tool.Arrangement.ArrReady.CurrentValue;
        _sourceSnapshot = [.. Tool.Arrangement.SourceShapes];
        _gesture.Clear();
        _gesture.Add(data.WorldPosition);

        _gestureView = new StrokeView { Material = AutoloadRendering.DashWireframeMaterial };
        Document.Get<WorldOverlay>().AddChild(_gestureView);

        UpdateGestureView();
    }

    // Our system generates that during interactor sessions moving, no change applies to the arrangement.
    public override void Moving(CursorMotionData data)
    {
        _gesture.Add(data.WorldPosition);
        UpdateGestureView();
        RefreshDoomedPreview();
    }

    public override void End(CursorButtonData data)
    {
        // Final query off the full gesture, then commit.
        var hits = QueryHits();
        if (hits.Count > 0)
            CommitTrim(hits);
        Clear();
    }

    public override void Cancel() => Clear();

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    private void UpdateGestureView()
    {
        _gestureView.SetGeometry(_gesture, AppPreference.StrokeWireframeRadius);
    }

    private List<TrimEdgeHit> QueryHits()
    {
        if (_arrSnapshot == null || _gesture.Count < 2) return [];
        var raw = _arrSnapshot.PolylineQueryEdges([.. _gesture]);
        return TrimGeometry.ParseEdgeHits(raw);
    }

    private void RefreshDoomedPreview()
    {
        var hits = QueryHits();
        foreach (var hit in hits)
        {
            var key = (hit.SourceShape, hit.FromT, hit.ToT);
            if (_doomedPreviews.ContainsKey(key)) continue;
            if (!_sourceSnapshot.Contains(hit.SourceShape)) continue;
            if (!hit.SourceShape.IsAlive || !hit.SourceShape.Has<PolylineGeometry>()) continue;

            var geom = hit.SourceShape.Get<PolylineGeometry>();
            var slicePts = TrimGeometry.SliceVec2(geom.Positions.Value, hit.FromT, hit.ToT);
            if (slicePts.Length < 2) continue;

            var preview = new StrokeView { Material = AutoloadRendering.DashWireframeMaterial };
            Document.Get<WorldOverlay>().AddChild(preview);
            preview.SetGeometry(slicePts, AppPreference.StrokeWireframeRadius);
            _doomedPreviews[key] = preview;
        }
    }

    private void Clear()
    {
        _gesture.Clear();
        _gestureView?.QueueFree();
        _gestureView = null;
        foreach (var view in _doomedPreviews.Values) view.QueueFree();
        _doomedPreviews.Clear();
        _arrSnapshot = null;
        _sourceSnapshot = null;
    }

    private void CommitTrim(List<TrimEdgeHit> hits)
    {
        // Process sources by descending layer index so each AddToLayerTreeCmd's static
        // insertion index stays valid: mutations at higher indices don't shift lower ones.
        var groups = hits
            .Where(h => _sourceSnapshot.Contains(h.SourceShape))
            .Where(h => h.SourceShape.IsAlive && h.SourceShape.Has<PolylineGeometry>())
            .GroupBy(h => h.SourceShape)
            .Select(g =>
            {
                var sourceLayer = g.Key.Get<LayerTreeNode>().ParentValue;
                var sourceLayerNode = sourceLayer.Get<LayerTreeNode>();
                return new
                {
                    SourceE = g.Key,
                    Hits = g.ToList(),
                    SourceLayer = sourceLayer,
                    SourceLayerNode = sourceLayerNode,
                    Index = sourceLayerNode.Children.IndexOf(g.Key),
                };
            })
            .Where(x => x.Index >= 0)
            .GroupBy(x => x.SourceLayer)
            .OrderByDescending(layerGroup => layerGroup.Key.PackedValue)
            .SelectMany(layerGroup => layerGroup.OrderByDescending(x => x.Index))
            .ToArray();

        var snapCandidates = _sourceSnapshot.ToArray();

        var cmd = new CommandBuilder("Trim");
        bool any = false;

        foreach (var entry in groups)
        {
            var sourceE = entry.SourceE;
            var geom = sourceE.Get<PolylineGeometry>();
            int n = geom.Positions.Value.Length;
            if (n < 2) continue;

            var keptRanges = TrimGeometry.InvertDoomedRanges(entry.Hits, n);
            int originalIndex = entry.Index;

            cmd.SetTarget(sourceE).RemoveFromLayerTree().DeleteShape();
            any = true;

            int insertOffset = 0;
            foreach (var (from, to) in keptRanges)
            {
                if (to - from < 1e-4f) continue; // zero-length kept piece

                var pieceGeom = SliceGeometry(geom, from, to);
                if (pieceGeom.positions.Length < 2) continue;

                // Tolerance snap each interior endpoint to nearby strokes.
                var snappedPositions = pieceGeom.positions;
                bool fromInterior = from > 1e-4f;
                bool toInterior = to < (n - 1) - 1e-4f;
                if (fromInterior)
                    snappedPositions = SnapEndpoint(snappedPositions, isFromEnd: true, sourceE, snapCandidates);
                if (toInterior)
                    snappedPositions = SnapEndpoint(snappedPositions, isFromEnd: false, sourceE, snapCandidates);

                if (BoundsTooSmall(snappedPositions)) continue;

                var newE = WorkingLayer.World.Create();
                cmd.SetTarget(newE)
                    .NewStroke(sourceE)
                    .AddToLayerTree(entry.SourceLayer, originalIndex + insertOffset)
                    .SetPolylineGeometry(
                        snappedPositions,
                        pieceGeom.radii,
                        pieceGeom.pressures,
                        pieceGeom.tilts);
                insertOffset++;
            }
        }

        if (any) cmd.Commit();
    }

    private static (
        ImmutableArray<Vector2> positions,
        ImmutableArray<float> radii,
        ImmutableArray<float> pressures,
        ImmutableArray<Vector2> tilts) SliceGeometry(PolylineGeometry geom, float from, float to)
    {
        var pos = TrimGeometry.SliceVec2(geom.Positions.Value, from, to);
        var rad = geom.Radii.Value.Length == geom.Positions.Value.Length
            ? TrimGeometry.SliceFloat(geom.Radii.Value, from, to)
            : geom.Radii.Value;
        var pr = geom.Pressures.Value.Length == geom.Positions.Value.Length
            ? TrimGeometry.SliceFloat(geom.Pressures.Value, from, to)
            : geom.Pressures.Value;
        var ti = geom.Tilts.Value.Length == geom.Positions.Value.Length
            ? TrimGeometry.SliceVec2(geom.Tilts.Value, from, to)
            : geom.Tilts.Value;
        return (pos, rad, pr, ti);
    }

    private static ImmutableArray<Vector2> SnapEndpoint(
        ImmutableArray<Vector2> piecePositions,
        bool isFromEnd,
        Entity sourceE,
        IReadOnlyList<Entity> candidates)
    {
        if (piecePositions.Length < 2) return piecePositions;
        Vector2 endpoint = isFromEnd ? piecePositions[0] : piecePositions[^1];
        Vector2 inside = isFromEnd ? piecePositions[1] : piecePositions[^2];
        Vector2 keepDir = (endpoint - inside).Normalized();

        Vector2 best = endpoint;
        float bestDistSq = SnapToleranceWorld * SnapToleranceWorld;

        foreach (var other in candidates)
        {
            if (other == sourceE) continue;
            if (!other.IsAlive || !other.Has<PolylineGeometry>()) continue;
            var otherPos = other.Get<PolylineGeometry>().Positions.Value;
            if (otherPos.Length < 2) continue;

            // Coarse bbox prune.
            var bbox = otherPos.GetBoundingBox().Grow(SnapToleranceWorld);
            if (!bbox.HasPoint(endpoint)) continue;

            var closest = otherPos.GetClosestPoint(endpoint, out _);
            float distSq = closest.DistanceSquaredTo(endpoint);
            if (distSq >= bestDistSq) continue;

            // Direction check: the snap target must lie on the kept side, not behind the trimmed
            // tail. Project (closest - inside) onto keepDir; positive means same side as the
            // existing kept endpoint.
            if ((closest - inside).Dot(keepDir) <= 0) continue;

            bestDistSq = distSq;
            best = closest;
        }

        if (best == endpoint) return piecePositions;

        var builder = piecePositions.ToBuilder();
        if (isFromEnd) builder[0] = best;
        else builder[^1] = best;
        return builder.ToImmutable();
    }

    private static bool BoundsTooSmall(ImmutableArray<Vector2> positions)
    {
        if (positions.Length < 2) return true;
        var b = positions.GetBoundingBox();
        return b.Size.X < MinKeptBoundsSize && b.Size.Y < MinKeptBoundsSize;
    }
}
