using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewStrokeCmd : CommandBase
{
    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity strokeE)
    {
        strokeE.Add(new LayerTreeNode());
        strokeE.Add(new StrokeSetting());
        strokeE.Add(new PolylineGeometry());
    }

    public override void Do(Entity strokeE)
    {
        // Data
        strokeE.Tag<ToSerializeTag>();

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        };
        strokeE.Add(strokeView);

        // Overlay
        var strokeOverlay = new PolylineWireframe() { Visible = false };
        strokeE.Add(strokeOverlay);

        // Body
        var strokeBody = new Body();
        strokeE.Add(strokeBody);
    }

    public override void Undo(Entity strokeE)
    {
        // Body
        strokeE.Get<Body>().QueueFree();
        strokeE.Remove<Body>();

        // Overlay
        strokeE.Get<PolylineWireframe>().QueueFree();
        strokeE.Remove<PolylineWireframe>();

        // View
        strokeE.Get<StrokeView>().QueueFree();
        strokeE.Remove<StrokeView>();

        // Data
        strokeE.Detach<ToSerializeTag>();
    }
}