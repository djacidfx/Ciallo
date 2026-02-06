using System;
using System.Linq;
using Godot;
using Godot.Collections;

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
    private readonly System.Collections.Generic.Dictionary<Rid, int> _polylineLengthTracker = new();

    public Rid CreatePolyline()
    {
        var id = (Rid)_obj.Call("create_polyline");
        _polylineLengthTracker.Add(id, 0);
        return id;
    }

    /// <returns>Array of face Rids that are returned in previous queries and invalid since polyline change</returns>
    public Array<Rid> SetPolyline(Rid id, Vector2[] data)
    {
        if (_polylineLengthTracker[id] > 0)
            GC.RemoveMemoryPressure(MemoryPerPoint * _polylineLengthTracker[id]);
        _polylineLengthTracker[id] = data.Length;
        if (data.Length > 0)
            GC.AddMemoryPressure(MemoryPerPoint * data.Length);
        return (Array<Rid>)_obj.Call("set_polyline", id, data);
    }

    /// <returns>Array of face Rids that are returned in previous queries and invalid since removing polyline</returns>
    public Array<Rid> RemovePolyline(Rid id)
    {
        if (_polylineLengthTracker[id] > 0)
            GC.RemoveMemoryPressure(MemoryPerPoint * _polylineLengthTracker[id]);
        _polylineLengthTracker.Remove(id);
        return (Array<Rid>)_obj.Call("remove_polyline", id);
    }

    /// <returns>A face rid</returns>
    public Rid Query(Vector2 point)
    {
        return (Rid)_obj.Call("query", point);
    }

    /// <returns>An array of face rids</returns>
    public Array<Rid> BatchQuery(Vector2[] points)
    {
        return (Array<Rid>)_obj.Call("batch_query", points);
    }

    public Array<Rid> PolylineQuery(Vector2[] polyline)
    {
        return (Array<Rid>)_obj.Call("polyline_query", polyline);
    }

    /// <returns>
    /// Array of polygons
    /// if face is bounded the first polygon is outer rim and others are holes inside.
    /// if face is unbounded all polygons are holes of the unbounded face. 
    /// </returns>
    public Array<Vector2[]> GetPolygon(Rid id)
    {
        return (Array<Vector2[]>)_obj.Call("get_polygon", id);
    }

    public bool IsUnboundedFace(Rid id)
    {
        return (bool)_obj.Call("is_unbounded_face", id);
    }

    public Rid GetUnboundedFace()
    {
        return (Rid)_obj.Call("get_unbounded_face");
    }

    ~Arrangement2D()
    {
        int sum = _polylineLengthTracker.Values.Sum();
        if (sum > 0) GC.RemoveMemoryPressure(sum * MemoryPerPoint);
        _polylineLengthTracker.Clear();
        // Finalizer run in its own thread. Call_deferred makes it run in the main thread
        _obj.CallDeferred("free");
    }
}