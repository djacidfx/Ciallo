using System.Collections.Generic;
using Arch.Core;
using R3;

namespace Ciallo.Data;

public class SelectionManager
{
    public readonly ReactiveProperty<List<Entity>> SelectedLayers = new([]);
    public readonly ReactiveProperty<Entity> ActiveLayer = new(Entity.Null);
}