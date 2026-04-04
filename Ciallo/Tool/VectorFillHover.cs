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
    private Polygon2D _fillPreview;

    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldBody>().MouseDefaultCursorShape = Control.CursorShape.Cross;

        _fillPreview = new()
        {
            Material = AutoloadRendering.VectorFillPreviewMaterial,
            Texture = AutoloadRendering.DummyTextureForUV,
        };
        WorkingLayer.Get<OverlayHolder>().AddChild(_fillPreview);
        _fillPreview.SetPolygonWithQueryResult(WorkingLayer.Get<Arrangement2D>(), data.WorldPosition);
    }

    public override void Moving(CursorMotionData data)
    {
        _fillPreview.SetPolygonWithQueryResult(WorkingLayer.Get<Arrangement2D>(), data.WorldPosition);
    }

    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        _fillPreview.QueueFree();
        Document.Get<WorldBody>().MouseDefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;

    public override void DrawProperty(PropertyContainer container)
    {
        container.AddChild(new Label
        {
            Text = "[Create Vector Fill Layer Hint]",
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

        var fillColor = sm.WorkingVectorFillBrush
            .Select(e => e.TryGet<VectorFillBrushSetting>()?.FillColor)
            .Flatten();
        container.AddProperty("Fill color",
            new ColorPickerButton().BindColor(fillColor)
                .VisibleIf(sm.WorkingVectorFillBrush, Entity.IsNotNull)
        );
    }
}