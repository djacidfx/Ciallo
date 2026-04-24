using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class PolylineBezierDeformHover : PolylineSelectHover
{
    public BezierPoint[] Curve;
    public float[] PolyTs;

    private Node2D _wireframe;
    private List<Body> _buttons;

    public override void Start(CursorButtonData data)
    {
        base.Start(data);

        // Data
        var selectedShapes = Document.Get<SelectionManager>().SelectedShapes;
        int pointCount = selectedShapes.Select(e => e.Get<PolylineGeometry>().Count).Sum();
        if (pointCount <= 1) return;
        var polylines = selectedShapes
            .Select(e => (IReadOnlyList<Vector2>)e.Get<PolylineGeometry>().Positions.Value)
            .ToList();
        var fitPoints = Geometry.Geometry.ClusterPolylines(polylines);
        Curve = fitPoints.FitBezier(2);
        (_, PolyTs) = Curve.GetClosestPoint(fitPoints);

        // Curve wireframe view
        _wireframe = new();
        _wireframe.AddChild(DrawBezierCenterline(Curve));
        foreach (var handleNode in DrawBezierHandle(Curve))
        {
            _wireframe.AddChild(handleNode);
        }
        _wireframe.AddChild(DrawBezierControlPoint(Curve));
        Document.Get<WorldOverlay>().AddChild(_wireframe);

        // Cursor
        var worldBody = Document.Get<WorldBody>();
        Vector2 buttonSize = AppPreference.StrokeDotRadius * 2 * Vector2.One;
        _buttons = new();
        foreach (var bezierPoint in Curve)
        {
            Vector2[] positions = [bezierPoint.P, bezierPoint.PIn, bezierPoint.POut];
            foreach (var p in positions)
            {
                var body = worldBody.CreateAddRectBody(buttonSize, p, CursorRectFlags.ScreenSize);
                body.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
                _buttons.Add(body);
            }
        }
    }

    public static StrokeView DrawBezierCenterline(IReadOnlyList<BezierPoint> curve, StrokeView update = null)
    {
        update ??= new StrokeView()
        {
            Material = AutoloadRendering.WireframeMaterial,
        };
        var (polyline, _) = curve.TessellateWithCache();
        update.SetGeometry(polyline, AppPreference.StrokeWireframeRadius);
        return update;
    }

    public static StrokeView[] DrawBezierHandle(IReadOnlyList<BezierPoint> curve, StrokeView[] update = null)
    {
        // One StrokeView per non-zero handle: [anchor → anchor+Out] and [anchor → anchor+In]
        var segments = new List<(Vector2, Vector2)>();
        for (int i = 0; i < curve.Count; i++)
        {
            var pt = curve[i];
            if (i < curve.Count - 1 && !pt.Out.IsZeroApprox())
                segments.Add((pt.P, pt.P + pt.Out));
            if (i > 0 && !pt.In.IsZeroApprox())
                segments.Add((pt.P, pt.P + pt.In));
        }

        if (update == null || update.Length != segments.Count)
        {
            if (update != null)
                foreach (var v in update)
                    v?.QueueFree();
            update = new StrokeView[segments.Count];
            for (int i = 0; i < segments.Count; i++)
                update[i] = new StrokeView { Material = AutoloadRendering.WireframeMaterial };
        }

        for (int i = 0; i < segments.Count; i++)
        {
            var (a, b) = segments[i];
            update[i].SetGeometry([a, b], AppPreference.StrokeWireframeRadius);
        }
        return update;
    }

    public static MultiMeshInstance2D DrawBezierControlPoint(IReadOnlyList<BezierPoint> curve, MultiMeshInstance2D update = null)
    {
        update ??= AutoloadRendering.CreateDots();
        var positions = curve.SelectMany(pt => new[] { pt.P, pt.PIn, pt.POut }).ToArray();
        update.SetDotGeometry(positions, AppPreference.StrokeDotRadius);
        return update;
    }

    public override void Cancel()
    {
        Curve = null;
        PolyTs = null;
        _wireframe?.QueueFree();
        _wireframe = null;
        _buttons?.ForEach(b => b.QueueFree());
        _buttons = null;
    }
}