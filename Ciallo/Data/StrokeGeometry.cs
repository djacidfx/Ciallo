using System.Collections.Generic;
using System.Runtime.Serialization;
using Godot;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class StrokeGeometry
{
    [DataMember(Order = 0)] public List<Vector2> Points = [];
    [DataMember(Order = 1)] public List<float> Radii = [];

    public StrokeGeometry Clone()
    {
        return new StrokeGeometry()
        {
            Points = [..Points],
            Radii = [..Radii],
        };
    }
}