using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using R3;

namespace Ciallo.Core;

/// <summary>
/// The core node in this program.
/// Note: Current design is mixing data, rendering and control systems together, separate them when necessary.
/// </summary>
[Tool]
[GlobalClass, Icon("res://Icons/vector-curve.svg")]
public partial class Stroke : Node2D
{
    private Polyline _polyline;
    [Export]
    public Polyline Polyline
    {
        get => _polyline;
        set
        {
            _polyline = value;
            SetRenderBuffer();
            SetBoundingBox();
            QueueRedraw();
            UpdateConfigurationWarnings();
        }
    }

    public Subject<Unit> PointsChanged = new();
    public Subject<Unit> RadiiChanged = new();

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
    
    private void SetRenderBuffer()
    {
        // Set data
        _multiMesh.InstanceCount = 0; // Clear buffer
        if (_polyline == null) return;
        // value to push buffer
        List<Vector2> points;
        List<float> radii;
        if (_polyline.Count() > 1) // regular case
        {
            _multiMesh.InstanceCount = _polyline.Count() - 1;
            points = _polyline.Points.ToList();
            radii = _polyline.Radii.ToList();
        }
        else if (_polyline.Count() == 1) // a point, render it as an ultra short segment
        {
            _multiMesh.InstanceCount = 1;
            float delta = 1e-5f;
            points = [_polyline.Points[0], _polyline.Points[0] + delta*Vector2.Right];
            radii = [_polyline.Radii[0], _polyline.Radii[0] + delta];
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
            // This transform values are not used in shaders
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
    
    private void SetBoundingBox()
    {
        var box = _polyline.BoundingBox;
        // Shen: Finding this function takes me 3 hours.
        // Avoid node being frustum culled.
        RenderingServer.CanvasItemSetCustomRect(GetCanvasItem(), true, box);
    }

    public override void _Draw()
    {
        DrawMultimesh(_multiMesh, null);
    }
}
