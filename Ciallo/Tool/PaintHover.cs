using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Tool;
using Godot;

public class PaintHover : InteractiveSessionBase
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
}