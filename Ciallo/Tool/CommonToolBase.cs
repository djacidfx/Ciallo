using Ciallo.Command;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// The base class for common tools that need for three states: Hovering, left mouse drag, right mouse drag.
/// For more complex tools, we can write state machine code and implement ITool directly.
/// </summary>
public abstract partial class CommonToolBase : ToolButtonBase, ITool
{
    public virtual InteractorBase LeftInteractor => null;
    public virtual InteractorBase HoveringInteractor => null;
    public virtual InteractorBase RightInteractor => null;
    
    public bool IsLeftInteracting { get; protected set; }
    public bool IsHovering { get; protected set; }

    public void OnLeftClick(CursorButtonData data)
    {
        if(!LeftInteractor.CanInteract) return;
        IsLeftInteracting = true;
        LeftInteractor.Start(data);
    }

    public void OnMoving(CursorMotionData data)
    {
        if(IsLeftInteracting) LeftInteractor.Interacting(data);
        if (!IsLeftInteracting && !IsHovering) IsHovering = true;
        if(IsHovering && HoveringInteractor?.CanInteract == true) HoveringInteractor?.Interacting(data);
    }

    public void OnLeftRelease(CursorButtonData data)
    {
        if(IsLeftInteracting) LeftInteractor.End(data);
        IsLeftInteracting = false;
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
            if(IsLeftInteracting) LeftInteractor.Cancel();
            IsLeftInteracting = false;
        }
    }
}