using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Godot;
namespace Ciallo.Geometry;  

/// <summary>
/// This class is a combination of Godot's Curve2D and Curve.
/// It's mainly built upon Curve2D's data structure. To be used as a Curve, it must be X-monotone.
/// It largely changes the interfaces of Curve to fit our usage.
/// </summary>
[DataContract]
public class PolyBezier
{
    #region Curve2D
    // Members from godot's `Curve2D` class.
    [DataContract]
    public struct Point
    {
        [DataMember(Order = 0)] public Vector2 In;        // Incoming control handle relative to position
        [DataMember(Order = 1)] public Vector2 Out;       // Outgoing control handle relative to position
        [DataMember(Order = 2)] public Vector2 Position;  // Main control point
        
        public Point(Vector2 position, Vector2 @in, Vector2 @out)
        {
            Position = position;
            In = @in;
            Out = @out;
        }
        
        public HandleControlMode EstimatedHandleMode
        {
            get
            {
                bool isEqual = MathF.Abs(In.Length() - Out.Length()) < 1e-5f;
                bool isLinear = MathF.Abs(MathF.Abs(In.Angle() - Out.Angle()) - MathF.PI) < 1e-5f;
                if (isEqual && isLinear)
                    return HandleControlMode.LinearEqual;
                if (isLinear)
                    return HandleControlMode.Linear;
                return HandleControlMode.Free;
            }
            set
            {
                switch (value)
                {
                    case HandleControlMode.LinearEqual:
                        In = -Out;
                        break;
                    case HandleControlMode.Linear:
                        In = -Out.Normalized() * In.Length();
                        break;
                    case HandleControlMode.Free:
                        // Do nothing, already free
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
    }
    [DataMember(Order = 0)]
    public IReadOnlyList<Point> Points
    {
        get => _points;
        set
        {
            _points = value.ToList();
            OnChanged();
        }
    }

    private List<Point> _points = [];
    private readonly List<Vector2> _cachedPolyline = [];
    private readonly List<float> _cachedT = []; // Fractional T values for the cached polyline
    private Rect2 _cachedBoundingBox = default;
    
    public int Count => _points.Count;

    public Rect2 BoundingBox
    {
        get
        {
            if (_cachedBoundingBox == default)
            {
                Tessellate();
            }
            return _cachedBoundingBox;
        }
    }
    
    private void OnChanged() => ClearCache();
    public void ClearCache()
    {
        _cachedPolyline.Clear();
        _cachedBoundingBox = default;
        _cachedT.Clear();
    }

    public void AddPoint(Vector2 position, Vector2 inHandle, Vector2 outHandle, int at = -1)
    {
        var point = new Point(position, inHandle, outHandle);
        if (at >= 0 && at < _points.Count)
            _points.Insert(at, point);
        else
            _points.Add(point);
        OnChanged();
    }

    public void RemovePoint(int index)
    {
        if (index >= 0 && index < _points.Count)
        {
            _points.RemoveAt(index);
            OnChanged();
        }
    }

    public void Clear()
    {
        _points.Clear();
        OnChanged();
    }

    public Point GetPoint(int index)
    {
        if (index < 0 || index >= _points.Count) throw new ArgumentOutOfRangeException();
        return _points[index];
    }

    // Sample curve segment at index, t in [0,1]
    public Vector2 Sample(int index, float segT)
    {
        if (_points.Count == 0)
            throw new InvalidOperationException("Cannot sample from an empty curve.");

        if (_points.Count == 1 || index >= _points.Count - 1)
            return _points[^1].Position;

        var p0 = _points[index].Position;
        var p1 = p0 + _points[index].Out;
        var p3 = _points[index + 1].Position;
        var p2 = p3 + _points[index + 1].In;

        return p0.BezierInterpolate(p1, p2, p3, segT);
    }

    // Sample the whole curve at a fractional t (e.g. 2.5 means between segment 2 and 3 at t=0.5)
    public Vector2 Sample(float polyT)
    {
        if (_points.Count == 0)
            throw new InvalidOperationException("Cannot sample from an empty curve.");

        if (polyT < 0)
            polyT = 0;
        else if (polyT >= _points.Count - 1)
            polyT = _points.Count - 1;

        int idx = (int)Math.Floor(polyT);
        float t = polyT - idx;
        return Sample(idx, t);
    }

    /// <summary>
    /// Insert a bezier control point at a fractional t. Do not change the shape of original curve.
    /// </summary>
    /// <returns>The index of added point.</returns>
    public int Split(float polyT)
    {
        if (_points.Count < 2) return -1;
        // clamp to valid segment range
        if (polyT <= 0f || polyT >= _points.Count - 1) return -1;
        int idx = (int)Math.Floor(polyT);
        float t = polyT - idx;
        if (t <= 0f || t >= 1f) return -1;
        // original segment endpoints and controls
        var left = _points[idx];
        var right = _points[idx + 1];
        Vector2 p0 = left.Position;
        Vector2 p1 = p0 + left.Out;
        Vector2 p3 = right.Position;
        Vector2 p2 = p3 + right.In;
        
        var p01 = p0.Lerp(p1, t);
        var p12 = p1.Lerp(p2, t);
        var p23 = p2.Lerp(p3, t);
        var p012 = p01.Lerp(p12, t);
        var p123 = p12.Lerp(p23, t);
        var p0123 = p012.Lerp(p123, t);
        // compute new handles
        left.Out = p01 - p0;
        var newIn = p012 - p0123;
        var newOut = p123 - p0123;
        right.In = p23 - p3;
        // apply changes: update left, insert new, update right
        _points[idx] = left;
        _points.Insert(idx + 1, new(p0123, newIn, newOut));
        _points[idx + 2] = right;
        OnChanged();
        return idx + 1;
    }

    /// <summary>
    /// Insert a list of bezier control points at a list of fractional t.
    /// </summary>
    public void Split([NotNull] IReadOnlyList<float> polyT)
    {
        if (_points.Count < 2) return;
        // sort splits descending to avoid index shift issues
        var ts = polyT.Where(t => t > 0f && t < _points.Count - 1)
                   .Distinct()
                   .OrderByDescending(t => t)
                   .ToList();
        foreach (var t in ts) Split(t);
    }
    
    public void Tessellate(int subdivisionsPerSegment = 16)
    {
        ClearCache();
        if (_points.Count == 0)
            return;
        if (_points.Count == 1)
        {
            _cachedPolyline.Add(_points[0].Position);
            return;
        }

        for (int i = 0; i < _points.Count - 1; i++)
        {
            for (int j = 0; j < subdivisionsPerSegment; j++)
            {
                float t = (float)j / subdivisionsPerSegment;
                _cachedPolyline.Add(Sample(i, t));
                _cachedT.Add(i + t);
            }
        }
        // Add the last point
        _cachedPolyline.Add(_points[^1].Position);
        _cachedT.Add(_points.Count - 1);
        _cachedBoundingBox = _cachedPolyline.GetBoundingBox();
    }
    
    /// <summary>
    /// Change the position of an existing point.
    /// </summary>
    public void SetPointPosition(int index, Vector2 position)
    {
        var pt = _points[index];
        pt.Position = position;
        _points[index] = pt;
        OnChanged();
    }

    /// <summary>
    /// Change the incoming handle of an existing point.
    /// </summary>
    public void SetPointIn(int index, Vector2 inHandle)
    {
        var pt = _points[index];
        pt.In = inHandle;
        _points[index] = pt;
        OnChanged();
    }

    public void SetPointInLinearly(int index, Vector2 inHandle)
    {
        var pt = _points[index];
        var angleDelta = inHandle.Angle() - pt.In.Angle();
        var lengthDelta = inHandle.Length() - pt.In.Length();
        pt.In = inHandle;
        pt.Out = pt.Out.Normalized().Rotated(angleDelta) * (pt.Out.Length() + lengthDelta);
        _points[index] = pt;
        OnChanged();
    }
    /// <summary>
    /// Change the outgoing handle of an existing point.
    /// </summary>
    public void SetPointOut(int index, Vector2 outHandle)
    {
        var pt = _points[index];
        pt.Out = outHandle;
        _points[index] = pt;
        OnChanged();
    }
    
    public void SetPointOutLinearly(int index, Vector2 outHandle)
    {
        var pt = _points[index];
        var angleDelta = outHandle.Angle() - pt.Out.Angle();
        var lengthDelta = outHandle.Length() - pt.Out.Length();
        pt.Out = outHandle;
        pt.In = pt.In.Normalized().Rotated(angleDelta) * (pt.In.Length() + lengthDelta);
        _points[index] = pt;
        OnChanged();
    }

    /// <summary>
    /// Get the closest point on the curve to a given point p.
    /// </summary>
    /// <returns>Point position, output fractional t of the close point</returns>
    public Vector2 GetClosestPoint(Vector2 p, out float t)
    {
        if(_cachedT.Count == 0)
        {
            Tessellate();
        }
        _cachedPolyline.GetClosestPoint(p, out var polyT);
        var polyI = (int)MathF.Floor(polyT);
        if(polyI >= _cachedT.Count - 1)
        {
            t = _cachedT[^1];
            return _cachedPolyline[^1];
        }
        var deltaT = (polyT - polyI) * (_cachedT[polyI + 1] - _cachedT[polyI]);
        t = _cachedT[polyI] + deltaT;
        return Sample(t);
    }
    #endregion

    #region Curve
    
    /// <summary>
    /// Check if the curve is a monotone in X.
    /// So that each element of the function's domain X maps to a single element of its range Y.
    /// All methods in this region assume the curve is X-monotone.
    /// </summary>
    public bool IsXMonotone
    {
        get
        {
            for (int i = 0; i < _points.Count - 1; i++)
            {
                var p0 = _points[i].Position;
                var p1 = p0 + _points[i].Out;
                var p3 = _points[i + 1].Position;
                var p2 = p3 + _points[i + 1].In;

                // derivative coefficients for x′(t) = ax·t² + bx·t + cx
                float ax = -3 * p0.X + 9 * p1.X - 9 * p2.X + 3 * p3.X;
                float bx =  6 * p0.X -12 * p1.X + 6 * p2.X;
                float cx = -3 * p0.X + 3 * p1.X;

                if (MathF.Abs(ax) < 1e-5f)
                {
                    // linear case: bx·t + cx = 0 ⇒ t = -cx/bx
                    if (MathF.Abs(bx) < 1e-5f)
                        continue; // constant derivative, no sign change
                    float t = -cx / bx;
                    if (t > 0 && t < 1) 
                        return false;
                }
                else
                {
                    float disc = bx * bx - 4 * ax * cx;
                    if (disc <= 1e-5f) 
                        continue; // no real or double root ⇒ no sign change
                    float sqrtD = MathF.Sqrt(disc);
                    float t1 = (-bx + sqrtD) / (2 * ax);
                    float t2 = (-bx - sqrtD) / (2 * ax);
                    if ((t1 > 0 && t1 < 1) || (t2 > 0 && t2 < 1))
                        return false;
                }
            }
            return true;
        }
    }
    
    // Members from Godot's `Curve` class.
    public float MinX
    {
        get
        {
            if(_cachedBoundingBox == default) Tessellate();
            return _cachedBoundingBox.Position.X;
        }
    }
    public float MaxX
    {
        get
        {
            if(_cachedBoundingBox == default) Tessellate();
            return _cachedBoundingBox.End.X;
        }
    }
    public float MinY
    {
        get
        {
            if(_cachedBoundingBox == default) Tessellate();
            return _cachedBoundingBox.Position.Y;
        }
    }
    
    public float MaxY
    {
        get
        {
            if(_cachedBoundingBox == default) Tessellate();
            return _cachedBoundingBox.End.Y;
        }
    }

    public float XRange => MaxX - MinX;
    public float YRange => MaxY - MinY;

    /// <summary>
    /// Returns the Y value for the point at the X position.
    /// </summary>
    public float SampleX(float x)
    {
        if (_cachedPolyline.Count == 0) Tessellate();
        return _cachedPolyline.SampleX(x);
    }

    public Vector2 GetPointPosition(int i) => GetPoint(i).Position;
    public float GetPointInTangent(int i) => GetPoint(i).In.Y / GetPoint(i).In.X;
    public float GetPointOutTangent(int i) => GetPoint(i).Out.Y / GetPoint(i).Out.X;
    public void SetPointInTangent(int i, float tangent)
    {
        var pt = GetPoint(i);
        var oldHandle = pt.In;
        float length = oldHandle.Length();
        if (length > 0f)
        {
            // preserve handle orientation
            float sign = oldHandle.X != 0f ? MathF.Sign(oldHandle.X) : MathF.Sign(oldHandle.Y);
            // unit vector matching requested slope
            var baseDir = new Vector2(1f, tangent).Normalized();
            // assign new in-handle with original length
            pt.In = baseDir * sign * length;
        }
        _points[i] = pt;
        OnChanged();
    }
    public void SetPointOutTangent(int i, float tangent)
    {
        var pt = GetPoint(i);
        var oldHandle = pt.Out;
        float length = oldHandle.Length();
        if (length > 0f)
        {
            float sign = oldHandle.X != 0f ? MathF.Sign(oldHandle.X) : MathF.Sign(oldHandle.Y);
            var baseDir = new Vector2(1f, tangent).Normalized();
            pt.Out = baseDir * sign * length;
        }
        _points[i] = pt;
        OnChanged();
    }
    
    #endregion

    /// <summary>
    /// Shen: Godot's Curve2D's tangent mode is very, very unintuitive. I guess who programed it have never used Adobe Illustrator or Inkscape.
    /// </summary>
    public enum HandleControlMode
    {
        LinearEqual, // Two handles are equal in length and tangent (opposite direction)
        Linear, // Equal in tangent only
        Free
    }
}