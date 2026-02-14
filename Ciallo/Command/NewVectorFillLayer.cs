using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillLayer : CommandBase
{
    public override void BeforeFirstDo(Entity targetE)
    {
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);
    }

    public override void Do(Entity targetE) { }

    public override void Undo(Entity targetE) { }
}