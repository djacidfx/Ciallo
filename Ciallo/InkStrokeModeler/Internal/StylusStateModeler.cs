using System;
using System.Collections.Generic;
using System.Numerics;

namespace InkStrokeModeler.Internal;

internal sealed class StylusStateModeler
{
    private sealed class ModelerState
    {
        public bool ReceivedUnknownPressure;
        public bool ReceivedUnknownTilt;
        public bool ReceivedUnknownOrientation;
        public readonly List<ModelerResult> RawInputAndStylusStates = [];
        public int FirstRawInputIndex;
        public RawInputProjection Projection;

        public ModelerState Clone()
        {
            ModelerState clone = new()
            {
                ReceivedUnknownPressure = ReceivedUnknownPressure,
                ReceivedUnknownTilt = ReceivedUnknownTilt,
                ReceivedUnknownOrientation = ReceivedUnknownOrientation,
                FirstRawInputIndex = 0,
                Projection = Projection,
            };
            clone.RawInputAndStylusStates.EnsureCapacity(Count);
            for (int i = FirstRawInputIndex; i < RawInputAndStylusStates.Count; i++)
                clone.RawInputAndStylusStates.Add(RawInputAndStylusStates[i]);
            return clone;
        }

        public int Count => RawInputAndStylusStates.Count - FirstRawInputIndex;

        public ModelerResult this[int index] => RawInputAndStylusStates[FirstRawInputIndex + index];

        public ModelerResult Last => RawInputAndStylusStates[^1];

        public void Add(ModelerResult state) => RawInputAndStylusStates.Add(state);

        public void Clear()
        {
            RawInputAndStylusStates.Clear();
            FirstRawInputIndex = 0;
            Projection = default;
        }

        public void RemoveFirst()
        {
            FirstRawInputIndex++;
            if (FirstRawInputIndex <= 32 || FirstRawInputIndex <= RawInputAndStylusStates.Count / 2) return;

            RawInputAndStylusStates.RemoveRange(0, FirstRawInputIndex);
            FirstRawInputIndex = 0;
        }
    }

    private ModelerState _state = new();
    private ModelerState? _savedState;
    private StylusStateModelerParams _params = new();

    public void Reset(StylusStateModelerParams parameters)
    {
        _state = new ModelerState();
        _savedState = null;
        _params = parameters;
    }

    public StylusStateModeler CloneForPrediction() => new()
    {
        _state = _state.Clone(),
        _savedState = _savedState?.Clone(),
        _params = _params,
    };

    public void Update(Vector2 position, TimeSpan time, StylusState state)
    {
        if (state.Pressure < 0 || float.IsNaN(state.Pressure)) _state.ReceivedUnknownPressure = true;
        if (state.Tilt < 0 || float.IsNaN(state.Tilt)) _state.ReceivedUnknownTilt = true;
        if (state.Orientation < 0 || float.IsNaN(state.Orientation)) _state.ReceivedUnknownOrientation = true;

        if (!_params.UseStrokeNormalProjection &&
            _state.ReceivedUnknownPressure &&
            _state.ReceivedUnknownTilt &&
            _state.ReceivedUnknownOrientation)
        {
            _state.Clear();
            return;
        }

        Vector2 velocity = Vector2.Zero;
        Vector2 acceleration = Vector2.Zero;
        if (_state.Count > 0 &&
            time != _state.Last.Time)
        {
            ModelerResult last = _state.Last;
            float deltaSeconds = (float)(time - last.Time).TotalSeconds;
            velocity = (position - last.Position) / deltaSeconds;
            acceleration = (velocity - last.Velocity) / deltaSeconds;
        }

        _state.Add(new ModelerResult(
            position,
            velocity,
            acceleration,
            time,
            state.Pressure,
            state.Tilt,
            state.Orientation));

        while (_state.Count > _params.MaxInputSamples)
            _state.RemoveFirst();
    }

