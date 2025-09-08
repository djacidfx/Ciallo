using System;
using System.Collections.Generic;
using Godot;
using MessagePack;

namespace Ciallo.Data;

[MessagePackObject, ToSerialize]
public class StrokeGeometry
{
    [Key(0)] public List<Vector2> Points = [];
    [Key(1)] public List<float> Radii = [];

    public StrokeGeometry DeepClone()
    {
        return new StrokeGeometry()
        {
            Points = [..Points],
            Radii = [..Radii],
        };
    }
}