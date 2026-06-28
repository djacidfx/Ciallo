using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Frent;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Ciallo.Geometry;

public readonly record struct CurveEndpointInfo(
    bool StartDangling,
    bool EndDangling,
    float StartJunctionLength,
    float EndJunctionLength);

public readonly record struct PolylineEdgeHit(Entity SourceShape, float FromT, float ToT);

// A single intersection between a query polyline and a source curve already in the arrangement.
// QueryT / SourceT use SampledPolyline.Sample(t) semantics (segment index + in-segment fraction):
// QueryT indexes the polyline passed to the query, SourceT indexes SourceShape's own polyline.
public readonly record struct PolylineCurveIntersection(
    Entity SourceShape,
    float QueryT,
    float SourceT,
    Vector2 Position);

// Wrapper of CGAL's Arrangement_2 with polyline curves.
// Need manually call Dispose(), don't call Free() directly.
public partial class Arrangement : Arrangement2D
{
    public void SetPolyline(long id, ImmutableArray<Vector2> data)
    {
        SetPolyline(id, ImmutableCollectionsMarshal.AsArray(data));
    }

    // Note: this function not return real halfedges (x_mono_curves) t range in arrangement_2, it connects real halfedges at vertices degree 2.  
    public PolylineEdgeHit[] PolylineQueryEdges(ImmutableArray<Vector2> polyline)
    {
        var raw = PolylineQueryEdges(ImmutableCollectionsMarshal.AsArray(polyline));
        var hits = new PolylineEdgeHit[raw.Count];
        for (int i = 0; i < raw.Count; i++)
        {
            var dict = (GodotDictionary)raw[i];
            hits[i] = new PolylineEdgeHit(
                ((long)dict["source_id"]).ToEntity(),
                (float)dict["from_t"],
                (float)dict["to_t"]);
        }
        return hits;
    }

    public long[] PolylineQueryCurves(ImmutableArray<Vector2> polyline)
    {
        return PolylineQueryCurves(ImmutableCollectionsMarshal.AsArray(polyline));
    }

    // Returns one record per intersection point between the query polyline and any source curve in
    // the arrangement (the query polyline itself is NOT added to the arrangement). An overlap
    // (parallel coincident run) is reported as its two endpoints. Endpoints touching a curve count
    // as intersections too; callers filter by QueryT/SourceShape as needed.
    public PolylineCurveIntersection[] PolylineQueryCurveIntersections(ImmutableArray<Vector2> polyline)
    {
        var raw = PolylineQueryCurveIntersections(ImmutableCollectionsMarshal.AsArray(polyline));
        var hits = new PolylineCurveIntersection[raw.Count];
        for (int i = 0; i < raw.Count; i++)
        {
            var dict = (GodotDictionary)raw[i];
            hits[i] = new PolylineCurveIntersection(
                ((long)dict["source_id"]).ToEntity(),
                (float)dict["query_t"],
                (float)dict["source_t"],
                (Vector2)dict["position"]);
        }
        return hits;
    }

    public new CurveEndpointInfo GetCurveEndpointInfo(long curveId)
    {
        var dict = (GodotDictionary)Call("get_curve_endpoint_info", curveId);
        return new CurveEndpointInfo(
            (bool)dict["start_dangling"],
            (bool)dict["end_dangling"],
            (float)dict["start_junction_length"],
            (float)dict["end_junction_length"]);
    }

    protected override void Dispose(bool disposing)
    {
        CallDeferred(GodotObject.MethodName.Free);
        base.Dispose(disposing);
    }
}
