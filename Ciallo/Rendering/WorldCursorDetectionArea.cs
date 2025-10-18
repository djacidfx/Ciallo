using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;
using Godot.Collections;
using R3;

namespace Ciallo.Rendering;

public partial class WorldCursorDetectionArea : Node2D
{
    private CanvasLayer _canvasLayer;
    private Control _cursorSwitcher; // This is supposed to be the job of ViewportContainer, but it doesn't reponse even if changing MouseDefaultCursorShape.

    public Control.CursorShape MouseDefaultCursorShape { get; set; }

    private readonly ReactiveProperty<CursorDetectionArea> _hoveringArea = new();
    public ReadOnlyReactiveProperty<CursorDetectionArea> HoveringArea => _hoveringArea;

    private void SetHoveringArea(CursorDetectionArea value)
    {
        _cursorSwitcher.MouseDefaultCursorShape = value?.MouseDefaultCursorShape ?? MouseDefaultCursorShape;
        var area = _hoveringArea.Value;
        if (area == value) return;

        if (area != null) area.IsHovered = false;
        if (value != null) value.IsHovered = true;
        _hoveringArea.Value = value;
    }

    public override void _EnterTree()
    {
        _canvasLayer = GetChild<CanvasLayer>(0);
        _cursorSwitcher = _canvasLayer.GetChild<Control>(0);
    }

    // Note: not implement screen position, world size
    public CursorDetectionArea CreateAddRect(Vector2 size, Vector2 position, CursorRectFlags flags = default)
    {
        var area = CreateAddRect(flags);
        area.AddChild(new CollisionShape2D()
        {
            Shape = new RectangleShape2D { Size = size },
        });
        area.Position = flags.HasFlag(CursorRectFlags.CornerPosition) ? position - size * 0.5f : position;

        return area;
    }

    public CursorDetectionArea CreateAddRect(Vector2 size, Transform2D transform)
    {
        var area = CreateAddRect();
        area.AddChild(new CollisionShape2D()
        {
            Shape = new RectangleShape2D { Size = size },
            Transform = transform,
        });
        return area;
    }

    public CursorDetectionArea CreateAddRect(CursorRectFlags flags = default)
    {
        var area = new CursorDetectionArea();

        if (flags.HasFlag(CursorRectFlags.ScreenPosition))
            _canvasLayer.AddChild(area);
        else
            AddChild(area);

        return area;
    }

    public static CursorDetectionArea CreateRect(Vector2 size, Vector2 position)
    {
        var area = new CursorDetectionArea();
        area.AddChild(new CollisionShape2D()
        {
            Shape = new RectangleShape2D { Size = size },
        });
        area.Position = position;

        return area;
    }

    public static CursorDetectionArea CreateRect(Vector2 position, float size)
    {
        return CreateRect(new Vector2(size, size), position);
    }

    public static CursorDetectionArea CreateStroke(IReadOnlyList<Vector2> points, IReadOnlyList<float> radii)
    {
        if (points.Count == 1) return CreateRect(points[0], radii[0] * 2);
        if (points.Count != radii.Count) throw new ArgumentException("Points and radii count mismatch");

        var area = new CursorDetectionArea();
        for (var i = 0; i < points.Count - 1; i++)
        {
            var r0 = radii[i];
            var r1 = radii[i + 1];
            var p0 = points[i];
            var p1 = points[i + 1];
            var tangent = (p1 - p0).Normalized();
            var normal = tangent.Rotated(Mathf.Pi / 2);
            var vertices = new Vector2[4];
            vertices[0] = p0 + (-tangent - normal) * r0;
            vertices[1] = p1 + (tangent - normal) * r1;
            vertices[2] = p1 + (tangent + normal) * r1;
            vertices[3] = p0 + (-tangent + normal) * r0;

            var shape = PhysicsServer2D.ConvexPolygonShapeCreate();
            PhysicsServer2D.ShapeSetData(shape, vertices);
            area.AddShapeRid(shape);
        }

        return area;
    }

    private static int GetCanvasLayer(Node n)
    {
        while (n is not null && n is not CanvasLayer) n = n.GetParent();
        return (n as CanvasLayer)?.Layer ?? 0;
    }

    private static CursorDetectionArea TopMostFromHits(Array<Dictionary> hits)
    {
        return hits
            .Select(d => (CursorDetectionArea)d["collider"])
            .OrderByDescending(GetCanvasLayer)
            .ThenByDescending(n => n.ZIndex)
            .ThenByDescending(n => n.GetIndex())
            .First();
    }

    public void OnCursorMove(CursorMotionData data)
    {
        // See following pages for the point query method:
        // https://docs.godotengine.org/en/stable/tutorials/physics/ray-casting.html
        // https://godotforums.org/d/34175-collision-with-point
        // Note this is different to RayCast2D node, which is a ray on XY plane. We want a top-down cast here (a point on XY plane).
        var pp = new PhysicsPointQueryParameters2D()
        {
            CollideWithBodies = true,
            Position = data.WorldPosition,
            CollisionMask = (uint)AppGodotLayers.Physics2DLayerMask.Stroke,
        };
        var points = GetWorld2D().DirectSpaceState.IntersectPoint(pp, 32);
        SetHoveringArea(points.Count > 0 ? TopMostFromHits(points) : null);
    }
    public CursorDetectionArea[] CreateAddTransformAreas(Vector2 size, Transform2D transform)
    {
        var rotation = CreateAddRect(size, transform.ScaledLocal(new(1.2f, 1.2f)));
        rotation.MouseDefaultCursorShape = Control.CursorShape.Drag;
        var translation = CreateAddRect(size, transform);
        translation.MouseDefaultCursorShape = Control.CursorShape.Move;

        var half = size * 0.5f;
        Vector2[] cornerPos =
        [
            transform * -half,
            transform * new Vector2(half.X, -half.Y),
            transform * half,
            transform * new Vector2(-half.X, half.Y)
        ];
        var corners = new CursorDetectionArea[cornerPos.Length];
        foreach (var (idx, pos) in cornerPos.Index())
        {
            var a = CreateAddRect(Vector2.One * 100.0f / 3, pos, CursorRectFlags.ScreenSize);
            a.MouseDefaultCursorShape = idx % 2 == 0 ? Control.CursorShape.Fdiagsize : Control.CursorShape.Bdiagsize;
            corners[idx] = a;
        }

        return [rotation, translation, ..corners];
    }
}

[Flags]
public enum CursorRectFlags
{
    None = 0,

    /// <summary>Interpret the position as screen coordinates.</summary>
    ScreenPosition = 1 << 0,

    /// <summary>Interpret the size as a screen pixel measurement.</summary>
    ScreenSize = 1 << 1,

    /// <summary>Given position is the upper left corner of a button.</summary>
    CornerPosition = 1 << 2
}