using System.Collections.Generic;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Command;

public partial class MoveLayerCmd : CommandBase
{
    private readonly IReadOnlyList<int> _src;
    private readonly IReadOnlyList<int> _dst;

    public MoveLayerCmd(IReadOnlyList<int> src, IReadOnlyList<int> dst)
    {
        _src = src;
        _dst = dst;
    }

    public override void Do()
    {
        // Layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.MoveDescendant(_src, _dst);
        // layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.Move(_src, _dst);
    }

    public override void Undo()
    {
        // layer tree view
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.Move(_dst, _src);
        // layer tree data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.MoveDescendant(_dst, _src);
    }
}