using System;
using System.Linq;
using Ciallo;
using GdUnit4;
using static GdUnit4.Assertions;
using MemoryPack;

namespace Tests;

[TestSuite, RequireGodotRuntime]
public class TestPolyline
{
    [TestCase]
    public void SaveLoad()
    {
        var rng = new Random();
        var poly = new Polyline([rng.NextVector2(), rng.NextVector2()], [rng.NextSingle(), rng.NextSingle()]);
        var bin = MemoryPackSerializer.Serialize(poly);
        var v = MemoryPackSerializer.Deserialize<Polyline>(bin);
        AssertBool(poly.SequenceEqual(v)).IsTrue();
    }
}
