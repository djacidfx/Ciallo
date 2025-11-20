using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Rendering;

/// <summary>
/// On a special architected system (shen's laptop with touch screen), calling UpdateBuffer lags CPU?? Seems like system's GPU driver bug.
/// Shen didn't find the issue in his another laptop, and failed to fix it.
/// A pray to Alan Turing has been made to avoid this issue on users' computer.
/// </summary>
[Tool, GlobalClass]
public partial class StrokeView : MultiMeshInstance2D
{
    public StrokeView()
    {
        if (Multimesh != null) return;
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            Mesh = AutoloadRendering.DummyMesh,
        };
        Multimesh = multiMesh;
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
    }

    public void SetGeometry([NotNull] IReadOnlyList<Vector2> positions, float radius)
    {
        SetGeometry(positions,
            Enumerable.Repeat(radius, positions.Count).ToImmutableArray());
    }

    public void SetGeometry(
        [NotNull] IReadOnlyList<Vector2> positions,
        [NotNull] IReadOnlyList<float> radii)
    {
        SetGeometry(positions, radii, Enumerable.Repeat(1.0f, positions.Count).ToImmutableArray());
    }

    public void SetGeometry(
        [NotNull] IReadOnlyList<Vector2> positions,
        [NotNull] IReadOnlyList<float> radii,
        [NotNull] IReadOnlyList<float> pressures)
    {
        if (positions.Count != radii.Count || positions.Count != pressures.Count)
        {
            GD.PushError("List element number mismatch.");
            return;
        }

        if (positions.Count == 0 || radii.Count == 0)
        {
            Multimesh.InstanceCount = 0;
            return;
        }

        var multiMesh = Multimesh;
        multiMesh.InstanceCount = 0; // Also clear buffer

        IReadOnlyList<Vector2> ps;
        IReadOnlyList<float> rs;
        List<float> ns = new() { Capacity = positions.Count };

        if (positions.Count > 1) // regular case
        {
            multiMesh.InstanceCount = positions.Count - 1;
            ps = positions;
            rs = radii;
        }
        else if (positions.Count == 1) // a point, render it as an ultra short segment
        {
            multiMesh.InstanceCount = 1;
            ps = [positions[0], positions[0] + 1e-5f * Vector2.Right];
            rs = [radii[0], radii[0] + 1e-5f];
            pressures = [0, 0];
        }
        else throw new("Unreachable");

        ns.Add(0f);
        for (int i = 0; i < ps.Count - 1; i++)
        {
            var l = (ps[i + 1] - ps[i]).Length();
            var r0 = rs[i];
            var r1 = rs[i + 1];
            if (Mathf.Abs(r0 - r1) < 1e-5f)
            {
                // Nearly equal radius, avoid division by zero
                var r = (r0 + r1) * 0.5f;
                ns.Add(ns.Last() + l / r);
                continue;
            }

            var n = l / (r0 - r1) * Mathf.Log(r0 / r1);
            if (float.IsNaN(n))
            {
                GD.PushError("NaN encountered in stroke parameterization.");
            }
            ns.Add(ns.Last() + n);
        }

        for (int i = 0; i < multiMesh.InstanceCount; i++)
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
            multiMesh.SetInstanceColor(i,
                new(Float32Packer.Pack(rs[i], rs[i + 1]),
                    Float32Packer.Pack(ns[i], ns[i + 1]),
                    Float32Packer.Pack(pressures[i], pressures[i + 1]), 0)); // no enough empty spaces :(
            // Have to set transform or do not render, this transform values are not used in shaders
            // Cannot access this matrix from the CanvasItem shader, so cannot be used for passing data.
            multiMesh.SetInstanceTransform2D(i, Transform2D.Identity);
        }

        // Set bounding box
        var boundingBox = positions.GetBoundingBox(radii);
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