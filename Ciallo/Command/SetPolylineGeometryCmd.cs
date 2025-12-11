using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class SetPolylineGeometryCmd : CommandBase
{
    private readonly PolylineGeometry _newGeometry;
    private PolylineGeometry _oldGeometry;

    public SetPolylineGeometryCmd(PolylineGeometry newGeometry)
    {
        _newGeometry = newGeometry;
    }

    protected override void BeforeFirstDo(Entity polylineE)
    {
        _oldGeometry = polylineE.Get<PolylineGeometry>();
    }

    protected override void Do(Entity polylineE)
    {
        // Data
        polylineE.Get<PolylineGeometry>() = _newGeometry;

        // Overlay
        polylineE.Get<PolylineWireframe>().SetGeometry(_newGeometry.Positions, _newGeometry.Radii);

        // Polyline has stroke
        if (polylineE.Has<StrokeSetting>())
        {
            // View
            polylineE.Get<StrokeView>()
                .SetGeometry(_newGeometry.Positions, _newGeometry.Radii, _newGeometry.Pressures);

            // Cursor detection
            polylineE.Get<CursorDetectionArea>().SetStrokeShape(_newGeometry.Positions, _newGeometry.Radii);
        }

        // Polyline has fill
        else if (polylineE.Has<FilledPolygonSetting>())
        {
            var polygon = _newGeometry.Positions.ToSimplePolygon();
            // View
            var polygonView = polylineE.Get<Polygon2D>();
            polygonView.Polygon = [..polygon];

            // Cursor detection
            polylineE.Get<CursorDetectionArea>().SetSimplePolygon(polygon);
        }
    }

    protected override void Undo(Entity polylineE)
    {
        if (polylineE.Has<FilledPolygonSetting>())
        {
            var polygon = _oldGeometry.Positions.ToSimplePolygon();
            // Cursor detection
            polylineE.Get<CursorDetectionArea>().SetSimplePolygon(polygon);

            // View
            var polygonView = polylineE.Get<Polygon2D>();
            polygonView.Polygon = [..polygon];
        }
        else if (polylineE.Has<StrokeSetting>())
        {
            // Cursor detection
            polylineE.Get<CursorDetectionArea>().SetStrokeShape(_oldGeometry.Positions, _oldGeometry.Radii);

            // View
            polylineE.Get<StrokeView>()
                .SetGeometry(_oldGeometry.Positions, _oldGeometry.Radii, _oldGeometry.Pressures);
        }

        // Overlay
        polylineE.Get<PolylineWireframe>().SetGeometry(_oldGeometry.Positions, _oldGeometry.Radii);

        // Data
        polylineE.Get<PolylineGeometry>() = _oldGeometry;
    }
}