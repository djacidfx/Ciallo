using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using MemoryPack;

namespace Ciallo;

[Tool, GlobalClass, MemoryPackable]
public partial class Polyline : Resource, IEnumerable<Tuple<Vector2, float>>
{
    public List<Vector2> Points { get; set; } = [];
    public List<float> Radii { get; set; } = [];
    
    [MemoryPackConstructor]
    public Polyline()
    {
        
    }

    public Polyline(List<Vector2> points, List<float> radii)
    {
        Points = points;
        Radii = radii;
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
    
    [Export, MemoryPackIgnore] public Array<Vector2> LinePoints
    {
        get => new(Points);
        set
        {
            if(Points.SequenceEqual(value)) return;
            Points = value.ToList();
            EmitChanged();
        }
    }
    [Export, MemoryPackIgnore] public Array<float> LineRadii
    {
        get => new(Radii);
        set
        {
            if(Radii.SequenceEqual(value)) return;
            Radii = value.ToList();
            EmitChanged();
        }
    }
}