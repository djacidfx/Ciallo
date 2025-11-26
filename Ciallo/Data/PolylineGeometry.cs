using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Ciallo.Geometry;
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

    public int Capacity
    {
        get => Positions.Capacity;
        set
        {
            Positions.Capacity = value;
            Radii.Capacity = value;
            Pressures.Capacity = value;
            Tilts.Capacity = value;
        }
    }

    public int Count => Positions.Count;

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

    /// <summary>
    /// Linearly resample the polyline by parametric t values in [0, 1] + integer index.
    /// </summary>
    /// <param name="polyTs">Poly t values</param>
    /// <returns>Resampled geometry</returns>
    public PolylineGeometry Sample(List<float> polyTs)
    {
        var g = new PolylineGeometry() { Capacity = polyTs.Count };
        if (Count == 0) return g;
        foreach (var polyT in polyTs)
        {
            var (idx, t) = polyT.ResolvePolyT();
            int nIdx = int.Min(idx + 1, Count - 1);
            g.Positions.Add(Positions[idx].Lerp(Positions[nIdx], t));
            g.Radii.Add(float.Lerp(Radii[idx], Radii[nIdx], t));
            g.Pressures.Add(float.Lerp(Pressures[idx], Pressures[nIdx], t));
            g.Tilts.Add(Tilts[idx].Lerp(Tilts[nIdx], t));
        }

        return g;
    }

    public PolylineGeometry CatmullRomSample(List<float> polyTs)
    {
        var g = new PolylineGeometry() { Capacity = polyTs.Count };
        if (Count == 0) return g;
        if (Count < 3) return Sample(polyTs);

        foreach (var polyT in polyTs)
        {
            var (idx1, t) = polyT.ResolvePolyT();
            int idx0 = idx1 <= 0 ? idx1 : idx1 - 1;
            int idx2 = idx1 >= Count - 1 ? idx1 : idx1 + 1;
            int idx3 = idx2 >= Count - 1 ? idx2 : idx2 + 1;
            var p = Positions[idx0].CatmullRomInterpolation(Positions[idx1], Positions[idx2], Positions[idx3], t);
            var r = Radii[idx0].CatmullRomInterpolation(Radii[idx1], Radii[idx2], Radii[idx3], t);
            var pp = Pressures[idx0].CatmullRomInterpolation(Pressures[idx1], Pressures[idx2], Pressures[idx3], t);
            var tilt = Tilts[idx0].CatmullRomInterpolation(Tilts[idx1], Tilts[idx2], Tilts[idx3], t);
            g.Positions.Add(p);
            g.Radii.Add(r);
            g.Pressures.Add(pp);
            g.Tilts.Add(tilt);
        }

        return g;
    }

    public PolylineGeometry Index(List<int> indices)
    {
        var g = new PolylineGeometry();
        foreach (var idx in indices)
        {
            g.Positions.Add(Positions[idx]);
            g.Radii.Add(Radii[idx]);
            g.Pressures.Add(Pressures[idx]);
            g.Tilts.Add(Tilts[idx]);
        }
        return g;
    }

    #region Debug utilities

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

    #endregion
}