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
        strokeE.AddNode(new StrokeView()
        {
            Material = AutoloadRendering.MissingBrushMaterial,
        });

        // Overlay
        strokeE.AddNode(new PolylineWireframe() { Visible = false });

        // Body
        strokeE.AddNode(new Body());
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