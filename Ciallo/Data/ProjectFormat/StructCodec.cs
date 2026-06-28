using System;
using System.Collections.Generic;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Data;

/// <summary>
/// Maps a creative value type (Color, Vector2, Transform2D, BezierPoint) to a DuckDB STRUCT.
///
/// A codec is the single source of truth for one structured type's project-format contract:
/// its DuckDB column type, how to build a STRUCT literal in an INSERT, how to decompose a
/// CLR value into flat FLOAT leaves (for both single-struct params and list_zip arrays), and
/// how to recompose a value from the Dictionary DuckDB returns on read.
///
/// All leaves are FLOAT. Leaf order is fixed and shared by <see cref="Literal"/>,
/// <see cref="Decompose"/> so that leaf i in the literal always corresponds to leaf i produced
/// by Decompose.
/// </summary>
internal abstract class StructCodec
{
    public abstract Type TargetType { get; }
    public abstract int LeafCount { get; }

    /// <summary>Full DuckDB type, e.g. <c>STRUCT(r FLOAT, g FLOAT, b FLOAT, a FLOAT)</c>.</summary>
    public abstract string DuckDbType { get; }

    /// <summary>
    /// Build a STRUCT literal where <paramref name="leaf"/>(i) yields the SQL expression for leaf i
    /// (a parameter like <c>$p3</c> for a single struct, or <c>e[4]</c> inside a list_transform).
    /// </summary>
    public abstract string Literal(Func<int, string> leaf);

    /// <summary>Flatten a CLR value into <paramref name="leaves"/> (length == <see cref="LeafCount"/>).</summary>
    public abstract void Decompose(object value, float[] leaves);

    /// <summary>Rebuild a CLR value from the Dictionary DuckDB returns for a STRUCT.</summary>
    public abstract object Compose(IReadOnlyDictionary<string, object> dict);

    protected static float F(object o) => Convert.ToSingle(o);

    protected static Vector2 ReadVector2(object o)
    {
        var d = (IReadOnlyDictionary<string, object>)o;
        return new Vector2(F(d["x"]), F(d["y"]));
    }
}

internal sealed class ColorCodec : StructCodec
{
    public override Type TargetType => typeof(Color);
    public override int LeafCount => 4;
    public override string DuckDbType => "STRUCT(r FLOAT, g FLOAT, b FLOAT, a FLOAT)";

    public override string Literal(Func<int, string> leaf) =>
        $"{{'r': {leaf(0)}, 'g': {leaf(1)}, 'b': {leaf(2)}, 'a': {leaf(3)}}}";

    public override void Decompose(object value, float[] leaves)
    {
        var c = (Color)value;
        leaves[0] = c.R;
        leaves[1] = c.G;
        leaves[2] = c.B;
        leaves[3] = c.A;
    }

    public override object Compose(IReadOnlyDictionary<string, object> dict) =>
        new Color(F(dict["r"]), F(dict["g"]), F(dict["b"]), F(dict["a"]));
}

internal sealed class Vector2Codec : StructCodec
{
    public override Type TargetType => typeof(Vector2);
    public override int LeafCount => 2;
    public override string DuckDbType => "STRUCT(x FLOAT, y FLOAT)";

    public override string Literal(Func<int, string> leaf) =>
        $"{{'x': {leaf(0)}, 'y': {leaf(1)}}}";

    public override void Decompose(object value, float[] leaves)
    {
        var v = (Vector2)value;
        leaves[0] = v.X;
        leaves[1] = v.Y;
    }

    public override object Compose(IReadOnlyDictionary<string, object> dict) =>
        new Vector2(F(dict["x"]), F(dict["y"]));
}

internal sealed class Transform2DCodec : StructCodec
{
    public override Type TargetType => typeof(Transform2D);
    public override int LeafCount => 6;

    public override string DuckDbType =>
        "STRUCT(x STRUCT(x FLOAT, y FLOAT), y STRUCT(x FLOAT, y FLOAT), origin STRUCT(x FLOAT, y FLOAT))";

    public override string Literal(Func<int, string> leaf) =>
        $"{{'x': {{'x': {leaf(0)}, 'y': {leaf(1)}}}, " +
        $"'y': {{'x': {leaf(2)}, 'y': {leaf(3)}}}, " +
        $"'origin': {{'x': {leaf(4)}, 'y': {leaf(5)}}}}}";

    public override void Decompose(object value, float[] leaves)
    {
        var t = (Transform2D)value;
        leaves[0] = t.X.X;
        leaves[1] = t.X.Y;
        leaves[2] = t.Y.X;
        leaves[3] = t.Y.Y;
        leaves[4] = t.Origin.X;
        leaves[5] = t.Origin.Y;
    }

    public override object Compose(IReadOnlyDictionary<string, object> dict) =>
        new Transform2D(ReadVector2(dict["x"]), ReadVector2(dict["y"]), ReadVector2(dict["origin"]));
}

internal sealed class BezierPointCodec : StructCodec
{
    public override Type TargetType => typeof(BezierPoint);
    public override int LeafCount => 6;

    // "in" and "out" are DuckDB reserved words, so they must be quoted in the type definition.
    public override string DuckDbType =>
        "STRUCT(p STRUCT(x FLOAT, y FLOAT), \"in\" STRUCT(x FLOAT, y FLOAT), \"out\" STRUCT(x FLOAT, y FLOAT))";

    public override string Literal(Func<int, string> leaf) =>
        $"{{'p': {{'x': {leaf(0)}, 'y': {leaf(1)}}}, " +
        $"'in': {{'x': {leaf(2)}, 'y': {leaf(3)}}}, " +
        $"'out': {{'x': {leaf(4)}, 'y': {leaf(5)}}}}}";

    public override void Decompose(object value, float[] leaves)
    {
        var b = (BezierPoint)value;
        leaves[0] = b.P.X;
        leaves[1] = b.P.Y;
        leaves[2] = b.In.X;
        leaves[3] = b.In.Y;
        leaves[4] = b.Out.X;
        leaves[5] = b.Out.Y;
    }

    public override object Compose(IReadOnlyDictionary<string, object> dict) =>
        new BezierPoint(ReadVector2(dict["p"]), ReadVector2(dict["in"]), ReadVector2(dict["out"]));
}

internal static class StructCodecRegistry
{
    private static readonly Dictionary<Type, StructCodec> Codecs = new()
    {
        [typeof(Color)] = new ColorCodec(),
        [typeof(Vector2)] = new Vector2Codec(),
        [typeof(Transform2D)] = new Transform2DCodec(),
        [typeof(BezierPoint)] = new BezierPointCodec(),
    };

    public static bool TryGet(Type type, out StructCodec codec) => Codecs.TryGetValue(type, out codec);

    public static StructCodec Get(Type type) => Codecs[type];
}
