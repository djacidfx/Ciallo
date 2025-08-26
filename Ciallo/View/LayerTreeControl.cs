using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Misc;
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
    
    private readonly Dictionary<Control, CompositeDisposable> _subscriptions = [];

    public override void _Ready()
    {
        Root = GetNode<VBoxContainer>("%TreeRoot");
    }

    public Control CreateLayerControl(VectorLayerSetting setting)
    {
        var layerControl = LayerControlScene.Instantiate<Control>();
        var subs = new CompositeDisposable();
        _subscriptions[layerControl] = subs;
        
        var activeButton = layerControl.GetNode<CheckBox>("%Active");
        activeButton.ButtonGroup = IsActiveLayerButtonGroup;
        if (IsActiveLayerButtonGroup.GetPressedButton() == null) activeButton.SetPressed(true);
        var visibleButton = layerControl.GetNode<CheckBox>("%Visible");
        visibleButton.BindValue(setting.IsVisible).AddTo(subs);

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
        
        // Single click without drag and double click
        var singleClickObs = leftMouse
            .Where(button => button.IsPressed() || button.IsReleased())
            .Chunk(TimeSpan.FromMilliseconds(200))
            .Where(xs => xs.Length == 2 && xs.First().IsPressed() && xs.Last().IsReleased())
            .Select(xs => xs.First());
        singleClickObs.Subscribe(_ => activeButton.SetPressed(true)).AddTo(subs);
        
        // Drag
        var mouseState = leftMouse.ToReadOnlyReactiveProperty();
        var dragStart = guiInput
            // The most recent left mouse is clicked and not release.
            .Where(_ => mouseState.CurrentValue?.IsPressed() == true)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left)
            // mouse motion distance is larger than the value in pixels.
            .Where(motion => motion.GlobalPosition.DistanceTo(mouseState.CurrentValue.GlobalPosition) > 20)
            .Where(_ => !_isDragging);
        dragStart.Subscribe(motion =>
        {
            _isDragging = true;
            OnDragStart(layerControl, motion);
        }).AddTo(subs);

        var dragging = guiInput
            .Where(_ => _isDragging)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left);
        dragging.Subscribe(motion => OnDragging(layerControl, motion)).AddTo(subs);

        var dragEnd = leftMouse
            .Where(button => _isDragging && button.IsReleased());
        dragEnd.Subscribe(button =>
        {
            _isDragging = false;
            OnDragEnd(layerControl, button);
        }).AddTo(subs);
        
        return layerControl;
    }
    
    public void RemoveLayerControl(Control layerControl)
    {
        if (!_subscriptions.TryGetValue(layerControl, out var subscription))
            throw new ArgumentException("The given layer control is not managed by this LayerTreeControl.");
        subscription.Dispose();
        _subscriptions.Remove(layerControl);
        layerControl.QueueFree();
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
        if (_visibleDragHint != null) _visibleDragHint.Visible = false;
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
