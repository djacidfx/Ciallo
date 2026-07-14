using Ciallo.Data;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree]
public partial class DialogHost : Control
{
    public override void _EnterTree() => AppDialogHost = this;
}
