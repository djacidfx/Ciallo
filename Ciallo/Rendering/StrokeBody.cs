using System.Collections.Generic;
using Ciallo.Command;
using Godot;

namespace Ciallo.Rendering;

public partial class StrokeBody : StaticBody2D
{
    private readonly List<Rid> _shapes = [];
    
    public override void _Ready()
    {
        CollisionLayer = AppLayers.Physics2D.Stroke;
        CollisionMask = AppLayers.Physics2D.Empty; // Only detect mouse input
        InputPickable = true;
    }
    
    public void UpdateGeometry(IReadOnlyList<Vector2> points, IReadOnlyList<float> radii)
    {
        ClearFree();
        var vertices = new Vector2[4];
        for (var i = 0; i < points.Count - 1; i++)
        {
            var r0 = radii[i];
            var r1 = radii[i + 1];
            var p0 = points[i];
            var p1 = points[i + 1];
            var segmentLength = (p1 - p0).Length();
            vertices[0] = p0 + new Vector2(-1, -1) * r0;
            vertices[1] = p1 + new Vector2(1, -1) * r1;
            vertices[2] = p1 + new Vector2(1, 1) * r1;
            vertices[3] = p0 + new Vector2(-1, 1) * r0;
            
            var shape = PhysicsServer2D.ConvexPolygonShapeCreate();
            PhysicsServer2D.ShapeSetData(shape, vertices);
            _shapes.Add(shape);
            PhysicsServer2D.BodyAddShape(GetRid(), shape);
        }
    }

    public override void _ExitTree()
    {
        ClearFree();
    }

    public void ClearFree()
    {
        PhysicsServer2D.BodyClearShapes(GetRid());
        _shapes.ForEach(PhysicsServer2D.FreeRid);
        _shapes.Clear();
    }
}