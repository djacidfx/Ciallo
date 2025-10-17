using Godot;

namespace Ciallo.Rendering;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class ImageLayerOverlay : Node2D
{
    public Vector2 Size;
    public StrokeView Wireframe;
    public MultiMeshInstance2D Dots;

    // Note: Transform is set by Node2D, only size is necessary here
    public ImageLayerOverlay(Vector2 size)
    {
        Size = size;
        Wireframe = new() { Material = AutoloadRendering.WireframeMaterial };
        Wireframe.SetInstanceShaderParameter("overridingColor", AppPreference.StrokeWireframeColor);

        Dots = AutoloadRendering.CreateDots();
    }

    public override void _Ready()
    {
        Vector2 half = Size * 0.5f;
        Vector2[] positions =
        [
            -half,
            new(half.X, -half.Y),
            half,
            new(-half.X, half.Y),
            -half,
        ];
        Wireframe.SetGeometry(positions, AppPreference.StrokeWireframeRadius);
        AddChild(Wireframe);

        Dots.SetDotGeometry(positions[..4], AppPreference.StrokeDotRadius * 2f);
        AddChild(Dots);
    }
}