using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Command;

// Note: not implementing hierarchy on layer panel, only flat list move.
public class MoveLayerCmd : CommandBase
{
    private readonly ImmutableArray<int> _src;
    private readonly ImmutableArray<int> _dst;

    public MoveLayerCmd(IReadOnlyList<int> src, IReadOnlyList<int> dst)
    {
        _src = [..src];
        _dst = [..dst];
    }

    public override void Do()
    {
        // Data
        var root = Document.Get<LayerTreeNode>();
        root.MoveDescendant(_src, _dst);

        // layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.Move(_src, _dst);

        // View
        var worldView = Document.Get<WorldView>();
        worldView.MoveNode(_src, _dst);

        // Overlay is order-free
    }

    public override void Undo()
    {
        // View
        var worldView = Document.Get<WorldView>();
        worldView.MoveNode(_dst, _src);

        // layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.Move(_dst, _src);

        // Data
        var root = Document.Get<LayerTreeNode>();
        root.MoveDescendant(_dst, _src);
    }
}