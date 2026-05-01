using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

// Note: not implementing hierarchy on layer panel, only flat list move.
[CommandBuilder]
public class MoveLayerCmd : CommandBase
{
    // Raw path inputs — only set by path-based constructors (IsDefault when unused)
    private ImmutableArray<int> _srcPath;
    private ImmutableArray<int> _dstPath;

    // Entity-based state (resolved in BeforeFirstDo)
    private Entity _srcE;
    private Entity _dstParentE;
    private int _dstIdx;

    // Undo state (captured in BeforeFirstDo)
    private Entity _origParentE;
    private int _origIdx;

    public MoveLayerCmd(IReadOnlyList<int> src, IReadOnlyList<int> dst)
    {
        _srcPath = [..src];
        _dstPath = [..dst];
    }

    public MoveLayerCmd(Entity srcE, IReadOnlyList<int> dst)
    {
        _srcE = srcE;
        _dstPath = [..dst];
    }

    public MoveLayerCmd(Entity srcE, Entity dstParent, int dstIndex)
    {
        _srcE = srcE;
        _dstParentE = dstParent;
        _dstIdx = dstIndex;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        var root = Document.Get<LayerTreeNode>();

        // Resolve source entity from path if needed — O(depth)
        if (_srcE.IsNull)
            _srcE = root.GetDescendant(_srcPath);

        // Resolve destination parent entity + index from path if needed — O(depth)
        if (!_dstPath.IsDefault)
        {
            _dstIdx = _dstPath[^1];
            _dstParentE = _dstPath.Length > 1
                ? root.GetDescendant(_dstPath.RemoveAt(_dstPath.Length - 1))
                : root.Self;
        }

        // Capture undo state — O(siblings)
        _origParentE = _srcE.Get<LayerTreeNode>().ParentValue;
        _origIdx = _srcE.Get<LayerTreeNode>().Index;
    }

    public override void Do(Entity layerE)
    {
        Document.Get<LayerTreeNode>().MoveEntity(_srcE, _dstParentE, _dstIdx);
    }

    public override void Undo(Entity layerE)
    {
        Document.Get<LayerTreeNode>().MoveEntity(_srcE, _origParentE, _origIdx);
    }
}