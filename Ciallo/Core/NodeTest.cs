using Godot;
using System;
using MemoryPack;
using R3;

namespace Ciallo;

public partial class NodeTest : Node
{
    public override void _Ready()
    {
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>));
        var rng = new Random();
        var x = new ReactiveProperty<int>(rng.Next());
        var b = MemoryPackSerializer.Serialize(x);
        var v = MemoryPackSerializer.Deserialize<ReactiveProperty<int>>(b);
        GD.Print(v.Value);
        GD.Print(x.Value);
    }
}
