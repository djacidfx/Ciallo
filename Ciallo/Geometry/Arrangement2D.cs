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

// Wrapper of CGAL's Arrangement_2 with polyline curves.
// Need manually call Dispose(), don't call Free() directly.
public partial class Arrangement : Arrangement2D
{
    public void SetPolyline(long id, ImmutableArray<Vector2> data)
    {
        SetPolyline(id, ImmutableCollectionsMarshal.AsArray(data));
    }

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
