using System;
using System.Collections.Generic;
using Godot;
namespace Ciallo.Geometry;

public class Curve2D
{
    #region Curve2D
    public struct Point
    {
        public Vector2 In;        // Incoming control handle relative to position
        public Vector2 Out;       // Outgoing control handle relative to position
        public Vector2 Position;  // Main control point

        public Point(Vector2 position, Vector2 @in, Vector2 @out)
        {
            Position = position;
            In = @in;
            Out = @out;
        }
    }

    private readonly List<Point> _points = [];

    public int Count => _points.Count;

    public void AddPoint(Vector2 position, Vector2 inHandle, Vector2 outHandle, int at = -1)
    {
        var point = new Point(position, inHandle, outHandle);
        if (at >= 0 && at < _points.Count)
            _points.Insert(at, point);
        else
            _points.Add(point);
    }

    public void RemovePoint(int index)
    {
        if (index >= 0 && index < _points.Count)
            _points.RemoveAt(index);
    }

    public void Clear()
    {
        _points.Clear();
    }

    public Point GetPoint(int index)
    {
        if (index < 0 || index >= _points.Count) throw new ArgumentOutOfRangeException();
        return _points[index];
    }

    // Sample curve segment at index, t in [0,1]
    public Vector2 Sample(int segmentIndex, float t)
    {
        if (_points.Count == 0)
            return new Vector2(0, 0);

        if (_points.Count == 1 || segmentIndex >= _points.Count - 1)
            return _points[^1].Position;

        var p0 = _points[segmentIndex].Position;
        var p1 = p0 + _points[segmentIndex].Out;
        var p3 = _points[segmentIndex + 1].Position;
        var p2 = p3 + _points[segmentIndex + 1].In;

        return p0.BezierInterpolate(p1, p2, p3, t);
    }

    // Sample the whole curve at a fractional position (e.g. 2.5 means between segment 2 and 3 at t=0.5)
    public Vector2 SampleF(float fIndex)
    {
        if (_points.Count == 0)
            return new Vector2(0, 0);

        if (fIndex < 0)
            fIndex = 0;
        else if (fIndex >= _points.Count - 1)
            fIndex = _points.Count - 1;

        int idx = (int)Math.Floor(fIndex);
        float t = fIndex - idx;
        return Sample(idx, t);
    }

    // For display: Returns a polyline that tessellates the curve with n subdivisions per segment
    public List<Vector2> Tessellate(int subdivisionsPerSegment = 16)
    {
        var result = new List<Vector2>();
        if (_points.Count == 0)
            return result;
        if (_points.Count == 1)
        {
            result.Add(_points[0].Position);
            return result;
        }

        for (int i = 0; i < _points.Count - 1; i++)
        {
            for (int j = 0; j < subdivisionsPerSegment; j++)
            {
                float t = (float)j / subdivisionsPerSegment;
                result.Add(Sample(i, t));
            }
        }
        // Add the last point
        result.Add(_points[^1].Position);
        return result;
    }
    
    /// <summary>
    /// Change the position of an existing point.
    /// </summary>
    public void SetPointPosition(int index, Vector2 position)
    {
        if (index < 0 || index >= _points.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        var pt = _points[index];
        pt.Position = position;
        _points[index] = pt;
    }

    /// <summary>
    /// Change the incoming handle of an existing point.
    /// </summary>
    public void SetPointIn(int index, Vector2 inHandle)
    {
        if (index < 0 || index >= _points.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        var pt = _points[index];
        pt.In = inHandle;
        _points[index] = pt;
    }

    /// <summary>
    /// Change the outgoing handle of an existing point.
    /// </summary>
    public void SetPointOut(int index, Vector2 outHandle)
    {
        if (index < 0 || index >= _points.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        var pt = _points[index];
        pt.Out = outHandle;
        _points[index] = pt;
    }

    /// <summary>
    /// Returns the closest point on the curve to a given target.
    /// Brute‐force samples each segment at a fixed resolution (16 subdivisions).
    /// </summary>
    public Vector2 GetClosestPoint(Vector2 toPoint, int subdivisionsPerSegment = 16)
    {
        Vector2 nearest = Vector2.Zero;
        float nearestDist2 = float.MaxValue;

        if (_points.Count == 0)
            return nearest;

        // Iterate each segment
        for (int i = 0; i < _points.Count - 1; i++)
        {
            Vector2 p0 = _points[i].Position;
            Vector2 p1 = p0 + _points[i].Out;
            Vector2 p3 = _points[i + 1].Position;
            Vector2 p2 = p3 + _points[i + 1].In;

            // Sample along the segment
            for (int j = 0; j <= subdivisionsPerSegment; j++)
            {
                float t = (float)j / subdivisionsPerSegment;
                // cubic Bezier: B(t)
                Vector2 sample = p0.BezierInterpolate(p1, p2, p3, t);
                float d2 = sample.DistanceSquaredTo(toPoint);
                if (d2 < nearestDist2)
                {
                    nearestDist2 = d2;
                    nearest = sample;
                }
            }
        }

        // also check the last control point exactly
        Vector2 last = _points[^1].Position;
        float ld2 = last.DistanceSquaredTo(toPoint);
        if (ld2 < nearestDist2)
            nearest = last;

        return nearest;
    }

    /// <summary>
    /// Returns the parametric "offset" along the curve (in [0, segmentCount]) 
    /// where the curve is closest to the given target point. 
    /// Uses the same sampling resolution as GetClosestPoint.
    /// </summary>
    public float GetClosestOffset(Vector2 toPoint, int subdivisionsPerSegment = 16)
    {
        float bestOffset = 0f;
        float nearestDist2 = float.MaxValue;

        if (_points.Count == 0)
            return bestOffset;

        // For each segment
        for (int i = 0; i < _points.Count - 1; i++)
        {
            Vector2 p0 = _points[i].Position;
            Vector2 p1 = p0 + _points[i].Out;
            Vector2 p3 = _points[i + 1].Position;
            Vector2 p2 = p3 + _points[i + 1].In;

            for (int j = 0; j <= subdivisionsPerSegment; j++)
            {
                float t = (float)j / subdivisionsPerSegment;
                Vector2 sample = p0.BezierInterpolate(p1, p2, p3, t);
                float d2 = sample.DistanceSquaredTo(toPoint);
                if (d2 < nearestDist2)
                {
                    nearestDist2 = d2;
                    bestOffset = i + t;
                }
            }
        }

        // consider exactly the last point
        Vector2 last = _points[^1].Position;
        float ld2 = last.DistanceSquaredTo(toPoint);
        if (ld2 < nearestDist2)
            bestOffset = _points.Count - 1;

        return bestOffset;
    }
    #endregion
}