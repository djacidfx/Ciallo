using Godot;

namespace Ciallo.Rendering;

public partial class RenderingAutoload : Node
{
    public override void _Ready()
    {
        StrokeView.BrushMaterial.SetShaderParameter("strokeType", 1);
        StrokeView.BrushMaterial.SetShaderParameter("strokeColor", Colors.Black);
        StrokeView.BrushMaterial.SetShaderParameter("stampInterval", 2.0f);
        StrokeView.BrushMaterial.SetShaderParameter("radiusMode", 0);
    }
}