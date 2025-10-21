using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class PaintFillHover : HoverBase
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