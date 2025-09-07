using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Rendering;

public partial class StrokeView : MultiMeshInstance2D
{
    private static readonly Mesh DummyMesh = GD.Load<Mesh>("res://Rendering/StrokeDummyMesh.tres");
    private static readonly ShaderMaterial BrushMaterial = GD.Load<ShaderMaterial>("res://Rendering/StrokeMaterial.tres");

    public StrokeView()
    {
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            Mesh = DummyMesh,
        };
        this.Multimesh = multiMesh;
        this.Material = BrushMaterial;
    }
    
    public void UpdateStroke(
        [NotNull] IReadOnlyList<Vector2> points,
        [NotNull] IReadOnlyList<float> radii)
    {
        if(points.Count != radii.Count)
        {
            GD.PushError("Points and radii count mismatch.");
            return;
        }
        if(points.Count == 0 || radii.Count == 0)
        {
            GD.PushWarning("No points or radii provided.");
            return;
        }
        
        var multiMesh = this.Multimesh;
        multiMesh.InstanceCount = 0; // Also clear buffer
        
        ImmutableArray<Vector2> ps;
        ImmutableArray<float> rs;
        
        if (points.Count > 1) // regular case
        {
            multiMesh.InstanceCount = points.Count - 1;
            ps = [..points];
            rs = [..radii];
        }
        else if (points.Count == 1) // a point, render it as an ultra short segment
        {
            multiMesh.InstanceCount = 1;
            ps = [points[0], points[0] + float.Epsilon*Vector2.Right];
            rs = [radii[0], radii[0] + float.Epsilon];
        }
        else throw new("Unreachable");
        
        for(int i = 0; i < multiMesh.InstanceCount; i++)
        {
            Color customPos = new()
            {
                R = ps[i].X,
                G = ps[i].Y,
                B = ps[i + 1].X,
                A = ps[i + 1].Y
            };
            
            multiMesh.SetInstanceCustomData(i, customPos);
            // Have to use instance color to store t.
            multiMesh.SetInstanceColor(i, new(rs[i], rs[i + 1], 0, 0));
            // Have to set transform or do not render, this transform values are not used in shaders
            multiMesh.SetInstanceTransform2D(i, Transform2D.Identity);
        }
        
        // Set bounding box
        var boundingBox = points.GetBoundingBox(radii);
        // Incorrect method:
        // RenderingServer.CanvasItemSetCustomRect(strokeView.GetCanvasItem(), true, boundingBox);
        // Godot cannot save the value in the scene.
        var aabb = new Aabb(boundingBox.Position.X, boundingBox.Position.Y, 0, boundingBox.Size.X, boundingBox.Size.Y, 0);
        multiMesh.CustomAabb = aabb;
    }
}