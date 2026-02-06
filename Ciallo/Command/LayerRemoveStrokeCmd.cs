using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class LayerRemoveStrokeCmd : CommandBase
{
    private LayerAddStrokeCmd _addCmd;

    public override void BeforeFirstDo(Entity strokeE)
    {
        var layerE = strokeE.Get<LayerTreeNode>().Parent;
        var index = layerE.Get<LayerTreeNode>().Children.IndexOf(strokeE);

        _addCmd = new(layerE, index) { TargetE = strokeE };
        _addCmd.BeforeFirstDo(strokeE);
    }

    public override void Do(Entity strokeE)
    {
        _addCmd.Undo(strokeE);
    }

    public override void Undo(Entity strokeE)
    {
        _addCmd.Do(strokeE);
    }
}