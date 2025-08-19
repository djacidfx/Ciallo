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
    [Key(0)] public LayerTreeBranch Branch = [];

    public Entity CreateAddVectorLayer()
    {
        var e = this.World.Create();
        e.Add(new VectorLayerSetting(), new LayerTreeBranch(), new ToSerializeTag());
        Branch.Add(e);
        return e;
    }
}