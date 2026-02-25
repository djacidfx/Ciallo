using System.Runtime.Serialization;
using Frent;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FillMarkerSetting
{
    // Marker use stroke/polyline has single point
    [DataMember(Order = 0)] public ReactiveProperty<Entity> StrokeBrushE = new(default);
    [DataMember(Order = 1)] public ReactiveProperty<Color?> StrokeOverrideColor = new(null);
    // Order = 2 for future FillMaterial entity
    [DataMember(Order = 3)] public ReactiveProperty<Color> FillColor = new(Colors.White);

    public FillMarkerSetting Clone()
    {
        return new FillMarkerSetting
        {
            StrokeBrushE = { Value = StrokeBrushE.Value },
            StrokeOverrideColor = { Value = StrokeOverrideColor.Value },
            FillColor = { Value = FillColor.Value },
        };
    }
}