using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Command;

// ReSharper disable once Godot.MissingParameterlessConstructor
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
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.MoveDescendant(_src, _dst);

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

        // layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.MoveDescendant(_dst, _src);
    }
}