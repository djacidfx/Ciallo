using System.Collections.Generic;
using Arch.Core;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

public class SelectionManager
{
    public readonly ObservableList<Entity> SelectedLayers = [];
    public readonly ReactiveProperty<Entity> WorkingLayer = new(Entity.Null);
}