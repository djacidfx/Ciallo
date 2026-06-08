using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Ciallo.Geometry;
using Godot;
using Godot.Collections;
using GodotArray = Godot.Collections.Array;

namespace Ciallo.Rendering;

public static class Polygon2DExtension
{
    public static void SetPolygonFromRawRing(this Polygon2D node, ImmutableArray<Vector2> polygon)
    {
        if (polygon.IsDefaultOrEmpty)
        {
            node.Clear();
            return;
        }

        node.SetPolygonFromRawRings([ImmutableCollectionsMarshal.AsArray(polygon)]);
    }

    public static void SetPolygonFromRawRing(this Polygon2D node, ReadOnlySpan<Vector2> polygon)
    {
        if (polygon.IsEmpty)
        {
            node.Clear();
            return;
        }

        node.SetPolygonFromRawRings([polygon.ToArray()]);
    }

    public static void SetPolygonFromRawRing(this Polygon2D node, Vector2[] polygon)
    {
        if (polygon.Length == 0)
        {
            node.Clear();
            return;
        }

        node.SetPolygonFromRawRings([polygon]);
    }

    public static void SetPolygonFromRawRings(this Polygon2D node, Array<Vector2[]> polygons)
    {
        node.SetTriangleResult(Arrangement2D.RepairAndTriangulate(polygons));
    }

    public static void SetTriangleResult(this Polygon2D node, Dictionary triangleResult)
    {
        var vertices = (Vector2[])triangleResult["vertices"];
        var indices = (int[])triangleResult["indices"];

        node.SetPolygon(vertices.AsSpan());
        node.Polygons = ToPolygons(indices);
    }

    public static void SetPolygonWithQueryResult(this Polygon2D node, Arrangement arr, Vector2 point)
    {
        var faceRid = arr.PointQueryFace(point);
        if (!faceRid.IsValid || arr.IsUnboundedFace(faceRid))
        {
            node.Clear();
            return;
        }
        node.SetTriangleResult(arr.GetTrianglesFromFace(faceRid));
    }

    public static void Clear(this Polygon2D node)
    {
        node.Polygon = null;
        node.Polygons = null;
    }

    private static GodotArray ToPolygons(int[] indices)
    {
        GodotArray polygons = [];
        for (int i = 0; i + 2 < indices.Length; i += 3)
            polygons.Add(new[] { indices[i], indices[i + 1], indices[i + 2] });

        return polygons;
    }
}
