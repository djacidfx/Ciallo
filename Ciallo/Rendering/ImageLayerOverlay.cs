using Godot;

namespace Ciallo.Rendering;

public partial class ImageLayerOverlay : Node2D
{
    public Vector2[] Corners;
    public StrokeView Wireframe;
    public MultiMeshInstance2D Dots;
    public TouchScreenButton[] Buttons;

    public ImageLayerOverlay(Vector2[] corners)
    {
        Corners = corners;
    }

    public override void _Ready()
    {
        Wireframe = new() { Material = AutoloadRendering.WireframeMaterial };
        Wireframe.SetInstanceShaderParameter("overridingColor", AppPreference.StrokeWireframeColor);
        Vector2[] positions = [..Corners, Corners[0]];
        Wireframe.SetGeometry(positions, AppPreference.StrokeWireframeRadius);
        AddChild(Wireframe);
        
        Dots = AutoloadRendering.CreateDots();
        Dots.SetDotGeometry(positions, AppPreference.StrokeDotRadius * 2);
        AddChild(Dots);
    }
}