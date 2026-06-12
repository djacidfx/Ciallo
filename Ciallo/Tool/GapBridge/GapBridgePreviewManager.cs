using System;
using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public sealed class GapBridgePreviewManager : IDisposable
{
    private readonly Node2D _root;
    private readonly Node2D _facesRoot = new();
    private readonly Node2D _bridgesRoot = new();
    private readonly Dictionary<Rid, Polygon2D> _faceNodes = [];
    private readonly Dictionary<Rid, Color> _faceColors = [];
    private List<GapBridgeTarget> _targets = [];
    private int _nextFaceColorIndex;
    private readonly IReadOnlySet<Entity> _sourceShapes;

    public GapBridgePreviewManager(Node2D parent, IReadOnlySet<Entity> sourceShapes)
    {
        _sourceShapes = sourceShapes;
        _root = new Node2D { Visible = false };
        _root.AddChild(_facesRoot);
        _root.AddChild(_bridgesRoot);
        _facesRoot.ZIndex = 0;
        _bridgesRoot.ZIndex = 1;
        parent.AddChild(_root);
    }

    public void Refresh(Arrangement arr)
    {
        if (arr == null)
        {
            _root.Visible = false;
            _targets.Clear();
            return;
        }

        _root.Visible = true;
        SyncFaces(arr);
        SyncBridges(arr);
    }

    public void Dispose()
    {
        _root.QueueFree();
    }

    public bool TryPickTarget(Vector2 worldPosition, out GapBridgeTarget target)
    {
        return GapBridgeGeometry.TryFindNearestTarget(
            _targets,
            worldPosition,
            AppPreference.GapBridgeHitRadius.Value,
            out target);
    }

    private void SyncFaces(Arrangement arr)
    {
        var seen = new HashSet<Rid>();
        foreach (var faceRid in arr.GetAllFaces())
        {
            if (!faceRid.IsValid || arr.IsUnboundedFace(faceRid))
                continue;

            seen.Add(faceRid);
            if (!_faceNodes.TryGetValue(faceRid, out var faceNode))
            {
                faceNode = new Polygon2D
                {
                    Antialiased = true,
                };
                _faceNodes[faceRid] = faceNode;
                _facesRoot.AddChild(faceNode);
            }

            faceNode.Color = GetFaceColor(faceRid);
            faceNode.SetTriangleResult(arr.GetTrianglesFromFace(faceRid));
        }

        var deadFaces = new List<Rid>();
        foreach (var (faceRid, faceNode) in _faceNodes)
        {
            if (seen.Contains(faceRid)) continue;
            faceNode.QueueFree();
            deadFaces.Add(faceRid);
        }

        foreach (var faceRid in deadFaces)
            _faceNodes.Remove(faceRid);
    }

    private void SyncBridges(Arrangement arr)
    {
        foreach (var child in _bridgesRoot.GetChildren())
            child.QueueFree();

        var maxGapLength = AppPreference.GapBridgeDetectMaxGapLength.Value;
        _targets = GapBridgeGeometry.QueryTargets(arr, _sourceShapes, maxGapLength);

        foreach (var target in _targets)
        {
            var bridge = new StrokeView
            {
                Material = AutoloadRendering.DashWireframeMaterial,
                Modulate = new Color(1f, 1f, 1f, 0.9f),
            };
            _bridgesRoot.AddChild(bridge);
            bridge.SetGeometry(target.TargetPolyline, AppPreference.StrokeWireframeRadius * 1.25f);
        }
    }

    private Color GetFaceColor(Rid faceRid)
    {
        if (_faceColors.TryGetValue(faceRid, out var color))
            return color;

        float hue = (0.61803398875f * _nextFaceColorIndex++) % 1f;
        color = Color.FromHsv(hue, 0.55f, 0.95f, 0.28f);
        _faceColors[faceRid] = color;
        return color;
    }
}
