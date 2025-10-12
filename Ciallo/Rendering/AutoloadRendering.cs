using Godot;

namespace Ciallo.Rendering;

public partial class AutoloadRendering : Node
{
    public static readonly ShaderMaterial WireframeMaterial = GD.Load<ShaderMaterial>("res://Rendering/WireframeMaterial.tres");
    public static readonly ShaderMaterial WireframeDotMaterial = GD.Load<ShaderMaterial>("res://Rendering/WireframeDotMaterial.tres");
    public static readonly Shader StrokeShader = GD.Load<Shader>("res://Rendering/Stroke.gdshader");
    public static readonly Mesh DummyMesh = GD.Load<Mesh>("res://Rendering/StrokeDummyMesh.tres");
    
    public override void _Ready()
    {
        StrokeShader.TakeOverPath("");
        DummyMesh.TakeOverPath("");
    }
}