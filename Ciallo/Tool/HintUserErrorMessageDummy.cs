using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Widget;
using Godot;

namespace Ciallo.Tool;

public class HintUserErrorMessageDummy : InteractiveSessionBase
{
    private Label _label;

    public string Message
    {
        get;
        set
        {
            field = value;
            _label?.Text = "⚠ " + value;
        }
    }

    public HintUserErrorMessageDummy(string msg = "")
    {
        Message = msg;
    }

    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldBody>().MouseDefaultCursorShape = Control.CursorShape.Forbidden;
    }
    public override void Moving(CursorMotionData data) { }
    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        Document.Get<WorldBody>().MouseDefaultCursorShape = Control.CursorShape.Forbidden;
    }
    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;

    public override void DrawProperty(PropertyContainer container)
    {
        _label = new() { Text = "⚠ " + Message };
        container.AddChild(_label);
    }
}