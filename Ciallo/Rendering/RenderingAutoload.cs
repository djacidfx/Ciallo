using Godot;

namespace Ciallo.Rendering;

public partial class RenderingAutoload : Node
{
    public override void _Ready()
    {
        var m = StrokeView.BrushMaterial;
        m.SetShaderParameter("strokeType", 1);
        m.SetShaderParameter("strokeColor", Colors.Black);
        m.SetShaderParameter("stampInterval", 1.0f);
    }
}