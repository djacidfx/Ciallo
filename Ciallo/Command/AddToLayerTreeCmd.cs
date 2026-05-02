using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class AddToLayerTreeCmd : CommandBase
{
    private readonly Entity _parentE;
    private int _index;

    public AddToLayerTreeCmd(Entity parentE, int index = -1)
    {
        _parentE = parentE;
        _index = index;
    }

    public override void BeforeFirstDo(Entity strokeE)
    {
        if (_index < 0)
            _index = _parentE.Get<LayerTreeNode>().Children.Count + _index + 1;
    }

    public override void Do(Entity targetE)
    {
        _parentE.Get<LayerTreeNode>().InsertChild(_index, targetE);
    }

    public override void Undo(Entity targetE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedShapes.Remove(targetE);

        _parentE.Get<LayerTreeNode>().RemoveChild(_index);
    }
}
