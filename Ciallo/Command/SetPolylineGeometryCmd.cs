using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Data;

public class SetPolylineGeometryCmd : CommandBase
{
    private readonly PolylineGeometry _newGeometry;
    private PolylineGeometry _oldGeometry;
    private readonly Entity _polylineE;

    public SetPolylineGeometryCmd(Entity polylineE, IReadOnlyList<Vector2> newPoints, IReadOnlyList<float> newRadii)
    {
        _polylineE = polylineE;
        _newGeometry = new()
        {
            Points = [..newPoints],
            Radii = [..newRadii],
        };
    }

    public SetPolylineGeometryCmd(Entity polylineE, PolylineGeometry newGeometry)
    {
        _polylineE = polylineE;
        _newGeometry = newGeometry;
    }

    public override void Do()
    {
        // Data
        _oldGeometry ??= _polylineE.Get<PolylineGeometry>();
        _polylineE.Get<PolylineGeometry>() = _newGeometry;

        // View
        _polylineE.Get<StrokeView>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);

        // Overlay
        _polylineE.Get<PolylineWireframe>().SetGeometry(_newGeometry.Points, _newGeometry.Radii);

        // Cursor detection
        _polylineE.Get<CursorDetectionArea>().SetStrokeGeometry(_newGeometry.Points, _newGeometry.Radii);
    }

    public override void Undo()
    {
        // Cursor detection
        _polylineE.Get<CursorDetectionArea>().SetStrokeGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // Overlay
        _polylineE.Get<PolylineWireframe>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // View
        _polylineE.Get<StrokeView>().SetGeometry(_oldGeometry.Points, _oldGeometry.Radii);

        // Data
        _polylineE.Get<PolylineGeometry>() = _oldGeometry;
    }
}