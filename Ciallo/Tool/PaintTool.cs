using Godot;
using System;
using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.NodeControl;
using Ciallo.Tool;

public partial class PaintTool : Button, ITool
{
    public readonly PaintInteractor PaintInteractor = new();
    public readonly ResizeBrushInteractor BrushInteractor = new();
    
    private bool _isPainting = false;
    private bool _isResizingBrush = false;
    
    public bool IsInteracting => _isPainting || _isResizingBrush;

    public override void _Ready()
    {
        ButtonGroup = ToolManager.ToolButtonGroup;
        this.SetPressed(true);
    }

    public void OnLeftClick(CursorButtonData data)
    {
        if (!PaintInteractor.CanInteract) return;
        _isPainting = true;
        PaintInteractor.Start(data);
    }

    public void OnMoving(CursorMotionData data)
    {
        if(_isPainting) PaintInteractor.Interacting(data);
        if(_isResizingBrush) BrushInteractor.Interacting(data);
    }

    public void OnLeftRelease(CursorButtonData data)
    {
        if (_isPainting) PaintInteractor.End(data);
        _isPainting = false;
    }

    public void OnAction(InputEventAction action)
    {
        if (AppActions.CancelInteraction.IsJustPressed) CancelInteraction();
    }

    public void OnRightClick(CursorButtonData data)
    {
        
    }

    public void OnRightRelease(CursorButtonData data)
    {
        
    }

    public void CancelInteraction()
    {
        if(_isPainting) PaintInteractor.Cancel();
        if(_isResizingBrush) BrushInteractor.Cancel();
        _isPainting = false;
        _isResizingBrush = false;
    }
}
