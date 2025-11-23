using System;
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
        if (Positions.Count == 0)
        {
            return $"PolylineGeometry(Pos={Positions.Count}, Radii={Radii.Count}, Pressures={Pressures.Count}, Tilts={Tilts.Count}, Sample=[Ø])";
        }

        var count = Math.Min(sampleCount, Positions.Count);
        var samples = Enumerable.Range(0, count).Select(FormatSample);
        var sample = string.Join("\n", samples);
        var suffix = Positions.Count > sampleCount ? $"\n  ... ({Positions.Count - sampleCount} more)" : string.Empty;
        return $"PolylineGeometry(Pos={Positions.Count}, Radii={Radii.Count}, Pressures={Pressures.Count}, Tilts={Tilts.Count})\nSamples:\n{sample}{suffix}";
    }

    public PolylineGeometry Index(List<int> indices)
    {
        var newGeometry = new PolylineGeometry();
        foreach (var idx in indices)
        {
            newGeometry.Positions.Add(Positions[idx]);
            newGeometry.Radii.Add(Radii[idx]);
            newGeometry.Pressures.Add(Pressures[idx]);
            newGeometry.Tilts.Add(Tilts[idx]);
        }
        return newGeometry;
    }

    public override string ToString() => Describe();

    private string DebuggerDisplay => Describe(2);

    private string FormatSample(int index)
    {
        var pos = FormatVector(Positions[index]);
        var radius = index < Radii.Count ? $"{Radii[index],6:F1}" : "     ?";
        var pressure = index < Pressures.Count ? $"{Pressures[index],5:F2}" : "    ?";
        var tilt = index < Tilts.Count ? FormatVector(Tilts[index]) : "          ?";
        return $"  [{index,3}] Pos={pos,16} R={radius} P={pressure} Tilt={tilt}";
    }

    private static string FormatVector(Vector2 v) => $"({v.X,6:F1},{v.Y,6:F1})";
}