using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class ImageLayerSetting
{
    [DataMember] public ImageTexture Texture = new();
    [DataMember] public ReactiveProperty<Transform2D> ImageTransform = new(Transform2D.Identity);
    
    public Vector2[] GetCorners()
    {
        var transform = ImageTransform.Value;
        var half = Texture.GetSize() * 0.5f;
        return
        [
            transform * -half,
            transform * new Vector2(half.X, -half.Y),
            transform * half,
            transform * new Vector2(-half.X, half.Y)
        ];
    }

    public Vector2 Size => Texture.GetSize();
}