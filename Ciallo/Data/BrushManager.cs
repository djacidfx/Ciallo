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
    public ObservableList<Entity> Brushes = [];

    public int Add(Entity brush)
    {
        Brushes.Add(brush);
        return Brushes.Count - 1;
    }

    public void Remove(Entity brush)
    {
        Brushes.Remove(brush);
    }
}