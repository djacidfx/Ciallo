using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Tool;

public class PolylineBezierDeformHover : PolylineSelectHover
{
    public BezierPoint[] Curve;
    public float[] PolyTs;

    private Node2D _wireframe;
    private List<Body> _buttons;
    protected ObservableList<Entity> SelectedShapes;

    public override void Start(CursorButtonData data)
    {
        Subs = new();
        var worldBody = Document.Get<WorldBody>();
        SelectedShapes = Document.Get<SelectionManager>().SelectedShapes;

        // Enable cursor detections
        worldBody.EnableHoverDetection = true;
        worldBody.CursorWorldPosition = data.WorldPosition;

        Document.Get<WorldBody>().HoveringBody.Subscribe(ToggleWireframe).AddTo(Subs);
        SelectedShapes.ForEach(e => e.Get<Body>().ProcessMode = Node.ProcessModeEnum.Disabled);

        // Data
        int pointCount = SelectedShapes.Select(e => e.Get<PolylineGeometry>().Count).Sum();
        if (pointCount <= 1) return;
        var polylines = SelectedShapes
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
        Vector2 buttonSize = AppPreference.StrokeDotRadius * 2 * Vector2.One;
        _buttons = new();
        for (int i = 0; i < Curve.Length; i++)
        {
            var pt = Curve[i];
            var anchorBody = worldBody.CreateAddRectBody(buttonSize, pt.P, CursorRectFlags.ScreenSize);
            anchorBody.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
            _buttons.Add(anchorBody);
            if (i > 0 && !pt.In.IsZeroApprox())
            {
                var inBody = worldBody.CreateAddRectBody(buttonSize, pt.PIn, CursorRectFlags.ScreenSize);
                inBody.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
                _buttons.Add(inBody);
            }
            if (i < Curve.Length - 1 && !pt.Out.IsZeroApprox())
            {
                var outBody = worldBody.CreateAddRectBody(buttonSize, pt.POut, CursorRectFlags.ScreenSize);
                outBody.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
                _buttons.Add(outBody);
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

    public static StrokeView[] DrawBezierHandle(IReadOnlyList<BezierPoint> curve)
    {
        var result = new StrokeView[curve.Count];
        for (int i = 0; i < curve.Count; i++)
        {
            result[i] = new StrokeView { Material = AutoloadRendering.WireframeMaterial };
            var pt = curve[i];
            bool hasIn = i > 0 && !pt.In.IsZeroApprox();
            bool hasOut = i < curve.Count - 1 && !pt.Out.IsZeroApprox();
            List<Vector2> pts = [];
            if (hasIn)
                pts.Add(pt.PIn);
            pts.Add(pt.P);
            if (hasOut)
                pts.Add(pt.POut);
            result[i].SetGeometry(pts, AppPreference.StrokeWireframeRadius);
        }
        return result;
    }

    public static MultiMeshInstance2D DrawBezierControlPoint(IReadOnlyList<BezierPoint> curve, MultiMeshInstance2D update = null)
    {
        update ??= AutoloadRendering.CreateDots();
        var positions = new List<Vector2>();
        for (int i = 0; i < curve.Count; i++)
        {
            var pt = curve[i];
            positions.Add(pt.P);
            if (i > 0 && !pt.In.IsZeroApprox())
                positions.Add(pt.PIn);
            if (i < curve.Count - 1 && !pt.Out.IsZeroApprox())
                positions.Add(pt.POut);
        }
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
        SelectedShapes.ForEach(e => e.Get<Body>().ProcessMode = Node.ProcessModeEnum.Inherit);
        base.Cancel();
    }
}