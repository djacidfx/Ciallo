using Ciallo.Command;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// The base class for tools that use a single interactor for left-click interactions.
/// </summary>
public abstract partial class ToolBaseSingularInteractor : ToolButtonBase, ITool
{
    public abstract InteractorBase LeftInteractor { get; }
    
    private bool _isInteracting = false;

    public void OnLeftClick(CursorButtonData data)
    {
        if(!LeftInteractor.CanInteract) return;
        _isInteracting = true;
        LeftInteractor.Start(data);
    }

    public void OnMoving(CursorMotionData data)
    {
        if(_isInteracting) LeftInteractor.Interacting(data);
    }

    public void OnLeftRelease(CursorButtonData data)
    {
        if(_isInteracting) LeftInteractor.End(data);
        _isInteracting = false;
    }

    public void OnRightClick(CursorButtonData data)
    {
    }

    public void OnRightRelease(CursorButtonData data)
    {
    }

    public void OnKey(InputEventKey key)
    {
        if(AppActions.CancelInteraction.IsJustPressed)
        {
            if(_isInteracting) LeftInteractor.Cancel();
            _isInteracting = false;
        }
    }
}