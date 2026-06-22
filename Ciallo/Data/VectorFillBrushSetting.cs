using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillBrushSetting
{
    [DataMember, ProjectField(StorageKind.Blob)]
    public ReactiveProperty<ImageTexture> MarkerTexture = new(null);
    [DataMember, ProjectField]
    public ReactiveProperty<Color> MarkerColor = new(Colors.Black);

    [DataMember, ProjectField]
    public ReactiveProperty<Color> FillColor = new(Colors.Black);

    public VectorFillBrushSetting Clone()
    {
        return new VectorFillBrushSetting
        {
            MarkerTexture = { Value = MarkerTexture.Value },
            FillColor = { Value = FillColor.Value }
        };
    }
}
