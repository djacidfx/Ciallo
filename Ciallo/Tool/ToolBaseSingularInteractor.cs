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
    
    public bool IsLeftInteracting { get; protected set; }

    public void OnLeftClick(CursorButtonData data)
    {
        if(!LeftInteractor.CanInteract) return;
        IsLeftInteracting = true;
        LeftInteractor.Start(data);
    }

    public void OnMoving(CursorMotionData data)
    {
        if(IsLeftInteracting) LeftInteractor.Interacting(data);
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