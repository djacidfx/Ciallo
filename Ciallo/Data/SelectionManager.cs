using System.Runtime.Serialization;
using Frent;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class SelectionManager
{
    [DataMember] public ObservableList<Entity> SelectedLayers = [];

    [DataMember] public ReactiveProperty<Entity> WorkingLayer = new(Entity.Null);

    [DataMember] public ReactiveProperty<Entity> WorkingBrush = new(Entity.Null);

    public ObservableList<Entity> SelectedShapes = [];
}