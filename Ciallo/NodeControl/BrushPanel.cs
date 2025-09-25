using System.Collections.Specialized;
using System.Linq;
using Arch.Core;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using Godot;
using ObservableCollections;
using R3;
using Arch.Core;
using Arch.Core.Extensions;

namespace Ciallo.NodeControl;

public partial class BrushPanel : AcceptDialog
{
    public readonly ReactiveProperty<int> SelectedIndex = new(-1);
    
    public ItemList BrushSelector;
    public Container PropertiesHolder;
    public Button Add;
    public Button Remove;
    public Button Copy;
    public Button Reset;
    public Button Top;
    public Button Up;
    public Button Down;
    public Button Bottom;
    public Container Operators;
    
    [OnInstantiate]
    private void Initialise()
    {
    }

    public override void _EnterTree()
    {
        GetOkButton().Visible = false;
        BrushSelector = GetNode<ItemList>("%BrushSelector");
        PropertiesHolder = GetNode<Container>("%PropertiesHolder");
        Add = GetNode<Button>("%Add");
        Remove = GetNode<Button>("%Remove");
        Copy = GetNode<Button>("%Copy");
        Reset = GetNode<Button>("%Reset");
        Top = GetNode<Button>("%Top");
        Up = GetNode<Button>("%Up");
        Down = GetNode<Button>("%Down");
        Bottom = GetNode<Button>("%Bottom");
        Operators = GetNode<Container>("%Operators");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Propagate unhandled key event to main viewport to enable shortcuts
        // Only work for godot `Window` without exclusive
        if (@event is InputEventKey)
        {
            GetParent().GetViewport().PushInput(@event);
        }
    }

    public void BindBrushManager(BrushManager manager)
    {
        var brushes = manager.Brushes;
        BrushSelector.BindObservableList(brushes, e=>e.Get<BrushSetting>().Name);
        BrushSelector.BindSelectionIndex(SelectedIndex);
        
        foreach (var brushE in brushes)
        {
            var propertyBox = new PropertyContainer();
            brushE.Get<BrushSetting>().DrawProperty(propertyBox);
            propertyBox.VisibleIf(SelectedIndex, idx=>brushes.ElementAtOrDefault(idx) == brushE);
            PropertiesHolder.AddChild(propertyBox);
        }
        
        brushes.ObserveChanged().Subscribe(e =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var brushE = e.NewItem;
                    var propertyBox = new PropertyContainer();
                    brushE.Get<BrushSetting>().DrawProperty(propertyBox);
                    propertyBox.VisibleIf(SelectedIndex, idx=>brushes.ElementAtOrDefault(idx) == brushE);
                    PropertiesHolder.AddChild(propertyBox);
                    PropertiesHolder.MoveChild(propertyBox, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    PropertiesHolder.GetChild(e.OldStartingIndex).QueueFree();
                    break;
                case NotifyCollectionChangedAction.Move:
                    PropertiesHolder.MoveNode([e.OldStartingIndex], [e.NewStartingIndex]);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    PropertiesHolder.QueueFreeChildren();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new("Replace action is not supported in brush collection");
            }
        }).AddTo(this);
    }
}
