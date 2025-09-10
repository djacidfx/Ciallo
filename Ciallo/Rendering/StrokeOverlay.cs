using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace Ciallo.Rendering;

public partial class StrokeOverlay : Node2D
{
    public static readonly ShaderMaterial WireframeMaterial = GD.Load<ShaderMaterial>("res://Rendering/WireframeMaterial.tres");
    public static readonly ShaderMaterial WireframeDotMaterial = GD.Load<ShaderMaterial>("res://Rendering/WireframeDotMaterial.tres");
    private List<Vector2> _points = [];

    public StrokeView Wireframe;
    public MultiMeshInstance2D WireframeDot;

    public override void _Ready()
    {
        Wireframe = new() { Material = WireframeMaterial };
        WireframeDot = new () {Material = WireframeDotMaterial};
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            Mesh = GD.Load<Mesh>("res://Rendering/WireframeDotMesh.tres"),
        };
        WireframeDot.Multimesh = multiMesh;
        AddChild(Wireframe);
        AddChild(WireframeDot);
    }

    public void UpdateGeometry(IReadOnlyList<Vector2> points)
    {
        const float wireframeRadius = 2f;
        const float dotRadius = 12f;
        Wireframe.UpdateGeometry(points, Enumerable.Repeat(wireframeRadius, points.Count).ToImmutableArray());
        _points = points.ToList();
        WireframeDot.UpdateDotGeometry(points, Enumerable.Repeat(dotRadius, points.Count).ToImmutableArray());
    }
}

public static class DotExtension
{
    public static void UpdateDotGeometry(this MultiMeshInstance2D instance, IReadOnlyList<Vector2> points, IReadOnlyList<float> radii)
    {
        if (points.Count == 0)
        {
            instance.Multimesh.InstanceCount = 0;
            return;
        }
        if(points.Count != radii.Count) throw new System.ArgumentException("Points and radii count mismatch.");

        var multiMesh = instance.Multimesh;
        multiMesh.InstanceCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            var transform = Transform2D.Identity.Scaled(Vector2.One * radii[i]).Translated(points[i]);
            multiMesh.SetInstanceTransform2D(i, transform);
            multiMesh.SetInstanceColor(i, Colors.RoyalBlue);
        }
    }
}