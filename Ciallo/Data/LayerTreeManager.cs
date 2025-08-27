using MessagePack;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;

namespace Ciallo.Data;

/// <summary>
/// Manages the layer tree in the document. Considered as root of the tree.
/// </summary>
[MessagePackObject, ToSerialize]
public class LayerTreeManager
{
    [IgnoreMember] public World World;
    [Key(0)] public readonly LayerTreeNode Node;

    public Entity CreateAddVectorLayer()
    {
        var e = this.World.Create();
        var node = new LayerTreeNode()
        {
            Name = { Value = "New Vector Layer" },
        };
        
        e.Add(new VectorLayerSetting(), node, new ToSerializeTag());
        Node.AddChild(e);
        
        return e;
    }
}