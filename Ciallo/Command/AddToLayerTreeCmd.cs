using Ciallo.Data;
using Ciallo.Rendering;
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

    public override void Do(Entity strokeE)
    {
        // Data
        _parentE.Get<LayerTreeNode>().InsertChild(_index, strokeE);

        // View
        var strokeView = strokeE.Get<StrokeView>();
        var layerView = _parentE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(strokeView, _index);
        strokeView.SetOwner(layerView.Owner);

        // Overlay
        Document.Get<WorldOverlay>().AddChild(strokeE.Get<PolylineWireframe>());

        // Body
        _parentE.Get<PolylineBodyHolder>().InsertNodeAt(strokeE.Get<Body>(), _index);
    }

    public override void Undo(Entity strokeE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(strokeE);

        // Body
        strokeE.Get<Body>().RemoveFromParent();

        // Overlay
        strokeE.Get<PolylineWireframe>().RemoveFromParent();

        // View
        strokeE.Get<StrokeView>().RemoveFromParent();

        // Data
        _parentE.Get<LayerTreeNode>().RemoveChild(strokeE);
    }
}