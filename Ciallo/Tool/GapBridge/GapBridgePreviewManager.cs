using Ciallo.Data;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public sealed class GapBridgePreviewManager : System.IDisposable
{
    private readonly Node2D _root;
    private readonly Node2D _facesRoot = new();
    private readonly Node2D _bridgesRoot = new();
    private readonly Dictionary<Rid, Polygon2D> _faceNodes = [];
    private readonly Dictionary<Rid, Color> _faceColors = [];
    private int _nextFaceColorIndex;

    public GapBridgePreviewManager(Node2D parent)
    {
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

        foreach (var candidate in GapBridgeGeometry.ParseCandidates(arr.GetGapBridgeCandidates(100f)))
        {
            if (!candidate.FromCurve.IsAlive || !candidate.ToCurve.IsAlive)
                continue;
            if (!candidate.FromCurve.Has<PolylineGeometry>() || !candidate.ToCurve.Has<PolylineGeometry>())
                continue;

            var fromPositions = candidate.FromCurve.Get<PolylineGeometry>().Positions.Value;
            var toPositions = candidate.ToCurve.Get<PolylineGeometry>().Positions.Value;
            var fromPoint = TrimGeometry.SampleVec2(fromPositions, candidate.FromT);
            var toPoint = TrimGeometry.SampleVec2(toPositions, candidate.ToT);

            var bridge = new StrokeView
            {
                Material = AutoloadRendering.DashWireframeMaterial,
                Modulate = new Color(1f, 1f, 1f, 0.9f),
            };
            _bridgesRoot.AddChild(bridge);
            bridge.SetGeometry([fromPoint, toPoint], AppPreference.StrokeWireframeRadius * 1.25f);
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
