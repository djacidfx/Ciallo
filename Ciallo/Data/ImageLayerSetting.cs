using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class ImageLayerSetting
{
    [DataMember] public ImageTexture Texture = new();
    [DataMember] public ReactiveProperty<Transform2D> ImageTransform = new(Transform2D.Identity);
}