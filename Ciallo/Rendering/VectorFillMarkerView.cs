using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Widget;
using Godot;
using Frent;

namespace Ciallo.Rendering;

public partial class VectorFillMarkerView : Node2D
{
    private const float OutlineWidth = 5.0f;
    private static readonly Shader OutlineShader = GD.Load<Shader>("res://Rendering/VectorFillMarker.gdshader");

    public FixedSizeSprite2D Sprite = new() { Visible = false };

    public VectorFillMarkerView()
    {
        AddChild(Sprite);
    }

    public void SetGeometry(
        IReadOnlyList<Vector2> positions,
        IReadOnlyList<float> radii)
    {
        if (positions.Count == 1)
        {
            Sprite.Visible = true;
            var p = positions[0];
            var r = radii[0];
            Sprite.Position = p;
            Sprite.Size = 2 * r * Vector2.One;
            return;
        }
        Sprite.Visible = false;
    }

    public static void ApplyBrush(Polygon2D polygon, VectorFillMarkerView marker, Entity brushE)
    {
        if (brushE.IsNull)
        {
            ApplyMissingBrush(polygon, marker);
            return;
        }

        ApplyBrush(polygon, marker, brushE.Get<FillBrushSetting>());
    }

    public static void ApplyBrush(Polygon2D polygon, VectorFillMarkerView marker, FillBrushSetting setting)
    {
        polygon.Color = setting.FillColor.Value;
        polygon.Material = null;
        polygon.Texture = null;
        marker.Sprite.Texture = setting.MarkerTexture.Value;
        marker.Sprite.Modulate = setting.MarkerColor.Value;
        marker.SetOutlineColor(setting.MarkerOutlineColor.Value);
    }

    public static void ApplyMissingBrush(Polygon2D polygon, VectorFillMarkerView marker)
    {
        polygon.Color = Colors.Black;
        polygon.Material = AutoloadRendering.MissingFillBrushMaterial;
        polygon.Texture = AutoloadRendering.DummyTextureForUV;
        marker.Sprite.Texture = null;
        marker.Sprite.Modulate = Colors.Black;
        marker.SetOutlineColor(null);
    }

    public void SetOutlineColor(Color? color)
    {
        if (!color.HasValue)
        {
            Sprite.Material = null;
            return;
        }

        var material = Sprite.Material as ShaderMaterial;
        if (material == null)
        {
            material = new ShaderMaterial
            {
                Shader = OutlineShader,
            };
            material.SetShaderParameter("width", OutlineWidth);
        }

        material.SetShaderParameter("color", color.Value);
        Sprite.Material = material;
    }
}
