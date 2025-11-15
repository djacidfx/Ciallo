using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Godot;

namespace Ciallo.Data;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
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

    public string Describe(int sampleCount = 4)
    {
        var sample = Positions.Count == 0
            ? "Ø"
            : string.Join(", ", Positions.Take(sampleCount).Select(FormatVector));
        var suffix = Positions.Count > sampleCount ? ", …" : string.Empty;
        return $"PolylineGeometry(Pos={Positions.Count}, Radii={Radii.Count}, Pressures={Pressures.Count}, Tilts={Tilts.Count}, Sample=[{sample}{suffix}])";
    }

    public override string ToString() => Describe();

    private string DebuggerDisplay => Describe(2);

    private static string FormatVector(Vector2 v) => $"({v.X:F1}, {v.Y:F1})";
}