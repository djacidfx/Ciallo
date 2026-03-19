using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class VectorFillHover : InteractiveSessionBase
{
    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldBody>().MouseDefaultCursorShape = Control.CursorShape.Cross;
    }

    public override void Moving(CursorMotionData data) { }

    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        Document.Get<WorldBody>().MouseDefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;

    public override void OnMouseButton(InputEventMouseButton button, CursorButtonData data)
    {
        if (button.Pressed && button.ButtonIndex == MouseButton.Left)
        {
            var i = WorkingLayer.Get<LayerTreeNode>().Index;
            var layerE = WorkingLayer.World.Create();
            var brushE = Document.Get<BrushManager>().VectorFillBrushEs[0];
            new CommandBuilder(layerE)
                .NewVectorFillLayer()
                .AddToLayerTree(Document, i)
                .SetWorkingLayer()
                .SetTarget(WorkingLayer.World.Create())
                .NewVectorFillMarker()
                .AddToLayerTree(layerE)
                .SetPolylineGeometry([data.WorldPosition], [100.0f], [1.0f], [Vector2.Zero])
                .SetProperty(e => e.Get<VectorFillMarkerSetting>().BrushE, brushE)
                .Commit();
        }
    }
}