using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;
using R3;

namespace Ciallo.Geometry;

// Wrapper of CGAL's Arrangement_2 with polyline curves.
// Need manually call Dispose(), don't call Free() directly.
public partial class Arrangement : Godot.Arrangement2D
{
    public void SetPolyline(Rid id, ImmutableArray<Vector2> data)
    {
        SetPolyline(id, ImmutableCollectionsMarshal.AsArray(data));
    }

    public Array<Dictionary> PolylineQueryEdges(ImmutableArray<Vector2> polyline)
    {
        return PolylineQueryEdges(ImmutableCollectionsMarshal.AsArray(polyline));
    }

    protected override void Dispose(bool disposing)
    {
        CallDeferred(GodotObject.MethodName.Free);
        base.Dispose(disposing);
    }
}
