using System.Linq;
using Ciallo.Command;
using Frent;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// Base class for easy tool implementation.
/// Classes derive from this no need to configure state machine manually.
/// by setting the interactive sessions of hover and left and right mouse buttons drag.
/// </summary>
public abstract class EasyToolBase : ToolBase
{
    protected EasyToolBase(IInteractiveSession hover, IInteractiveSession left, IInteractiveSession right)
    {
        ConfigureInitial(hover)
            .Permit(Press(MouseButton.Left), left)
            .Permit(Press(MouseButton.Right), right);

        Configure(left)
            .Permit(Release(MouseButton.Left), hover)
            .Permit(Press(AppActions.CancelInteraction), hover)
            .Permit(Press(AppActions.ConfirmInteraction), hover);

        Configure(right)
            .Permit(Release(MouseButton.Right), hover)
            .Permit(Press(AppActions.CancelInteraction), hover)
            .Permit(Press(AppActions.ConfirmInteraction), hover);
    }

    public abstract bool CanHandleLayer(Entity layerE);

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        return layerEs.Length == 1 && CanHandleLayer(layerEs.Single());
    }
}