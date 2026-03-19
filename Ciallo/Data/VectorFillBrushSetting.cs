using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class VectorFillBrushSetting
{
    [DataMember]
    public StrokeBrushSetting MarkerStrokeBrush = new()
    {
        RenderingType = { Value = BrushRenderingType.Stamp },
        ActiveStampFlags = { Value = StampFlags.StampTexture },
    };

    [DataMember]
    public ReactiveProperty<Color> FillColor = new(Colors.Black);

    public VectorFillBrushSetting Clone()
    {
        return new VectorFillBrushSetting
        {
            MarkerStrokeBrush = MarkerStrokeBrush.Clone(),
            FillColor = { Value = FillColor.Value }
        };
    }
}