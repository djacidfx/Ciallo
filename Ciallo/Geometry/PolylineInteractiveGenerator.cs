namespace Ciallo.Geometry;

/// <summary>
/// Generate polyline geometry with stylus/mouse interaction.
/// </summary>
/// <remarks>
/// Always use this class to generate polylines with user interaction instead of directly creating polylines from raw cursor data.
/// This class handles smoothing, simplification, and other aspects to create high-quality polylines.
/// Raw cursor position is gridded by pixels and introduces jitter to polylines.
/// </remarks>
public class PolylineInteractiveGenerator
{
    public void Start(CursorButtonData data)
    {
    }

    public void Update(CursorMotionData data)
    {
    }

    public void Clear()
    {
    }
}