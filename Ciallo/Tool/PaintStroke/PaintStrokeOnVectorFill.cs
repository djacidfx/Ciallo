using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Tool;

public class PaintStrokeOnVectorFill : PaintStrokeInteractor
{
    public override void End(CursorButtonData data)
    {
        var layers = WorkingLayer.Get<VectorFillLayerSetting>().ReferenceLayers;
        if (layers.Count <= 0) return;
        var targetShapeLayer = layers.First();
        new CommandBuilder(WorkingLayer.World.Create())
            .NewStroke()
            .AddToLayerTree(targetShapeLayer)
            .SetProperty(e => e.Get<StrokeSetting>().BrushE, BrushE)
            .SetPolylineGeometry([..Generator.Positions], [..Generator.Radii], [..Generator.Pressures], [..Generator.Tilts])
            .Commit();
        Clear();
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}