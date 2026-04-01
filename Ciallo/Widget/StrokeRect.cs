using Godot;

namespace Ciallo.Widget;

/// <summary>
/// A control that draws a stroked rectangle.
/// Behavior of resizing is identical to ColorRect, but draw a stroked rectangle instead of a filled one.
/// </summary>
[GlobalClass, Tool]
public partial class StrokeRect : Control
{
    [Export]
    public Color Color
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = Colors.White;

    [Export]
    public float Width
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 10f;

    public override void _Draw()
    {
        float half = Width / 2f;
        DrawRect(new Rect2(half, half, Size.X - Width, Size.Y - Width), Color, false, Width);
    }
}