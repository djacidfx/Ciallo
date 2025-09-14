using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Command;
using Godot;

namespace Ciallo.Rendering;

public partial class StrokeOverlay : Node2D
{
    public static readonly ShaderMaterial WireframeMaterial = GD.Load<ShaderMaterial>("res://Rendering/WireframeMaterial.tres");
    public static readonly ShaderMaterial WireframeDotMaterial = GD.Load<ShaderMaterial>("res://Rendering/WireframeDotMaterial.tres");

    public StrokeView Wireframe;
    public MultiMeshInstance2D WireframeDot;
    public StrokeBody HitTestBody;

    public override void _Ready()
    {
        Wireframe = new() { Material = WireframeMaterial };
        Wireframe.SetInstanceShaderParameter("overridingColor", AppPreference.StrokeWireframeColor);
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            Mesh = GD.Load<Mesh>("res://Rendering/WireframeDotMesh.tres"),
        };
        WireframeDot = new()
        {
            Material = WireframeDotMaterial,
            Multimesh = multiMesh
        };
        HitTestBody = new();
        
        AddChild(Wireframe);
        AddChild(WireframeDot);
        AddChild(HitTestBody);
    }

    public void SetGeometry(IReadOnlyList<Vector2> points, IReadOnlyList<float> radii)
    {
        const float wireframeRadius = 2f;
        const float dotRadius = 12f;
        Wireframe.SetGeometry(points, Enumerable.Repeat(wireframeRadius, points.Count).ToImmutableArray());
        WireframeDot.SetDotGeometry(points, Enumerable.Repeat(dotRadius, points.Count).ToImmutableArray());
        HitTestBody.SetGeometry(points, radii);
    }
    
    public void SetColor(Color color)
    {
        Wireframe.SetInstanceShaderParameter("overridingColor", color);
        for(int i = 0; i < WireframeDot.Multimesh.InstanceCount; i++)
        {
            WireframeDot.Multimesh.SetInstanceColor(i, color);
        }
    }
}

public static class DotExtension
{
    public static void SetDotGeometry(this MultiMeshInstance2D instance, IReadOnlyList<Vector2> points, IReadOnlyList<float> radii)
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
            multiMesh.SetInstanceColor(i, AppPreference.StrokeWireframeColor);
        }
    }
}