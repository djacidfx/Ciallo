using Godot;

namespace Ciallo.Rendering;

public partial class ImageLayerOverlay : Node2D
{
    public Sprite2D Sprite;
    public StrokeView Wireframe;
    public TouchScreenButton[] Buttons;

    public ImageLayerOverlay(Sprite2D sprite)
    {
        Sprite = sprite;
    }

    public override void _Ready()
    {
        Wireframe = new() { Material = AutoloadRendering.WireframeMaterial };
        Wireframe.SetInstanceShaderParameter("overridingColor", AppPreference.StrokeWireframeColor);
        var rect = Sprite.GetRect();
        Vector2[] positions =
        [
            rect.Position,
            rect.Position + new Vector2(rect.Size.X, 0),
            rect.End,
            rect.Position + new Vector2(0, rect.Size.Y),
            rect.Position
        ];
        Wireframe.SetGeometry(positions, AppPreference.StrokeWireframeRadius);
        AddChild(Wireframe);
    }
}