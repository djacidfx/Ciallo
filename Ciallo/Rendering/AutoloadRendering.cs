using Godot;

namespace Ciallo.Rendering;

public partial class AutoloadRendering : Node
{
    public static BrushMaterial WireframeMaterial;
    public static readonly ShaderMaterial WireframeDotMaterial =
        GD.Load<ShaderMaterial>("res://Rendering/WireframeDotMaterial.tres");
    public static readonly Shader StrokeShader = GD.Load<Shader>("res://Rendering/Stroke.gdshader");
    public static readonly Mesh DummyMesh = GD.Load<Mesh>("res://Rendering/StrokeDummyMesh.tres");
    public static readonly Mesh WireframeDotMesh = GD.Load<Mesh>("res://Rendering/WireframeDotMesh.tres");
    public static BrushMaterial DashWireframeMaterial;

    public override void _Ready()
    {
        StrokeShader.TakeOverPath("");
        DummyMesh.TakeOverPath("");

        DashWireframeMaterial = new();
        DashWireframeMaterial.SetShaderParameter("StrokeType", 0);
        DashWireframeMaterial.SetShaderParameter("RadiusMode", 1);
        DashWireframeMaterial.SetShaderParameter("DashLength", 10f);
        DashWireframeMaterial.SetShaderParameter("GapLength", 5f);
        DashWireframeMaterial.SetShaderParameter("DashForwardSpeed", 10f);

        WireframeMaterial = new();
        WireframeMaterial.SetShaderParameter("StrokeType", 0);
        WireframeMaterial.SetShaderParameter("RadiusMode", 1);
        WireframeMaterial.SetShaderParameter("MaterialColor", AppPreference.StrokeWireframeColor);
    }

    public static MultiMeshInstance2D CreateDots()
    {
        return new()
        {
            Material = WireframeDotMaterial,
            Multimesh = new MultiMesh
            {
                UseColors = true,
                Mesh = WireframeDotMesh,
            }
        };
    }
}