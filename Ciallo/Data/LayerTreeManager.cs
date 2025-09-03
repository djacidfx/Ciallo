using MessagePack;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;

namespace Ciallo.Data;

/// <summary>
/// Manages the layer tree in the document. Considered as root of the tree.
/// </summary>
[MessagePackObject(true), ToSerialize]
public class LayerTreeManager
{
    public readonly LayerTreeNode Root = new()
    {
        Name = { Value = "Root" },
    };
}