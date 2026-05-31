using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class ImageLayerSetting
{
    [DataMember, ProjectField(StorageKind.Blob)] public ImageTexture Texture = new();
    [DataMember, ProjectField(StorageKind.Blob)] public ReactiveProperty<Transform2D> ImageTransform = new(Transform2D.Identity);

    public Vector2[] GetCorners()
    {
        var transform = ImageTransform.Value;
        var half = Texture.GetSize() * 0.5f;
        return
        [
            transform * -half,
            transform * new Vector2(-half.X, half.Y),
            transform * half,
            transform * new Vector2(half.X, -half.Y)
        ];
    }

    public Vector2 ImageSize => Texture.GetSize();
    public Vector2 Position => ImageTransform.Value.Origin;
    public Vector2 Scale => ImageTransform.Value.Scale;
    public float Rotation => ImageTransform.Value.Rotation;

    public ImageLayerSetting Clone()
    {
        return new ImageLayerSetting
        {
            Texture = Texture,
            ImageTransform = { Value = ImageTransform.Value },
        };
    }
}
