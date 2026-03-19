using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FillMarkerBrushSetting
{
    [DataMember]
    public BrushSetting MarkerBrush = new()
    {
        RenderingType = { Value = BrushRenderingType.Stamp },
        ActiveStampFlags = { Value = StampFlags.StampTexture },
    };

    [DataMember]
    public ReactiveProperty<Color> FillColor = new(Colors.Black);

    public FillMarkerBrushSetting Clone()
    {
        return new FillMarkerBrushSetting
        {
            MarkerBrush = MarkerBrush.Clone(),
            FillColor = { Value = FillColor.Value }
        };
    }
}