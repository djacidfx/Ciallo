using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Command;

// Note: not implementing hierarchy on layer panel, only flat list move.
[CommandBuilder]
public class MoveLayerCmd : CommandBase
{
    private ImmutableArray<int> _src;
    private readonly ImmutableArray<int> _dst;

    public MoveLayerCmd(IReadOnlyList<int> src, IReadOnlyList<int> dst)
    {
        _src = [..src];
        _dst = [..dst];
    }

    public MoveLayerCmd(IReadOnlyList<int> dst)
    {
        _dst = [..dst];
    }

    public override void BeforeFirstDo(Entity layerE)
    {
        if (_src.Length == 0)
        {
            var root = Document.Get<LayerTreeNode>();
            _src = root.FindPathTo(layerE);
        }
    }

    public override void Do(Entity layerE)
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

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.MoveNode(_src, _dst);
    }

    public override void Undo(Entity layerE)
    {
        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.MoveNode(_dst, _src);

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