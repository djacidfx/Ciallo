using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Widget;
using Godot;
using R3;

namespace Ciallo.Tool;

public class PaintFillHover : InteractiveSessionBase
{
    public readonly ReactiveProperty<Color> FillColor = new(Colors.Black);

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
        container.AddProperty("Fill Color", new ColorPickerButton
        {
            CustomMinimumSize = new(0, 32),
        }.BindColor(FillColor));
    }
}