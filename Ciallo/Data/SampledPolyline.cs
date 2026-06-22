using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DataContract, ToSerialize]
public class SampledPolyline
{
    [DataMember(Order = 0), ProjectField]
    public ReactiveProperty<ImmutableArray<Vector2>> Positions = new([]);
    [DataMember(Order = 1), ProjectField]
    public ReactiveProperty<ImmutableArray<float>> Radii = new([]);
    [DataMember(Order = 2), ProjectField]
    public ReactiveProperty<ImmutableArray<float>> Pressures = new([]);
    [DataMember(Order = 3), ProjectField]
    public ReactiveProperty<ImmutableArray<Vector2>> Tilts = new([]);

    public int Count => Positions.Value.Length;
    public int Length => Count;

    public SampledPolyline Clone()
    {
        return new SampledPolyline()
        {
            Positions = { Value = Positions.Value },
            Radii = { Value = Radii.Value },
            Pressures = { Value = Pressures.Value },
            Tilts = { Value = Tilts.Value },
        };
    }

    /// <summary>
    /// Observe position and radius changes together. Debounce frame.
    /// </summary>
    public Observable<(ImmutableArray<Vector2>, ImmutableArray<float>)> ObserveShape()
    {
        return Positions
            .CombineLatest(Radii, (p, r) => (p, r));
    }

    public Observable<(ImmutableArray<Vector2>, ImmutableArray<float>, ImmutableArray<float>, ImmutableArray<Vector2>)> ObserveAll()
    {
        return Positions
            .CombineLatest(Radii, Pressures, Tilts,
                (p, r, pr, tt) => (p, r, pr, t: tt));
    }


    #region Debug utilities

    public string Describe(int sampleCount = 4)
    {
        var positions = Positions.Value;
        var radii = Radii.Value;
        var pressures = Pressures.Value;
        var tilts = Tilts.Value;

        if (positions.Length == 0)
        {
            return $"PolylineGeometry(Pos={Positions.Value.Length}, Radii={Radii.Value.Length}, Pressures={Pressures.Value.Length}, Tilts={Tilts.Value.Length}, Sample=[Ø])";
        }

        var count = Math.Min(sampleCount, positions.Length);
        var samples = Enumerable.Range(0, count).Select(FormatSample);
        var sample = string.Join("\n", samples);
        var suffix = positions.Length > sampleCount ? $"\n  ... ({positions.Length - sampleCount} more)" : string.Empty;
        return $"PolylineGeometry(Pos={positions.Length}, Radii={radii.Length}, Pressures={pressures.Length}, Tilts={tilts.Length})\nSamples:\n{sample}{suffix}";
    }

    public override string ToString() => Describe();

    private string DebuggerDisplay => Describe(2);

    private string FormatSample(int index)
    {
        var positions = Positions.Value;
        var radii = Radii.Value;
        var pressures = Pressures.Value;
        var tilts = Tilts.Value;

        var pos = FormatVector(positions[index]);
        var radius = index < radii.Length ? $"{radii[index],6:F1}" : "     ?";
        var pressure = index < pressures.Length ? $"{pressures[index],5:F2}" : "    ?";
        var tilt = index < tilts.Length ? FormatVector(tilts[index]) : "          ?";
        return $"  [{index,3}] Pos={pos,16} R={radius} P={pressure} Tilt={tilt}";
    }

    private static string FormatVector(Vector2 v) => $"({v.X,6:F1},{v.Y,6:F1})";

    #endregion
}
