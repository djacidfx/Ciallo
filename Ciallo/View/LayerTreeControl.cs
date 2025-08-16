using Godot;
using System;
using System.Linq;
using Ciallo.Widget;
using R3;

public partial class LayerTreeControl : Container
{
    [Export] public PackedScene LayerControlScene;
    [Export] public ButtonGroup IsActiveLayerButtonGroup;
    
    public VBoxContainer Root;
    
    private bool _isDragging = false;

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

    public Node CreateLayerControl()
    {
        var layerRoot = LayerControlScene.Instantiate();
        var isActiveButton = layerRoot.GetNode<CheckBox>("IsActive");
        isActiveButton.ButtonGroup = IsActiveLayerButtonGroup;
        if (IsActiveLayerButtonGroup.GetPressedButton() == null)
        {
            isActiveButton.SetPressed(true);
        }
        
        var lineEdit = layerRoot.GetNode<LabelLineEdit>("LabelLineEdit");
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
        var connection1 = singleClickObs.Subscribe(_ => isActiveButton.SetPressed(true));
        
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
        var connection2 = dragStart.Subscribe(motion =>
        {
            _isDragging = true;
            OnDragStart(layerRoot, motion);
        });

        var dragging = guiInput
            .Where(_ => _isDragging)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left);
        var connection3 = dragging.Subscribe(motion => OnDragging(layerRoot, motion));

        var dragEnd = leftMouse
            .Where(button => _isDragging && button.IsReleased());
        var connection4 = dragEnd.Subscribe(button =>
        {
            _isDragging = false;
            OnDragEnd(layerRoot, button);
        });
        
        var connections = Disposable.Combine(connection1, connection2, connection3, connection4);
        layerRoot.TreeExiting += connections.Dispose;
        
        return layerRoot;
    }

    private void OnDragStart(Node layerRoot, InputEventMouseMotion motion)
    {
        
    }

    public void OnDragging(Node layerRoot, InputEventMouseMotion e)
    {
        GD.Print("Dragging layer");
    }

    private void OnDragEnd(Node layerRoot, InputEventMouseButton button)
    {
        GD.Print("Drag end" + button.GlobalPosition);
    }

    private void OnSingleClicked(InputEventMouseButton e)
    {
        GD.Print("Single Clicked on Layer Control");
    }
}
