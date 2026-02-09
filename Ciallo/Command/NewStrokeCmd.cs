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

        // View
        var strokeView = new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        };
        strokeE.AddNode(strokeView);

        // Overlay
        var strokeOverlay = new PolylineWireframe() { Visible = false };
        strokeE.AddNode(strokeOverlay);

        // Body
        var strokeBody = new Body();
        strokeE.AddNode(strokeBody);
    }

    public override void Do(Entity strokeE)
    {
        // Data
        strokeE.Tag<ToSerializeTag>();
    }

    public override void Undo(Entity strokeE)
    {
        strokeE.Detach<ToSerializeTag>();
    }
}