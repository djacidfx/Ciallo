using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Data;

public class SetStrokeGeometryCmd : CommandBase
{
    private readonly StrokeGeometry _newGeometry;
    private StrokeGeometry _oldGeometry;
    private readonly Entity _strokeE;

    public SetStrokeGeometryCmd(Entity strokeE, IReadOnlyList<Vector2> newPoints, IReadOnlyList<float> newRadii)
    {
        _strokeE = strokeE;
        _newGeometry = new()
        {
            Points = [..newPoints],
            Radii = [..newRadii],
        };
    }

    public SetStrokeGeometryCmd(Entity strokeE, StrokeGeometry newGeometry)
    {
        _strokeE = strokeE;
        _newGeometry = newGeometry;
    }

    public override void Do()
    {
        // Data
        _oldGeometry ??= _strokeE.Get<StrokeGeometry>();
        _strokeE.Get<StrokeGeometry>() = _newGeometry;

        // View
        _strokeE.Get<StrokeView>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);

        // Overlay
        _strokeE.Get<StrokeOverlay>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);

        // Cursor detection
        _strokeE.Get<CursorDetectionArea>().SetStrokeGeometry(_newGeometry.Points, _newGeometry.Radii);
    }

    public override void Undo()
    {
        // Cursor detection
        _strokeE.Get<CursorDetectionArea>().SetStrokeGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // Overlay
        _strokeE.Get<StrokeOverlay>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // View
        _strokeE.Get<StrokeView>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // Data
        _strokeE.Get<StrokeGeometry>() = _oldGeometry;
    }
}