using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Rendering;

[Tool, GlobalClass]
public partial class StrokeView : MultiMeshInstance2D
{
    public static readonly Mesh DummyMesh = GD.Load<Mesh>("res://Rendering/StrokeDummyMesh.tres");

    public static readonly ShaderMaterial BrushMaterial = new()
    {
        Shader = GD.Load<Shader>("res://Rendering/Stroke.gdshader"),
    };

    public StrokeView()
    {
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            Mesh = DummyMesh,
        };
        Multimesh = multiMesh;
        Material = BrushMaterial;
    }

    public override void _Ready()
    {
        // For editor preview, convenient rendering test.
        if (!Engine.IsEditorHint()) return;
        UpdateGeometry([new(1, 1), new(100, 0), new(150, 50), new(200, 0)], [4f, 10f, 5f, 15f]);
    }

    public void UpdateGeometry(
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
            Multimesh.InstanceCount = 0;
            return;
        }
        
        var multiMesh = Multimesh;
        multiMesh.InstanceCount = 0; // Also clear buffer
        
        ImmutableArray<Vector2> ps;
        ImmutableArray<float> rs;
        List<float> nds = [];
        
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
        
        nds.Add(0);
        for(int i = 0; i < ps.Length - 1; i++)
        {
            var l = (ps[i + 1] - ps[i]).Length();
            var r0 = rs[i];
            var r1 = rs[i + 1];
            if(Mathf.Abs(r0 - r1) < 1e-10f)
            {
                // Nearly equal radius, avoid division by zero
                var r = (r0 + r1) * 0.5f;
                nds.Add(nds.Last() + l / r);
                continue;
            }
            
            var nd = l / (r0 - r1) * Mathf.Log(r0 / r1);
            nds.Add(nds.Last() + nd);
        }
        // GD.Print("----------");
        // // GD.Print(string.Join(" ", ps));
        // GD.Print(string.Join(" ", rs));
        // GD.Print(string.Join(" ", nds));
        
        for(int i = 0; i < multiMesh.InstanceCount; i++)
        {
            Color customPos = new()
            {
                R = ps[i].X,
                G = ps[i].Y,
                B = ps[i + 1].X,
                A = ps[i + 1].Y,
            };
            
            multiMesh.SetInstanceCustomData(i, customPos);
            // Have to use instance color to store t.
            multiMesh.SetInstanceColor(i, new(Float32Packer.Pack(rs[i],rs[i+1]), Float32Packer.Pack(nds[i], nds[i+1]), 0, 0)); // empty spaces for tilt
            // Have to set transform or do not render, this transform values are not used in shaders
            // Cannot access this matrix from the CanvasItem shader, so cannot be used for passing data.
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

public static class Float32Packer
{
    /// <summary>
    /// Packs two 32-bit floats into one 32-bit float
    /// </summary>
    public static float Pack(float x, float y)
    {
        ushort hx = BitConverter.HalfToUInt16Bits((Half)x);
        ushort hy = BitConverter.HalfToUInt16Bits((Half)y);
        
        uint word = ((uint)hy << 16) | hx;
        
        return BitConverter.Int32BitsToSingle((int)word);
    }
    
    public static float Pack(Vector2 v) => Pack(v.X, v.Y);
    
    // ReSharper disable once UnusedMember.Global
    public static Vector2 Unpack(float packed)
    {
        uint word = (uint)BitConverter.SingleToInt32Bits(packed);

        // slice into the two 16-bit halves
        ushort hx = (ushort)(word >> 16);
        ushort hy = (ushort)(word & 0xFFFF);

        // convert back to full-precision floats
        var x = (float)BitConverter.UInt16BitsToHalf(hx);
        var y = (float)BitConverter.UInt16BitsToHalf(hy);
        return new(x, y);
    }
}