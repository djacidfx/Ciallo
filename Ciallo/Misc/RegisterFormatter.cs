using Godot;
using MemoryPack;
using ObservableCollections;
using R3;

namespace Ciallo;

public partial class RegisterFormatter : Node
{
    public override void _EnterTree()
    {
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>));
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ObservableList<>), typeof(ObservableListFormatter<>));
    }
}