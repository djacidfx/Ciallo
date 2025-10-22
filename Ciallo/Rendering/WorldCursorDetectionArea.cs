using System;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Misc;
using Godot;
using Godot.Collections;
using R3;

namespace Ciallo.Rendering;

public partial class WorldCursorDetectionArea : Node2D
{
    private CanvasLayer _canvasLayer;
    private Control _cursorSwitcher; // This is supposed to be the job of ViewportContainer, but it doesn't respond even if changing MouseDefaultCursorShape.

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
        area.Position = flags.HasFlag(CursorRectFlags.CornerPosition) ? position + size * 0.5f : position;

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

    public static CursorDetectionArea CreateRect(Vector2 size, Vector2 center)
    {
        var area = new CursorDetectionArea();
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

    private CursorDetectionArea TopMostFromHits(Array<Dictionary> hits)
    {
        return hits
            .Select(d => (CursorDetectionArea)d["collider"])
            .OrderByDescending(GetCanvasLayer)
            .ThenByDescending(n => n.ZIndex)
            .ThenByDescending(GetIndexPath, NodeIndexPathComparer.Instance) // child parent hierarchy
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

        var cornerAreas = new CursorDetectionArea[corners.Length];
        foreach (var (idx, pos) in corners.Index())
        {
            var a = CreateAddRect(dotAreaSize, pos, CursorRectFlags.ScreenSize);
            a.MouseDefaultCursorShape = idx % 2 == 0 ? Control.CursorShape.Fdiagsize : Control.CursorShape.Bdiagsize;
            cornerAreas[idx] = a;
        }

        return [rotation, translation, ..cornerAreas];
    }

    public CursorDetectionArea[] CreateAddTransformAreas(Vector2 size, Vector2 position)
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