using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace Ciallo.Rendering;

public partial class StrokeOverlay : Node2D
{
    private List<Vector2> _points = [];
    public static readonly ShaderMaterial WireframeMaterial = new()
    {
        Shader = GD.Load<Shader>("res://Rendering/StrokeShader.gdshader"),
    };

    public readonly StrokeView Wireframe = new()
    {
        Material = WireframeMaterial
    };

    public override void _Ready()
    {
        AddChild(Wireframe);
    }

    public void UpdateGeometry(IReadOnlyList<Vector2> points)
    {
        QueueRedraw();
        const float wireframeRadius = 1f;
        Wireframe.UpdateGeometry(points, Enumerable.Repeat(wireframeRadius, points.Count).ToImmutableArray());
        _points = points.ToList();
    }

    public override void _Draw()
    {
        base._Draw();
        foreach (var p in _points) DrawCircle(p, 1.5f, Colors.Blue);
    }
}