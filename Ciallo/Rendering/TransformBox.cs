using System.Linq;
using Godot;

namespace Ciallo.Rendering;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class TransformBox : Node2D
{
    public Vector2 Size;
    public Transform2D LocalTransform;
    public StrokeView Wireframe;
    public MultiMeshInstance2D Dots;

    public TransformBox(Vector2 size, Transform2D localTransform)
    {
        Size = size;
        LocalTransform = localTransform;
        Wireframe = new() { Material = AutoloadRendering.WireframeMaterial };
        Wireframe.SetInstanceShaderParameter("overridingColor", AppPreference.StrokeWireframeColor);

        Dots = AutoloadRendering.CreateDots();
        AddChild(Wireframe);
        AddChild(Dots);
        UpdateGeometry();
    }

    public TransformBox(Vector2 size) : this(size, Transform2D.Identity) // Transform2D.Identity is not static const
    {
    }

    public TransformBox(Vector2 size, Vector2 position) : this(size, new Transform2D(0, position))
    {
    }

    public void UpdateGeometry()
    {
        Vector2 half = Size * 0.5f;
        Vector2[] positions =
        [
            -half,
            new(half.X, -half.Y),
            half,
            new(-half.X, half.Y),
            -half,
        ];
        positions = positions.Select(p => LocalTransform * p).ToArray();
        Wireframe.SetGeometry(positions, AppPreference.StrokeWireframeRadius);
        Dots.SetDotGeometry(positions[..4], AppPreference.StrokeDotRadius * 2f);
    }
}