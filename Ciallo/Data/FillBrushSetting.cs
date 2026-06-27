using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FillBrushSetting
{
    [DataMember, ProjectField(StorageKind.Blob)]
    public ReactiveProperty<ImageTexture> MarkerTexture = new(null);
    [DataMember, ProjectField]
    public ReactiveProperty<Color> MarkerColor = new(Colors.Black);
    [DataMember, ProjectField]
    public ReactiveProperty<Color?> MarkerOutlineColor = new();

    [DataMember, ProjectField]
    public ReactiveProperty<Color> FillColor = new(Colors.Black);

    public FillBrushSetting Clone()
    {
        return new FillBrushSetting
        {
            MarkerTexture = { Value = MarkerTexture.Value },
            MarkerColor = { Value = MarkerColor.Value },
            MarkerOutlineColor = { Value = MarkerOutlineColor.Value },
            FillColor = { Value = FillColor.Value }
        };
    }
}
