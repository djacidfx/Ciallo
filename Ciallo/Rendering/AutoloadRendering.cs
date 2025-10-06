using Godot;

namespace Ciallo.Rendering;

public partial class AutoloadRendering : Node
{
    public override void _Ready()
    {
        BrushMaterial.StrokeShader.TakeOverPath("");
        StrokeView.DummyMesh.TakeOverPath("");
    }
}