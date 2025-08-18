using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Widget;
using R3;

public partial class LayerTreeControl : Container
{
    [Export] public PackedScene LayerControlScene;
    [Export] public ButtonGroup IsActiveLayerButtonGroup;
    
    public VBoxContainer Root;
    
    private bool _isDragging = false;
    private Control _visibleDragHint;
    private Control _mouseHoveringLayer;
    
    private Dictionary<Control, IDisposable> _subscriptions = [];

    public override void _Ready()
    {
        Root = GetChild<VBoxContainer>(0);
        if(LayerControlScene != null && IsActiveLayerButtonGroup != null)
        {
            foreach (int i in Enumerable.Range(0, 10))
            {
                var node = CreateLayerControl();
                Root.AddChild(node);
            }
        }
    }

    public Control CreateLayerControl()
    {
        var layerControl = LayerControlScene.Instantiate<Control>();
        var isActiveButton = layerControl.GetNode<CheckBox>("%IsActive");
        isActiveButton.ButtonGroup = IsActiveLayerButtonGroup;
        if (IsActiveLayerButtonGroup.GetPressedButton() == null)
        {
            isActiveButton.SetPressed(true);
        }

        var lineEdit = layerControl.GetNode<LabelLineEdit>("%LabelLineEdit");
        
        lineEdit.MouseEntered += () =>
        {
            _mouseHoveringLayer = layerControl;
        };

        var guiInput = lineEdit
            .SignalAsObservable<InputEvent>(Control.SignalName.GuiInput)
            .Where(_=>!lineEdit.IsEditing());
        var leftMouse = guiInput
            .OfType<InputEvent, InputEventMouseButton>()
            .Where(button => button.ButtonIndex == MouseButton.Left);

        // Single click
        var singleClickObs = leftMouse
            .Where(button => button.IsPressed() || button.IsReleased())
            .Chunk(TimeSpan.FromMilliseconds(200))
            .Where(xs => xs.Length == 2 && xs.First().IsPressed() && xs.Last().IsReleased())
            .Select(xs => xs.First());
        var subscription1 = singleClickObs.Subscribe(_ => isActiveButton.SetPressed(true));
        
        // Drag
        var mouseState = leftMouse.ToReadOnlyReactiveProperty();
        var dragStart = guiInput
            // The most recent left mouse is clicked but not release.
            .Where(_ => mouseState.CurrentValue?.IsPressed() == true)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left)
            // mouse motion distance is larger than the value in pixels.
            .Where(motion => motion.GlobalPosition.DistanceTo(mouseState.CurrentValue.GlobalPosition) > 20)
            .Where(_ => !_isDragging);
        var subscription2 = dragStart.Subscribe(motion =>
        {
            _isDragging = true;
            OnDragStart(layerControl, motion);
        });

        var dragging = guiInput
            .Where(_ => _isDragging)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left);
        var subscription3 = dragging.Subscribe(motion => OnDragging(layerControl, motion));

        var dragEnd = leftMouse
            .Where(button => _isDragging && button.IsReleased());
        var subscription4 = dragEnd.Subscribe(button =>
        {
            _isDragging = false;
            OnDragEnd(layerControl, button);
        });
        
        _subscriptions[layerControl] = Disposable.Combine(subscription1, subscription2, subscription3, subscription4);
        return layerControl;
    }

    private void OnDragStart(Control layerControl, InputEventMouseMotion motion)
    {
        
    }

    public void OnDragging(Control layerControl, InputEventMouseMotion e)
    {
        if (_mouseHoveringLayer == null) return;
        var locPos = _mouseHoveringLayer.GetLocalMousePosition();
        var size = _mouseHoveringLayer.Size;
            
        var sep = size.Y / 2; // separation on whether the drop target is above or below the hovering layer.
        var hintToShow = _mouseHoveringLayer.GetNode<HSeparator>(locPos.Y < sep ? "%AboveHint" : "%BelowHint");
        if (_visibleDragHint == hintToShow) return;
        if (_visibleDragHint != null)
            _visibleDragHint.Visible = false;
        hintToShow.Visible = true;
        _visibleDragHint = hintToShow;
    }

    private void OnDragEnd(Control layerControl, InputEventMouseButton button)
    {
        
        // Move layer
        
        
        // Drag hint
        if(_visibleDragHint != null) _visibleDragHint.Visible = false;
        _visibleDragHint = null;
    }
}
