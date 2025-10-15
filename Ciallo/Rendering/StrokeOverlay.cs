using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ciallo.Rendering;

public partial class StrokeOverlay : Node2D
{
    public StrokeView Wireframe;
    public MultiMeshInstance2D Dots;

    public override void _Ready()
    {
        Wireframe = new() { Material = AutoloadRendering.WireframeMaterial };
        Wireframe.SetInstanceShaderParameter("overridingColor", AppPreference.StrokeWireframeColor);
        Dots = AutoloadRendering.CreateDots();

        AddChild(Wireframe);
        AddChild(Dots);
    }

    public void SetGeometry(IReadOnlyList<Vector2> points, IReadOnlyList<float> radii)
    {
        float wireframeRadius = AppPreference.StrokeWireframeRadius;
        float dotRadius = AppPreference.StrokeDotRadius;
        Wireframe.SetGeometry(points, wireframeRadius);
        Dots.SetDotGeometry(points, dotRadius);
    }

    public void SetColor(Color color)
    {
        Wireframe.SetInstanceShaderParameter("overridingColor", color);
        for (int i = 0; i < Dots.Multimesh.InstanceCount; i++)
        {
            Dots.Multimesh.SetInstanceColor(i, color);
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
        if (points.Count != radii.Count) throw new ArgumentException("Points and radii count mismatch.");

        var multiMesh = instance.Multimesh;
        multiMesh.InstanceCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            var transform = Transform2D.Identity.Scaled(Vector2.One * radii[i]).Translated(points[i]);
            multiMesh.SetInstanceTransform2D(i, transform);
            multiMesh.SetInstanceColor(i, AppPreference.StrokeWireframeColor);
        }
    }

    public static void SetDotGeometry(this MultiMeshInstance2D instance, IReadOnlyList<Vector2> points, float radius)
    {
        SetDotGeometry(instance, points, Enumerable.Repeat(radius, points.Count).ToArray());
    }
}