using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using Godot;
using ObservableCollections;
using R3;

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
    public Viewport BrushPreviewViewport;
    public SubViewportContainer BrushPreviewContainer;
    public float PreviewBaseWidth = Single.Pi * (2f + 0.3f); // 2pi + padding blank
    public const float PreviewAspectRatio = 1.618f * 2;

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
        BrushPreviewViewport = GetNode<SubViewport>("%BrushPreviewViewport");
        BrushPreviewContainer = BrushPreviewViewport.GetParent<SubViewportContainer>();
    }

    public override void _Ready()
    {
        var background = (GradientTexture2D)BrushPreviewViewport.GetChild<Sprite2D>(1).Texture;
        background.Width = (int)Mathf.Ceil(PreviewBaseWidth);
        background.Height = (int)Mathf.Ceil(PreviewBaseWidth / PreviewAspectRatio);
        
        BrushPreviewContainer.Resized += () =>
        {
            Vector2 size = BrushPreviewContainer.Size;
            if ((MathF.Abs(size.X / size.Y - PreviewAspectRatio) <= 1e-5f)) return;

            size.Y = size.X / PreviewAspectRatio;
            BrushPreviewContainer.CustomMinimumSize = new(0, size.Y);
        };

        BrushPreviewViewport.SizeChanged += () =>
        {
            var size = BrushPreviewContainer.Size;
            var zoomLevel = size.X / PreviewBaseWidth;
            BrushPreviewViewport.GetChild<Camera2D>(0).Zoom = Vector2.One * zoomLevel;
        };
        
        BrushPreviewContainer.ResetSize();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Propagate unhandled key event to main viewport to enable shortcuts
        // Only work for godot `Window` with `exclusive` false
        if (@event is InputEventKey)
        {
            GetParent().GetViewport().PushInput(@event);
        }
    }

    public void BindBrushSetting<T>(ObservableList<T> list, Func<T, BrushSetting> toBrushSetting)
    {
        BrushSelector.ObserveObservableList(list, e => toBrushSetting(e).Name);
        BrushSelector.BindSelectionIndex(SelectedIndex);
        
        foreach (var item in list)
        {
            var propertyBox = new PropertyContainer();
            toBrushSetting(item).DrawProperty(propertyBox);
            propertyBox.VisibleIf(SelectedIndex, idx => EqualityComparer<T>.Default.Equals(list.ElementAtOrDefault(idx), item));
            PropertiesHolder.AddChild(propertyBox);
        }
        
        list.ObserveChanged().Subscribe(e =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var propertyBox = new PropertyContainer();
                    toBrushSetting(e.NewItem).DrawProperty(propertyBox);
                    propertyBox.VisibleIf(SelectedIndex, idx=>EqualityComparer<T>.Default.Equals(list.ElementAtOrDefault(idx), e.NewItem));
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
                    throw new("Replace action is not supported yet");
            }
        }).AddTo(this);
    }
}
