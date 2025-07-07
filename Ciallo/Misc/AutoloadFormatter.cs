using Godot;
using MemoryPack;
using ObservableCollections;
using R3;

namespace Ciallo;

public partial class AutoloadFormatter : Node
{
    public override void _Ready()
    {
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>));
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ObservableList<>), typeof(ObservableListFormatter<>));
    }
}