using Godot;

namespace Ciallo.Widget;

/// <summary>
/// Sprite2D not sized by texture. 
/// </summary>
/// <remarks>
/// Godot's sprite2D world size is determined by the texture size, and there is no built-in way to change it.
/// This class provides a way to set the world size of the sprite2D.
/// </remarks>
public partial class FixedSizeSprite2D : Sprite2D
{
    // Hide the base class's properties to prevent external misuse
    public new Vector2 Scale => base.Scale;
    public new Transform2D Transform => base.Transform;

    public new Texture2D Texture
    {
        get => base.Texture;
        set
        {
            base.Texture = value;
            UpdateScale();
        }
    }

    public Vector2 Size
    {
        get;
        set
        {
            field = value;
            UpdateScale();
        }
    }

    private void UpdateScale()
    {
        if (base.Texture is null) return;
        var textureSize = base.Texture.GetSize();
        base.Scale = Size / textureSize;
    }
}