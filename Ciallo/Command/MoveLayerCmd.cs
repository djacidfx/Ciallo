using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

// Note: not implementing hierarchy on layer panel, only flat list move.
[CommandBuilder]
public class MoveLayerCmd : CommandBase
{
    private readonly ImmutableArray<int> _src;
    private readonly ImmutableArray<int> _dst;

    /// <summary>
    /// TODO: Support hierarchy.
    /// Target entity as root, move the descendant at path src to path dst.
    /// </summary>
    public MoveLayerCmd(IReadOnlyList<int> src, IReadOnlyList<int> dst)
    {
        _src = [..src];
        _dst = [..dst];
    }

    public override void BeforeFirstDo(Entity targetE) { }

    public override void Do(Entity layerE)
    {
        // Data
        var root = Document.Get<LayerTreeNode>();
        root.MoveDescendant(_src, _dst);
    }

    public override void Undo(Entity layerE)
    {
        // Data
        var root = Document.Get<LayerTreeNode>();
        root.MoveDescendant(_dst, _src);
    }
}