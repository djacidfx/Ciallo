using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using MemoryPack;
using ObservableCollections;

namespace Ciallo;

[Tool, GlobalClass]
public partial class Polyline : Resource, IEnumerable<Tuple<Vector2, float>>
{
    public ObservableList<Vector2> Points { get; init; }
    public ObservableList<float> Radii { get; init; }
    [MemoryPackIgnore]
    public Rect2 BoundingBox
    {
        get
        {
            // Calculate the bounding box of polyline Points and Radii
            if (Points.Count == 0) return default;
            Vector2 min = Points[0];
            Vector2 max = Points[0];

            for (int i = 0; i < Points.Count; i++)
            {
                Vector2 p = Points[i];
                float r = Radii[i];
                Vector2 ElementMax(Vector2 v1, Vector2 v2) => 
                    new (Single.Max(v1.X, v2.X), Single.Max(v1.Y, v2.Y));
                Vector2 ElementMin(Vector2 v1, Vector2 v2) => 
                    new (Single.Min(v1.X, v2.X), Single.Min(v1.Y, v2.Y));
                min = ElementMin(min, p - Vector2.One * r);
                max = ElementMax(max, p + Vector2.One * r);
            }
            return new(min, max-min);
        }
    }

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