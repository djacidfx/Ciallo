using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;

namespace Ciallo.Core;

public class NewVectorLayerCommand(World workingWorld, List<int> insertPath = null)
{
    public void Do()
    {
        var m = workingWorld.Singleton().Get<LayerTreeManager>();
        var e = workingWorld.Create();
        var node = new LayerTreeNode()
        {
            Name = { Value = $"Vector Layer {m.Root.ChildCount+1}" },
        };
        e.Add(new VectorLayerSetting(), node, new ToSerializeTag());
        
        m.Root.InsertDescendant(insertPath, e);
    }

    public void Redo()
    {
        
    }

    public void Undo()
    {
        
    }
}