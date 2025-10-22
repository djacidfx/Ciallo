using Ciallo.Rendering;
using Ciallo.Tool;
using Godot;

public class PaintHover : HoverBase
{
    public override void Start()
    {
        Document.Get<WorldCursorDetectionArea>().MouseDefaultCursorShape = Control.CursorShape.Cross;
    }

    public override void End()
    {
        Document.Get<WorldCursorDetectionArea>().MouseDefaultCursorShape = default;
    }
}