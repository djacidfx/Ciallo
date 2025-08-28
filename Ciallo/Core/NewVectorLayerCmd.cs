using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Core;

public partial class NewVectorLayerCmd(List<int> insertPath = null) : CommandBase
{
    private List<int> _insertPath = insertPath;

    public override void Do()
    {
        var m = Document.Get<LayerTreeManager>();

        if (DestructionQueue.Count == 0)
        {
            var e = WorkingWorld.Create();
            var node = new LayerTreeNode()
            {
                Name = { Value = $"Layer {m.Root.ChildCount+1}" },
            };
            e.Add(new VectorLayerSetting(), node, new ToSerializeTag());
            DestructionQueue.Add(e);
        }
        var layer = DestructionQueue[0];
        _insertPath ??= [m.Root.ChildCount];
        m.Root.InsertDescendant(_insertPath, layer);
    }

    public override void Undo()
    {
        var m = Document.Get<LayerTreeManager>();
        m.Root.RemoveDescendant(_insertPath);
    }
}