using System.Collections.Generic;
using System.Runtime.Serialization;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushManager
{
    private List<Entity> _brushes = [];
}