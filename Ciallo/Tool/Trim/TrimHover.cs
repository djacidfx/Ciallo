using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class TrimHover : InteractiveSessionBase
{
    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldBody>().DefaultCursorShape = Control.CursorShape.Cross;
    }

    public override void Moving(CursorMotionData data) { }
    public override void End(CursorButtonData data) => Cancel();

    public override void Cancel()
    {
        Document.Get<WorldBody>().DefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;
}
