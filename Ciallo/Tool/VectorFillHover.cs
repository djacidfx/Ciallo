using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

public class VectorFillHover : InteractiveSessionBase
{
    private readonly List<StrokeView> _contours = [];

    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldBody>().DefaultCursorShape = Control.CursorShape.Cross;
        SetContoursWithQueryResult(WorkingLayer.Get<OverlayHolder>(),
            WorkingLayer.Get<Arrangement>(), data.WorldPosition);
    }

    public override void Moving(CursorMotionData data)
    {
        SetContoursWithQueryResult(WorkingLayer.Get<OverlayHolder>(),
            WorkingLayer.Get<Arrangement>(), data.WorldPosition);
    }

    public override void End(CursorButtonData data) => Cancel();

    public override void Cancel()
    {
        foreach (var sv in _contours) sv.QueueFree();
        _contours.Clear();
        Document.Get<WorldBody>().DefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;

    public void SetContoursWithQueryResult(Node parent, Arrangement arr, Vector2 point)
    {
        var faceRid = arr.Query(point);
        if (!faceRid.IsValid)
        {
            foreach (var sv in _contours) sv.Multimesh.InstanceCount = 0;
            return;
        }
        var polygons = arr.GetFacePolygons(faceRid);
        if (polygons.Count == 0)
        {
            foreach (var sv in _contours) sv.Multimesh.InstanceCount = 0;
            return;
        }

        // Grow
        while (_contours.Count < polygons.Count)
        {
            var sv = new StrokeView { Material = AutoloadRendering.DashWireframeMaterial };
            _contours.Add(sv);
            parent.AddChild(sv);
        }
        // Shrink
        while (_contours.Count > polygons.Count)
        {
            var sv = _contours[^1];
            _contours.RemoveAt(_contours.Count - 1);
            sv.QueueFree();
        }

        float radius = AppPreference.StrokeWireframeRadius * 1.5f;
        for (int i = 0; i < polygons.Count; i++)
        {
            var closed = polygons[i].Append(polygons[i][0]).ToArray();
            _contours[i].SetGeometry(closed, radius);
        }
    }

    public override void DrawProperty(PropertyContainer container)
    {
        container.AddChild(new Label
        {
            Text = "Fill brush",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        var brushPreview = VectorFillBrushPreviewList.New(Document);
        brushPreview.CustomMinimumSize = new(0, 256);
        container.AddChild(brushPreview);

        var sm = Document.Get<SelectionManager>();

        var markerColor = sm.WorkingVectorFillBrush
            .Select(e => e.TryGet<VectorFillBrushSetting>()?.MarkerColor)
            .Flatten();
        container.AddProperty("Marker color",
            new ColorPickerButton { EditAlpha = false }
                .BindColor(markerColor)
                .VisibleIf(sm.WorkingVectorFillBrush, Entity.IsNotNull)
        );

        container.AddProperty("Marker radius",
            new SpinSlider
            {
                MinValue = 1.0f,
                MaxValue = 32f,
            }
                .BindNumber(AppPreference.VectorFillMarkerRadius)
                .VisibleIf(sm.WorkingVectorFillBrush, Entity.IsNotNull)
        );

        var fillColor = sm.WorkingVectorFillBrush
            .Select(e => e.TryGet<VectorFillBrushSetting>()?.FillColor)
            .Flatten();
        container.AddProperty("Fill color",
            new ColorPickerButton()
                .BindColor(fillColor)
                .VisibleIf(sm.WorkingVectorFillBrush, Entity.IsNotNull)
        );

        var showWireframe = new CheckButton()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new(128, 0),
        }
            .BindBool(AppPreference.ShowVectorFillReferenceLayerWireframe);
        container.AddProperty("Show reference wireframe", showWireframe);
    }
}