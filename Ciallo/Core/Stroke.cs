using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ciallo;

// [Tool] // After enabling the tool script, have to reload the editor on compile. Disable this during development.
[GlobalClass, Icon("res://Icons/vector-curve.svg")]
public partial class Stroke : MultiMeshInstance2D
{
    [Export, Notify] public partial Polyline Polyline { get; set; }

    public Stroke()
    {
        InitMultiMesh();
        InitMaterial();
    }

    public Stroke([NotNull] Polyline polyline)
    {
        Polyline = polyline;
        InitMultiMesh();
        InitMaterial();
    }
    
    public void SetRenderBuffer(Polyline line)
    {
        // Set data
        Multimesh.InstanceCount = 0; // Clear buffer
        if (line == null) return;
        // value to push buffer
        List<Vector2> points;
        List<float> radii;
        if (line.Count() > 1) // regular case
        {
            Multimesh.InstanceCount = line.Count() - 1;
            points = line.Points;
            radii = line.Radii;
        }
        else if (line.Count() == 1) // a point, render it as an ultra short segment
        {
            Multimesh.InstanceCount = 1;
            float delta = 1e-5f;
            points = [line.Points[0], line.Points[0] + delta*Vector2.Right];
            radii = [line.Radii[0], line.Radii[0] + delta];
        }
        else
        {
            GD.PushWarning("Trying to render a line without points");
            return;
        }
        
        for(int i = 0; i < Multimesh.InstanceCount; i++)
        {
            Color value = new()
            {
                R = points[i].X,
                G = points[i].Y,
                B = points[i + 1].X,
                A = points[i + 1].Y
            };
            Multimesh.SetInstanceCustomData(i, value);
            // Have to use instance color to store t.
            Multimesh.SetInstanceColor(i, new(radii[i], radii[i + 1], 0, 0));
            // Have to set transform or do not render.
            // This transform values are not used in shaders, only to avoid godot frustum culling our strokes.
            // Camera frustum culling cannot be disabled.
            var transform = Transform2D.Identity.Translated(points[i]);
            Multimesh.SetInstanceTransform2D(i, transform);
        }
    }

    public void InitMultiMesh()
    {
        Multimesh = new MultiMesh();
        Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
        Multimesh.UseColors = true;
        Multimesh.UseCustomData = true;
        Multimesh.Mesh = GD.Load<Mesh>("res://core/StrokeDummyMesh.tres");
    }

    public void InitMaterial()
    {
        var material = GD.Load<ShaderMaterial>("res://core/StrokeMaterial.tres");
        Material = material;
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = base._GetConfigurationWarnings();
        if (Polyline == null)
        {
            warnings ??= [];
            warnings = warnings.Concat(["No polyline assigned."]).ToArray();
        }
        else if (!Polyline.Any())
        {
            warnings ??= [];
            warnings = warnings.Concat(["Assigned polyline has no points."]).ToArray();
        }
        return warnings;
    }
    

    public override void _EnterTree()
    {
        PolylineChanged += () =>
        {
            SetRenderBuffer(Polyline);
            UpdateConfigurationWarnings();
        };
        SetRenderBuffer(Polyline);
    }

    public override void _Ready()
    {
        
    }
}
