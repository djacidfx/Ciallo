using Ciallo.Geometry;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Ciallo.Widget;
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

    public override void DrawProperty(PropertyContainer container)
    {
        var brushPreview = VectorFillBrushPreviewList.New(Document);
        brushPreview.CustomMinimumSize = new(0, 256);
        container.AddChild(brushPreview);
    }
}