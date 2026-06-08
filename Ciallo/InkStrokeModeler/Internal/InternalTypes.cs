using System;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal readonly record struct TipState(Vector2 Position, Vector2 Velocity, Vector2 Acceleration, TimeSpan Time);

internal readonly record struct StylusState(float Pressure = -1, float Tilt = -1, float Orientation = -1);

internal readonly record struct RawInputProjection(int SegmentIndex, float RatioAlongSegment);