    public ModelerResult Project(TipState tip, Vector2? strokeNormal)
    {
        if (_state.Count == 0) return default;

        _state.Projection = _params.UseStrokeNormalProjection && strokeNormal.HasValue
            ? ProjectAlongStrokeNormal(tip.Position, tip.Acceleration, strokeNormal.Value, _state, _state.Projection)
            : ProjectToClosestPoint(tip.Position, _state, _state.Projection);

        while (_state.Projection.SegmentIndex > 0)
        {
            _state.Projection = _state.Projection with { SegmentIndex = _state.Projection.SegmentIndex - 1 };
            _state.RemoveFirst();
        }

        ModelerResult projected = _state.Count > 1
            ? Utils.InterpResult(_state[0], _state[1], _state.Projection.RatioAlongSegment)
            : _state[0];

        projected = projected with { Time = tip.Time };
        if (_state.ReceivedUnknownPressure) projected = projected with { Pressure = -1 };
        if (_state.ReceivedUnknownTilt) projected = projected with { Tilt = -1 };
        if (_state.ReceivedUnknownOrientation) projected = projected with { Orientation = -1 };
        return projected;
    }

    public void Save() => _savedState = _state.Clone();

    public void Restore()
    {
        if (_savedState is not null) _state = _savedState.Clone();
    }

    private static RawInputProjection ProjectAlongStrokeNormal(
        Vector2 position,
        Vector2 acceleration,
        Vector2 strokeNormal,
        ModelerState rawInputPolyline,
        RawInputProjection previousProjection)
    {
        RawInputProjection? bestLeftProjection = null;
        RawInputProjection? bestRightProjection = null;
        float bestDistanceLeft = float.PositiveInfinity;
        float bestDistanceRight = float.PositiveInfinity;
        int endIndex = rawInputPolyline.Count - 1;

        for (int i = previousProjection.SegmentIndex; i < endIndex; i++)
        {
            Vector2 segmentStart = rawInputPolyline[i].Position;
            Vector2 segmentEnd = rawInputPolyline[i + 1].Position;
            float? segmentRatio = Utils.ProjectToSegmentAlongNormal(segmentStart, segmentEnd, position, strokeNormal);
            if (!segmentRatio.HasValue) continue;
            if (i == previousProjection.SegmentIndex && segmentRatio.Value <= previousProjection.RatioAlongSegment) continue;

            Vector2 projection = Utils.Interp(segmentStart, segmentEnd, segmentRatio.Value);
            float distance = Utils.Distance(position, projection);
            RawInputProjection candidate = new(i, segmentRatio.Value);
            float dot = Vector2.Dot(projection - position, strokeNormal);
            if (dot == 0) return candidate;

            if (dot < 0)
                MaybeUpdate(candidate, distance, ref bestRightProjection, ref bestDistanceRight);
            else
                MaybeUpdate(candidate, distance, ref bestLeftProjection, ref bestDistanceLeft);
        }

        if (bestLeftProjection.HasValue && bestRightProjection.HasValue)
            return Vector2.Dot(strokeNormal, acceleration) > 0 ? bestRightProjection.Value : bestLeftProjection.Value;

        return bestRightProjection ?? bestLeftProjection ?? previousProjection;

        static void MaybeUpdate(RawInputProjection candidate, float distance, ref RawInputProjection? bestProjection, ref float bestDistance)
        {
            if (distance >= bestDistance) return;
            bestProjection = candidate;
            bestDistance = distance;
        }
    }

    private static RawInputProjection ProjectToClosestPoint(
        Vector2 position,
        ModelerState rawInputPolyline,
        RawInputProjection previousProjection)
    {
        RawInputProjection? bestProjection = null;
        float minDistance = float.PositiveInfinity;
        int endIndex = rawInputPolyline.Count - 1;

        for (int i = 0; i < endIndex; i++)
        {
            Vector2 segmentStart = rawInputPolyline[i].Position;
            Vector2 segmentEnd = rawInputPolyline[i + 1].Position;
            float segmentRatio = Utils.NearestPointOnSegment(segmentStart, segmentEnd, position);
            if (i == previousProjection.SegmentIndex && segmentRatio < previousProjection.RatioAlongSegment) continue;

            float distance = Utils.Distance(position, Utils.Interp(segmentStart, segmentEnd, segmentRatio));
            if (distance <= minDistance)
            {
                bestProjection = new RawInputProjection(i, segmentRatio);
                minDistance = distance;
            }
        }

        return bestProjection ?? previousProjection;
    }
}
