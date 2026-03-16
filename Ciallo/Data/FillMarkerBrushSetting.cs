using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FillMarkerBrushSetting
{
    // public StrokeBrushSetting MarkerBrushSetting; // To implement brushes as stroke marker in the future
    [DataMember]
    public ReactiveProperty<ImageTexture> Image = new(null);
    [DataMember]
    public ReactiveProperty<Color> MarkerColor = new(Colors.Black);
    [DataMember]
    public ReactiveProperty<Color> FillColor = new(Colors.Black);
}