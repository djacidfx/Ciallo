using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Godot;

namespace Ciallo.Tool;

public class PaintStrokeOnVectorFill : PaintStrokeInteractor
{
    public override void End(CursorButtonData data)
    {
        Generator.End(data);
        var layers = WorkingLayer.Get<VectorFillLayerSetting>().ReferenceLayers;
        if (layers.Count > 0)
        {
            var targetShapeLayer = layers.First();
            var geometry = Generator.CurrentGeometry;
            new CommandBuilder(WorkingLayer.World.Create())
                .NewStroke()
                .AddToLayerTree(targetShapeLayer)
                .SetProperty(e => e.Get<StrokeSetting>().BrushE, BrushE)
                .SetPolylineGeometry([..geometry.Positions], [..geometry.Radii], [..geometry.Pressures], [..geometry.Tilts])
                .Commit();
        }
        Clear();
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}
