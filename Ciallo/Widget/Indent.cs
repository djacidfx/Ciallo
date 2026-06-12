using Godot;

namespace Ciallo.Widget;

/// <summary>
/// Draw blank indent with Width*Count, each indent area draw a vertical bar in the middle
/// </summary>
[GlobalClass, Tool]
public partial class Indent : Control
{
    [Export]
    public int Count
    {
        get;
        set
        {
            field = value;
            RefreshMinimumSize();
            QueueRedraw();
        }
    } = 0;

    [Export]
    public int Width
    {
        get;
        set
        {
            field = value;
            RefreshMinimumSize();
            QueueRedraw();
        }
    } = 12;

    [Export]
    public int BarWidth
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = 2;

    [Export]
    public Color BarColor
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = new Color(1f, 1f, 1f, 0.2f);

    public override void _Ready()
    {
        RefreshMinimumSize();
    }

    public override void _Draw()
    {
        for (int i = 0; i < Count; i++)
        {
            float x = i * Width + Width / 2f;
            DrawDashedLine(new Vector2(x, 0), new Vector2(x, Size.Y), BarColor, BarWidth, 4);
        }
    }

    private void RefreshMinimumSize()
    {
        CustomMinimumSize = new Vector2(Width * Count, 0);
    }
}