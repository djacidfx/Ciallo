using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using R3;

namespace Ciallo;

[Tool] // After enabling the tool script, have to reload the editor on compile. Disable this during development.
[GlobalClass, Icon("res://Icons/vector-curve.svg")]
public partial class Stroke : Node2D
{
    [Export, Notify] public partial Polyline Polyline { get; set; }
    
    private MultiMesh _multiMesh;

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
        _multiMesh.InstanceCount = 0; // Clear buffer
        if (line == null) return;
        // value to push buffer
        List<Vector2> points;
        List<float> radii;
        if (line.Count() > 1) // regular case
        {
            _multiMesh.InstanceCount = line.Count() - 1;
            points = line.Points.ToList();
            radii = line.Radii.ToList();
        }
        else if (line.Count() == 1) // a point, render it as an ultra short segment
        {
            _multiMesh.InstanceCount = 1;
            float delta = 1e-5f;
            points = [line.Points[0], line.Points[0] + delta*Vector2.Right];
            radii = [line.Radii[0], line.Radii[0] + delta];
        }
        else
        {
            GD.PushWarning("Trying to render a line without points");
            return;
        }
        
        for(int i = 0; i < _multiMesh.InstanceCount; i++)
        {
            Color customPos = new()
            {
                R = points[i].X,
                G = points[i].Y,
                B = points[i + 1].X,
                A = points[i + 1].Y
            };
            _multiMesh.SetInstanceCustomData(i, customPos);
            // Have to use instance color to store t.
            _multiMesh.SetInstanceColor(i, new(radii[i], radii[i + 1], 0, 0));
            // Have to set transform or do not render.
            // This transform values are not used in shaders, only to avoid godot frustum culling our strokes.
            // Camera frustum culling cannot be disabled.
            _multiMesh.SetInstanceTransform2D(i, Transform2D.Identity);
        }
    }

    public void InitMultiMesh()
    {
        _multiMesh = new MultiMesh();
        _multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
        _multiMesh.UseColors = true;
        _multiMesh.UseCustomData = true;
        _multiMesh.Mesh = GD.Load<Mesh>("res://core/StrokeDummyMesh.tres");
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
            QueueRedraw();
        };
        SetRenderBuffer(Polyline);
    }

    public override void _Ready()
    {
        
    }

    public override void _Draw()
    {
        DrawMultimesh(_multiMesh, null);
    }
}
