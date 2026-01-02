using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Data;
using Ciallo.Misc;
using Frent;
using Godot;
using Godot.Collections;
using R3;

namespace Ciallo.Rendering;

/// <summary>
/// Root node holding all the physical bodies on canvas.
/// </summary>
/// <remarks>
/// Physical bodies (Body class) are used for click detection, and other selection operations on canvas.
/// </remarks>
public partial class WorldBody : Node2D
{
    private CanvasLayer _canvasLayer;
    private Control _cursorSwitcher; // This is supposed to be the job of ViewportContainer, but it doesn't respond even if changing MouseDefaultCursorShape.

    public Control.CursorShape MouseDefaultCursorShape { get; set; }

    private readonly ReactiveProperty<Body> _hoveringArea = new();
    public ReadOnlyReactiveProperty<Body> HoveringArea => _hoveringArea;

    public override void _EnterTree()
    {
        _canvasLayer = GetChild<CanvasLayer>(0);
        _cursorSwitcher = _canvasLayer.GetChild<Control>(0);
    }

    private void SetHoveringArea(Body value)
    {
        _cursorSwitcher.MouseDefaultCursorShape = value?.MouseDefaultCursorShape ?? MouseDefaultCursorShape;
        var area = _hoveringArea.Value;
        if (area == value) return;

        if (area != null) area.IsHovered = false;
        if (value != null) value.IsHovered = true;
        _hoveringArea.Value = value;
    }

    // Note: not implement screen position, world size
    public Body CreateAddRect(Vector2 size, Vector2 position, CursorRectFlags flags = default)
    {
        var area = CreateAddRect(flags);
        area.AddChild(new CollisionShape2D()
        {
            Shape = new RectangleShape2D { Size = size },
        });
        area.Position = flags.HasFlag(CursorRectFlags.CornerPosition) ? position + size * 0.5f : position;

        return area;
    }

    public Body CreateAddRect(Vector2 size, Transform2D transform)
    {
        var area = CreateAddRect();
        area.AddChild(new CollisionShape2D()
        {
            Shape = new RectangleShape2D { Size = size },
            Transform = transform,
        });
        return area;
    }

    public Body CreateAddRect(CursorRectFlags flags = default)
    {
        var area = new Body();

        if (flags.HasFlag(CursorRectFlags.ScreenPosition))
            _canvasLayer.AddChild(area);
        else
            AddChild(area);

        return area;
    }

    public static Body CreateRect(Vector2 size, Vector2 center)
    {
        var area = new Body();
        area.AddChild(new CollisionShape2D()
        {
            Shape = new RectangleShape2D { Size = size },
        });
        area.Position = center;

        return area;
    }

    private static int GetCanvasLayer(Node n)
    {
        while (n is not null && n is not CanvasLayer) n = n.GetParent();
        return (n as CanvasLayer)?.Layer ?? 0;
    }

    private ImmutableArray<int> GetIndexPath(Node n)
    {
        return n.GetIndexPathTo(this);
    }

    private Body TopMostFromHits(Array<Dictionary> hits)
    {
        return hits
            .Select(d => (Body)d["collider"])
            .Distinct()
            .OrderByDescending(GetCanvasLayer)
            .ThenByDescending(n => n.ZIndex)
            .ThenByDescending(GetIndexPath, NodeIndexPathComparer.Instance) // child parent hierarchy
            .ThenByDescending(n => n.GetIndex())
            .First();
    }

    public void UpdateHovering(Vector2 worldPosition)
    {
        // See following pages for the point query method:
        // https://docs.godotengine.org/en/stable/tutorials/physics/ray-casting.html#space
        // https://godotforums.org/d/34175-collision-with-point
        // Note this is different to RayCast2D node, which is a ray on XY plane. We want a top-down cast here (a point on XY plane).
        var pointQuery = new PhysicsPointQueryParameters2D()
        {
            CollideWithBodies = true,
            Position = worldPosition,
            CollisionMask = (uint)AppGodotLayers.Physics2DLayerMask.Stroke,
        };
        var hits = GetWorld2D().DirectSpaceState.IntersectPoint(pointQuery, 32);
        var area = hits.Count > 0 ? TopMostFromHits(hits) : null;

        SetHoveringArea(area);
    }

    public List<Entity> RectQuery(Rect2 rect)
    {
        var rectQuery = new PhysicsShapeQueryParameters2D()
        {
            CollideWithBodies = true,
            Shape = new RectangleShape2D() { Size = rect.Size.Abs() },
            Transform = new Transform2D(0, rect.GetCenter()),
            CollisionMask = (uint)AppGodotLayers.Physics2DLayerMask.Stroke,
        };
        List<Entity> result = [];
        Array<Rid> exclude = [];
        const int maxIteration = 256;
        for (int i = 0; i < maxIteration; i++)
        {
            var hit = GetWorld2D().DirectSpaceState.IntersectShape(rectQuery, 1);
            if (hit.Count == 0) break;
            var body = (Body)hit[0]["collider"];
            result.Add(body.SelfEntity);
            exclude.Add(body.GetRid());
            rectQuery.Exclude = exclude;
        }
        return result;
    }

    public Body[] CreateAddTransformAreas(Vector2 size, Transform2D transform)
    {
        var half = size * 0.5f;
        Vector2[] corners =
        [
            transform * -half,
            transform * new Vector2(-half.X, half.Y),
            transform * half,
            transform * new Vector2(half.X, -half.Y)
        ];
        var dotAreaSize = Vector2.One * 100.0f / 3;

        var barDir = (corners[0] - corners[1]).Normalized();
        var barLength = AppPreference.StrokeDotRadius * 4f;
        var topMid = (corners[0] + corners[3]) * 0.5f;
        Vector2 rotationDotPos = topMid + barLength * barDir;

        var rotation = CreateAddRect(dotAreaSize, rotationDotPos);
        rotation.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        var translation = CreateAddRect(size, transform);
        translation.MouseDefaultCursorShape = Control.CursorShape.Move;

        var cornerAreas = new Body[corners.Length];
        foreach (var (idx, pos) in corners.Index())
        {
            var a = CreateAddRect(dotAreaSize, pos, CursorRectFlags.ScreenSize);
            a.MouseDefaultCursorShape = idx % 2 == 0 ? Control.CursorShape.Fdiagsize : Control.CursorShape.Bdiagsize;
            cornerAreas[idx] = a;
        }

        return [rotation, translation, ..cornerAreas];
    }

    public Body[] CreateAddTransformAreas(Vector2 size, Vector2 position)
    {
        return CreateAddTransformAreas(size, new Transform2D(0, position));
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