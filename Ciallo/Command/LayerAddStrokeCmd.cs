using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class LayerAddStrokeCmd : CommandBase
{
    private readonly Entity _layerE;
    private int _index;

    public LayerAddStrokeCmd(Entity layerE, int index = -1)
    {
        _layerE = layerE;
        _index = index;
    }

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity strokeE)
    {
        if (_index < 0)
            _index = _layerE.Get<LayerTreeNode>().Children.Count + _index + 1;
    }

    public override void Do(Entity strokeE)
    {
        // Data
        _layerE.Get<LayerTreeNode>().InsertChild(_index, strokeE);

        // View
        var strokeView = strokeE.Get<StrokeView>();
        var layerView = _layerE.Get<PolylineLayerView>();
        layerView.InsertNodeAt(strokeView, _index);
        strokeView.SetOwner(layerView.Owner);

        // Overlay
        Document.Get<WorldOverlay>().AddChild(strokeE.Get<PolylineWireframe>());

        // Body
        _layerE.Get<PolylineBodyHolder>().InsertNodeAt(strokeE.Get<Body>(), _index);
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
        _layerE.Get<LayerTreeNode>().RemoveChild(strokeE);
    }
}