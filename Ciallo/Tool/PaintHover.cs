using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Tool;
using Godot;

public class PaintHover : InteractiveSessionBase
{
    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldCursorDetectionArea>().MouseDefaultCursorShape = Control.CursorShape.Cross;
    }
    public override void Interacting(CursorMotionData data) { }
    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        Document.Get<WorldCursorDetectionArea>().MouseDefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;
}