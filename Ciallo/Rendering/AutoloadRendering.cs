using Ciallo.Data;
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
    public static BrushMaterial MissingBrushMaterial;

    public override void _Ready()
    {
        StrokeShader.TakeOverPath("");
        DummyMesh.TakeOverPath("");

        DashWireframeMaterial = new();
        DashWireframeMaterial.SetShaderParameter("StrokeType", 0);
        DashWireframeMaterial.SetShaderParameter("RadiusMode", 1);
        DashWireframeMaterial.SetShaderParameter("ActiveBrushFlags", (int)BrushFlags.Dash);
        DashWireframeMaterial.SetShaderParameter("DashLength", 10f);
        DashWireframeMaterial.SetShaderParameter("GapLength", 5f);
        DashWireframeMaterial.SetShaderParameter("DashForwardSpeed", 10f);

        WireframeMaterial = new();
        WireframeMaterial.SetShaderParameter("StrokeType", 0);
        WireframeMaterial.SetShaderParameter("RadiusMode", 1);
        WireframeMaterial.SetShaderParameter("MaterialColor", AppPreference.StrokeWireframeColor);

        MissingBrushMaterial = new();
        MissingBrushMaterial.SetShaderParameter("StrokeType", 0);
        MissingBrushMaterial.SetShaderParameter("MaterialColor", Colors.Crimson);
        MissingBrushMaterial.SetShaderParameter("ActiveBrushFlags", (int)BrushFlags.Dash);
        MissingBrushMaterial.SetShaderParameter("DashLength", 5f);
        MissingBrushMaterial.SetShaderParameter("GapLength", 5f);
        MissingBrushMaterial.SetShaderParameter("DashForwardSpeed", 7f);
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