using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using MemoryPack;
using ObservableCollections;

namespace Ciallo;

[Tool, GlobalClass, MemoryPackable]
public partial class Polyline : Resource, IEnumerable<Tuple<Vector2, float>>
{
    public ObservableList<Vector2> Points { get; init; }
    public ObservableList<float> Radii { get; init; }
    
    [MemoryPackConstructor]
    public Polyline()
    {
        Points = [];
        Radii = [];
    }

    public Polyline(List<Vector2> points, List<float> radii)
    {
        Points = new(points);
        Radii = new(radii);
    }

    public IEnumerator<Tuple<Vector2, float>> GetEnumerator()
    {
        for (int i = 0; i < Points.Count && i < Radii.Count; i++)
        {
            yield return Tuple.Create(Points[i], Radii[i]);
        }
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(Vector2 p, float r)
    {
        Points.Add(p);
        Radii.Add(r);
        var x = new Array<Vector2>(Points);
    }
    
    // Expose to godot editor
    [Export, MemoryPackIgnore] public Array<Vector2> LinePoints
    {
        get => new(Points);
        set
        {
            if(Points.SequenceEqual(value)) return;
            Points.Clear();
            Points.AddRange(value);
            EmitChanged();
        }
    }
    [Export, MemoryPackIgnore] public Array<float> LineRadii
    {
        get => new(Radii);
        set
        {
            if(Radii.SequenceEqual(value)) return;
            Radii.Clear();
            Radii.AddRange(value);
            EmitChanged();
        }
    }
}