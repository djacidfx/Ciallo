using System.Collections.Generic;
using Ciallo.Widget;
using Godot;

namespace Ciallo.Rendering;

public partial class VectorFillMarkerView : Node2D
{
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
}