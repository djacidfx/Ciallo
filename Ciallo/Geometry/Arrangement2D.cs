using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Array = Godot.Collections.Array;

namespace Ciallo.Geometry;

/// <summary>
/// The GDExtension binding class to call CGAL 2D arrangement
/// Arrangement2D.h and .cpp in GDExtension for low-level CGAL code
/// ArrangementManager.cs for high-level management
/// </summary>
public class Arrangement2D
{
    private readonly GodotObject _obj = (GodotObject)ClassDB.Instantiate("Arrangement2D");
    public static readonly int MemoryPerPoint = 200; // byte in very rough estimation
    private readonly Dictionary<Rid, int> _polylineLengthTracker = new();

    public Rid CreatePolyline()
    {
        var id = (Rid)_obj.Call("create_polyline");
        _polylineLengthTracker.Add(id, 0);
        return id;
    }

    /// <returns>Array of face Rids that are returned in previous queries and invalid since polyline change</returns>
    public Array SetPolyline(Rid id, Vector2[] data)
    {
        GC.RemoveMemoryPressure(MemoryPerPoint * _polylineLengthTracker[id]);
        _polylineLengthTracker[id] = data.Length;
        GC.AddMemoryPressure(MemoryPerPoint * data.Length);
        return (Array)_obj.Call("set_polyline", id, data);
    }

    /// <returns>Array of face Rids that are returned in previous queries and invalid since removing polyline</returns>
    public Array RemovePolyline(Rid id)
    {
        GC.RemoveMemoryPressure(MemoryPerPoint * _polylineLengthTracker[id]);
        _polylineLengthTracker.Remove(id);
        return (Array)_obj.Call("remove_polyline", id);
    }

    /// <returns>A face rid</returns>
    public Rid Query(Vector2 point)
    {
        return (Rid)_obj.Call("query", point);
    }

    /// <returns>An array of face rids</returns>
    public Array BatchQuery(Vector2[] points)
    {
        return (Array)_obj.Call("batch_query", points);
    }

    public Array PolylineQuery(Vector2[] polyline)
    {
        return (Array)_obj.Call("polyline_query", polyline);
    }

    public Vector2[] FaceGetPolygon(Rid id)
    {
        return (Vector2[])_obj.Call("face_get_polygon", id);
    }

    public bool FaceIsUnbounded(Rid id)
    {
        return (bool)_obj.Call("face_is_unbounded", id);
    }

    ~Arrangement2D()
    {
        GC.RemoveMemoryPressure(_polylineLengthTracker.Values.Sum() * MemoryPerPoint);
        _polylineLengthTracker.Clear();
        // Finalizer run in its own thread. Call_deferred makes it run in the main thread
        _obj.CallDeferred("free");
    }
}