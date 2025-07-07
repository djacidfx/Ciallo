using System;
using System.Linq;
using Ciallo;
using GdUnit4;
using static GdUnit4.Assertions;
using MemoryPack;
using R3;

namespace Tests;

[TestSuite, RequireGodotRuntime]
public class TestPolyline
{
    [TestCase]
    public void SaveLoad()
    {
        var rng = new Random();
        var poly = new Polyline(
            [rng.NextVector2(), rng.NextVector2(), rng.NextVector2()],
            [rng.NextSingle(), rng.NextSingle(), rng.NextSingle()]
        );
        
        var x = new ReactiveProperty<Polyline>(poly);
        var b = MemoryPackSerializer.Serialize(x);
        var v = MemoryPackSerializer.Deserialize<ReactiveProperty<Polyline>>(b);
        AssertBool(x.Value.SequenceEqual(v.Value));
        
        var bin = MemoryPackSerializer.Serialize(poly);
        var val = MemoryPackSerializer.Deserialize<Polyline>(bin);
        AssertBool(poly.SequenceEqual(val)).IsTrue();
    }
}
