using Frent;

namespace Ciallo.Tool;

// Native Arrangement2D.polyline_query_edges returns one dict per crossed halfedge:
// { source_id: long (Entity.PackedValue), from_t: float, to_t: float }.
// from_t/to_t are fractional indices into the source polyline's segment array.
public readonly record struct TrimEdgeHit(Entity SourceShape, float FromT, float ToT);
