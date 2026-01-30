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

    public Rid CreatePolyline()
    {
        return (Rid)_obj.Call("create_polyline");
    }
    
    /// <returns>Array of face Rids that are returned in previous queries and invalid since polyline change</returns>
    public Array SetPolyline(Rid id, Vector2[] data)
    {
        return (Array)_obj.Call("set_polyline", id, data);
    }

    /// <returns>Array of face Rids that are returned in previous queries and invalid since removing polyline</returns>
    public Array RemovePolyline(Rid id)
    {
        return (Array)_obj.Call("remove_polyline", id);
    }

    public Rid Query(Vector2 point)
    {
        return (Rid)_obj.Call("query", point);
    }

    public Array BatchQuery(Vector2[] points)
    {
        return (Array)_obj.Call("batch_query", points);
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
        _obj.Free();
    }
}