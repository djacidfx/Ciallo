using Ciallo.Geometry;
using Ciallo.Widget;
using Godot;

namespace Ciallo.Tool;

public class HintUserErrorDummy : InteractiveSessionBase
{
    public override void Start(CursorButtonData data) { }
    public override void Moving(CursorMotionData data) { }
    public override void End(CursorButtonData data) { }
    public override void Cancel() { }
    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    public override void DrawProperty(PropertyContainer container)
    {
        container.AddChild(new Label { Text = "No brush selected" });
    }
}