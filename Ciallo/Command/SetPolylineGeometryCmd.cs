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

    public override void BeforeFirstDo(Entity targetE)
    {
        _oldGeometry = targetE.Get<PolylineGeometry>();
    }

    public override void Do(Entity targetE)
    {
        // Data
        targetE.Get<PolylineGeometry>() = _newGeometry;

        // Overlay
        targetE.Get<PolylineWireframe>().SetGeometry(_newGeometry.Positions);

        // Polyline has stroke
        if (targetE.Has<StrokeSetting>())
        {
            // View
            targetE.Get<StrokeView>()
                .SetGeometry(_newGeometry.Positions, _newGeometry.Radii, _newGeometry.Pressures);

            // Cursor detection
            targetE.Get<Body>().SetStrokeShape(_newGeometry.Positions, _newGeometry.Radii);
        }

        // Polyline has fill
        else if (targetE.Has<FilledPolygonSetting>())
        {
            var polygon = _newGeometry.Positions.ToSimplePolygon();
            // View
            targetE.Get<Polygon2D>().Polygon = [..polygon];

            // Cursor detection
            targetE.Get<Body>().SetSimplePolygon(polygon);
        }
    }

    public override void Undo(Entity targetE)
    {
        if (targetE.Has<FilledPolygonSetting>())
        {
            var polygon = _oldGeometry.Positions.ToSimplePolygon();
            // Body
            targetE.Get<Body>().SetSimplePolygon(polygon);

            // View
            targetE.Get<Polygon2D>().Polygon = [..polygon];
        }
        else if (targetE.Has<StrokeSetting>())
        {
            // Body
            targetE.Get<Body>().SetStrokeShape(_oldGeometry.Positions, _oldGeometry.Radii);

            // View
            targetE.Get<StrokeView>()
                .SetGeometry(_oldGeometry.Positions, _oldGeometry.Radii, _oldGeometry.Pressures);
        }

        // Overlay
        targetE.Get<PolylineWireframe>().SetGeometry(_oldGeometry.Positions);

        // Data
        targetE.Get<PolylineGeometry>() = _oldGeometry;
    }
}