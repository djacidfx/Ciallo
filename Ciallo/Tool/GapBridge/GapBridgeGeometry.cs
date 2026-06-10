using System.Collections.Generic;
using Frent;
using Godot.Collections;

namespace Ciallo.Tool;

public readonly record struct GapBridgeCandidate(
    Entity FromCurve,
    float FromT,
    Entity ToCurve,
    float ToT,
    float Score);

public static class GapBridgeGeometry
{
    public static List<GapBridgeCandidate> ParseCandidates(Array<Dictionary> raw)
    {
        var result = new List<GapBridgeCandidate>(raw.Count);
        foreach (var dict in raw)
        {
            long fromCurveId = (long)dict["from_curve_id"];
            float fromT = (float)dict["from_t"];
            long toCurveId = (long)dict["to_curve_id"];
            float toT = (float)dict["to_t"];
            float score = (float)dict["score"];
            result.Add(new GapBridgeCandidate(fromCurveId.ToEntity(), fromT, toCurveId.ToEntity(), toT, score));
        }
        return result;
    }
}
