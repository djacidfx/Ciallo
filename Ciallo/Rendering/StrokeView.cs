using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Rendering;

/// <remarks>
/// Rough load estimation:
/// By software design, we should roughly support 1K-4K visible strokes and 100K invisible&inactive strokes with good performance
/// Node system has high risk of performance degradation; Rids in RenderingServer are preferred.
/// But don't make pre mature optimization. Make sure this is a real bottleneck.
/// </remarks>
[Tool, GlobalClass]
public partial class StrokeView : MultiMeshInstance2D
{
    private readonly List<float> _ns = [];

    public StrokeView()
    {
        if (Multimesh != null) return;
        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseCustomData = true,
            Mesh = AutoloadRendering.DummyMesh,
        };
        Multimesh = multiMesh;
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
    }

    public void SetGeometry([NotNull] IReadOnlyList<Vector2> positions, float radius)
    {
        SetGeometry(positions,
            [.. Enumerable.Repeat(radius, positions.Count)]);
    }

    public void SetGeometry(
        [NotNull] IReadOnlyList<Vector2> positions,
        [NotNull] IReadOnlyList<float> radii)
    {
        SetGeometry(positions, radii, [.. Enumerable.Repeat(1.0f, positions.Count)]);
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

        IReadOnlyList<Vector2> ps;
        IReadOnlyList<float> rs;
        _ns.Clear();

        if (positions.Count > 1) // regular case
        {
            multiMesh.InstanceCount = positions.Count - 1;
            ps = positions;
            rs = radii;
        }
        else if (positions.Count == 1) // a point, render it as an ultra short segment
        {
            multiMesh.InstanceCount = 1;
            ps = [positions[0], positions[0] + 1e-2f * Vector2.Right * radii[0]];
            rs = [radii[0], radii[0]];
            pressures = [pressures[0], pressures[0]];
        }
        else throw new("Unreachable");

        _ns.Add(0f);
        for (int i = 0; i < ps.Count - 1; i++)
        {
            var l = (ps[i + 1] - ps[i]).Length();
            var r0 = rs[i];
            var r1 = rs[i + 1];
            if (Mathf.Abs(r0 - r1) < 1e-5f)
            {
                // Nearly equal radius, avoid division by zero
                var r = (r0 + r1) * 0.5f;
                _ns.Add(_ns.Last() + l / r);
                continue;
            }

            var n = l / (r0 - r1) * Mathf.Log(r0 / r1);
            if (float.IsNaN(n))
            {
                // Fallback to equal-radius approximation to avoid NaN propagation
                var r = (r0 + r1) * 0.5f;
                _ns.Add(_ns.Last() + l / r);
                continue;
            }
            _ns.Add(_ns.Last() + n);
        }

        for (int i = 0; i < multiMesh.InstanceCount; i++)
        {
            multiMesh.SetInstanceTransform2D(i,
                new Transform2D(
                    ps[i],
                    ps[i + 1],
                    new(rs[i], rs[i + 1])));
            multiMesh.SetInstanceCustomData(i, new(_ns[i], _ns[i + 1], pressures[i], pressures[i + 1]));
        }

        // Set bounding box
        var boundingBox = ps.GetBoundingBox(rs);
        // Incorrect method:
        // RenderingServer.CanvasItemSetCustomRect(strokeView.GetCanvasItem(), true, boundingBox);
        // Godot cannot save the value in the scene.
        var aabb = new Aabb(boundingBox.Position.X, boundingBox.Position.Y, 0, boundingBox.Size.X, boundingBox.Size.Y, 0);
        multiMesh.CustomAabb = aabb;
    }
}
