using Godot;

namespace Ciallo.Rendering;

public partial class RenderingAutoload : Node
{
    public override void _Ready()
    {
        StrokeView.BrushMaterial.SetShaderParameter("strokeType", 1);
        StrokeView.BrushMaterial.SetShaderParameter("strokeColor", Colors.Black);
        StrokeView.BrushMaterial.SetShaderParameter("stampInterval", 1.0f);
        StrokeView.BrushMaterial.SetShaderParameter("radiusMode", 0);
        
        StrokeOverlay.WireframeMaterial.SetShaderParameter("strokeType", 0);
        StrokeOverlay.WireframeMaterial.SetShaderParameter("strokeColor", Colors.Blue);
        StrokeOverlay.WireframeMaterial.SetShaderParameter("radiusMode", 1);
        StrokeOverlay.WireframeMaterial.SetShaderParameter("canvasZoomThreshold", 1.0f);
    }
}