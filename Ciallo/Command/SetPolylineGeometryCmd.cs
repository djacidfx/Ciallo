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

        // Overlay
        _polylineE.Get<PolylineWireframe>().SetGeometry(_newGeometry.Positions, _newGeometry.Radii);

        // Polyline has stroke
        if (_polylineE.Has<StrokeBrush>())
        {
            // View
            _polylineE.Get<StrokeView>().SetGeometry(_newGeometry.Positions, _newGeometry.Radii, _newGeometry.Pressures);

            // Cursor detection
            _polylineE.Get<CursorDetectionArea>().SetStrokeShape(_newGeometry.Positions, _newGeometry.Radii);
        }

        // Polyline has fill
        else if (_polylineE.Has<FilledPolygonSetting>())
        {
            // View
            var polygonView = _polylineE.Get<Polygon2D>();
            polygonView.Polygon = [.._newGeometry.Positions];

            // Cursor detection
            _polylineE.Get<CursorDetectionArea>().SetPolygonShape(_newGeometry.Positions);
        }
    }

    public override void Undo()
    {
        if (_polylineE.Has<FilledPolygonSetting>())
        {
            // Cursor detection
            _polylineE.Get<CursorDetectionArea>().SetPolygonShape(_oldGeometry.Positions);

            // View
            var polygonView = _polylineE.Get<Polygon2D>();
            polygonView.Polygon = [.._oldGeometry.Positions];
        }
        else if (_polylineE.Has<StrokeBrush>())
        {
            // Cursor detection
            _polylineE.Get<CursorDetectionArea>().SetStrokeShape(_oldGeometry.Positions, _oldGeometry.Radii);

            // View
            _polylineE.Get<StrokeView>().SetGeometry(_oldGeometry.Positions, _oldGeometry.Radii, _newGeometry.Pressures);
        }

        // Overlay
        _polylineE.Get<PolylineWireframe>().SetGeometry(_oldGeometry.Positions, _oldGeometry.Radii);

        // Data
        _polylineE.Get<PolylineGeometry>() = _oldGeometry;
    }
}