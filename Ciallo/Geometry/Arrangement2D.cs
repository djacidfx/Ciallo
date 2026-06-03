using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;
using R3;

namespace Ciallo.Geometry;

// Wrapper of CGAL's Arrangement_2 with polyline curves.
public partial class Arrangement : Godot.Arrangement2D
{
    public static readonly int MemoryPerPoint = 200; // byte in very rough estimation
    private readonly System.Collections.Generic.Dictionary<Rid, int> _polylineLengthTracker = new();
    private bool _nativeFreeQueued;

    public readonly Subject<Unit> StructureChanged = new();

    public void SetPolyline(Rid id, ImmutableArray<Vector2> data)
    {
        SetPolyline(id, ImmutableCollectionsMarshal.AsArray(data));
    }

    public Array<Rid> PolylineQuery(ImmutableArray<Vector2> polyline)
    {
        return PolylineQuery(ImmutableCollectionsMarshal.AsArray(polyline));
    }

    # region GDExtension bindings

    public new Rid CreatePolyline()
    {
        var id = base.CreatePolyline();
        _polylineLengthTracker.Add(id, 0);
        return id;
    }

    public new void SetPolyline(Rid id, Vector2[] data)
    {
        SetPolyline(id, data.AsSpan());
    }

    /// <returns>Array of face Rids that are returned in previous queries and invalid since polyline change</returns>
    public new void SetPolyline(Rid id, ReadOnlySpan<Vector2> data)
    {
        if (_polylineLengthTracker[id] > 0)
            GC.RemoveMemoryPressure(MemoryPerPoint * _polylineLengthTracker[id]);
        _polylineLengthTracker[id] = data.Length;
        if (data.Length > 0)
            GC.AddMemoryPressure(MemoryPerPoint * data.Length);
        base.SetPolyline(id, data);
        StructureChanged.OnNext(Unit.Default);
    }

    /// <returns>Array of face Rids that are returned in previous queries and invalid since removing polyline</returns>
    public new void RemovePolyline(Rid id)
    {
        bool toNotify = _polylineLengthTracker[id] != 0;
        if (_polylineLengthTracker[id] > 0)
            GC.RemoveMemoryPressure(MemoryPerPoint * _polylineLengthTracker[id]);
        _polylineLengthTracker.Remove(id);
        base.RemovePolyline(id);
        if (toNotify)
            StructureChanged.OnNext(Unit.Default);
    }

    /// <returns>
    /// Array of polygons
    /// if face is bounded the first polygon is outer rim and others are holes inside.
    /// if face is unbounded all the polygons are holes of the unbounded face.
    /// </returns>
    public Array<Vector2[]> GetFacePolygons(Rid id)
    {
        return GetPolygon(id);
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseMemoryPressure();
        if (!_nativeFreeQueued && NativeInstance != IntPtr.Zero)
        {
            _nativeFreeQueued = true;
            // Native Arrangement2D is an Object, not RefCounted; dispose only disconnects the managed binding.
            CallDeferred(GodotObject.MethodName.Free);
        }
        StructureChanged.Dispose();
        base.Dispose(disposing);
    }

    private void ReleaseMemoryPressure()
    {
        int sum = _polylineLengthTracker.Values.Sum();
        if (sum > 0) GC.RemoveMemoryPressure(sum * MemoryPerPoint);
        _polylineLengthTracker.Clear();
    }

    # endregion
}
