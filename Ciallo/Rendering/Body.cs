using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Frent;
using Frent.Components;
using Godot;

namespace Ciallo.Rendering;

public partial class Body : StaticBody2D, IInitable, IDestroyable
{
    private Control.CursorShape _mouseDefaultCursorShape = Control.CursorShape.Arrow;
    private List<Rid> _shapes = [];

    public bool IsHovered { get; set; } // Should only be set by manager
    public Entity SelfEntity;

    public Control.CursorShape MouseDefaultCursorShape
    {
        get => _mouseDefaultCursorShape;
        set => _mouseDefaultCursorShape = value;
    }

    public Body()
    {
        CollisionLayer = AppGodotLayers.Physics2D.Stroke;
        CollisionMask = AppGodotLayers.Physics2D.Empty; // Only detect mouse input, don't collide with anything else
        InputPickable = true;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            _shapes.ForEach(PhysicsServer2D.FreeRid);
            _shapes.Clear();
        }
    }

    public void ClearShapes()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        // Pitfall: calling QueueFree on child shapes and clearing the body's shapes at the same time will cause index error.
        // It seems we don't have to remove shape first before calling FreeRid.
        _shapes.ForEach(PhysicsServer2D.FreeRid);
        _shapes.Clear();
    }

    #region Set shape

    public void SetStrokeShape(IReadOnlyList<Vector2> points, IReadOnlyList<float> radii)
    {
        ClearShapes();
        if (points.Count == 1)
        {
            var shape = new CollisionShape2D()
            {
                Shape = new CircleShape2D { Radius = radii[0] },
                Position = points[0],
            };
            AddChild(shape);
            return;
        }
        if (points.Count != radii.Count) throw new ArgumentException("Positions and radii count mismatch");

        for (var i = 0; i < points.Count - 1; i++)
        {
            var r0 = radii[i];
            var r1 = radii[i + 1];
            var p0 = points[i];
            var p1 = points[i + 1];
            if (p0.IsEqualApprox(p1)) continue;
            var tangent = (p1 - p0).Normalized();
            var normal = tangent.Rotated(Mathf.Pi / 2);
            var vertices = new Vector2[4];
            vertices[0] = p0 + (-tangent - normal) * r0;
            vertices[1] = p1 + (tangent - normal) * r1;
            vertices[2] = p1 + (tangent + normal) * r1;
            vertices[3] = p0 + (-tangent + normal) * r0;

            var shapeRid = PhysicsServer2D.ConvexPolygonShapeCreate();
            PhysicsServer2D.ShapeSetData(shapeRid, vertices);
            _shapes.Add(shapeRid);
            PhysicsServer2D.BodyAddShape(GetRid(), shapeRid);
        }
    }

    public void SetSimplePolygon(IReadOnlyList<Vector2> points)
    {
        ClearShapes();

        AddChild(new CollisionPolygon2D()
        {
            Polygon = [..points],
        });
    }

    public void SetStrokeCenterline(IReadOnlyList<Vector2> points, float radius)
    {
        ClearShapes();

        if (points.Count == 1)
        {
            var shape = new CollisionShape2D()
            {
                Shape = new CircleShape2D { Radius = radius },
                Position = points[0],
            };
            AddChild(shape);
            return;
        }

        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[i];
            var p1 = points[i + 1];
            if (p0.IsEqualApprox(p1)) continue;

            var shapeRid = PhysicsServer2D.CapsuleShapeCreate();
            PhysicsServer2D.ShapeSetData(shapeRid, new Vector2((p1 - p0).Length(), radius));

            Transform2D transform = Transform.Rotated((p1 - p0).Angle()).Translated((p0 + p1) / 2);
            _shapes.Add(shapeRid);
            PhysicsServer2D.BodyAddShape(GetRid(), shapeRid, transform);
        }
    }

    public void UpdateStrokeCenterlineShape(IReadOnlyList<Vector2> points, float radius)
    {
        foreach (var (i, rid) in _shapes.Index())
        {
            var p0 = points[i];
            var p1 = points[i + 1];
            PhysicsServer2D.ShapeSetData(rid, new Vector2((p1 - p0).Length(), radius));
        }
    }

    #endregion


    // Note: If there is a button overlay on world, the Body's mouse entered/exited events won't work.

    public void Init(Entity self) => SelfEntity = self;
    public void Destroy() => SelfEntity = Entity.Null;
}