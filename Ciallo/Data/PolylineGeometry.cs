using System.Collections.Generic;
using System.Runtime.Serialization;
using Godot;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class PolylineGeometry
{
    [DataMember(Order = 0)] public List<Vector2> Positions = [];
    [DataMember(Order = 1)] public List<float> Radii = [];

    [DataMember(Order = 2)] public List<float> Pressures = [];
    [DataMember(Order = 3)] public List<Vector2> Tilts = [];

    public PolylineGeometry Clone()
    {
        return new PolylineGeometry()
        {
            Positions = [..Positions],
            Radii = [..Radii],
            Pressures = [..Pressures],
            Tilts = [..Tilts],
        };
    }
}