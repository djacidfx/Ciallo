using Godot;

namespace Ciallo.GuiControl;

public partial class WelcomePanelStart : VBoxContainer
{
    public void OnNewDocumentPressed() => AppGuiCommand.PopupNewDocumentDialog();
    public void OnOpenDocumentPressed() => AppGuiCommand.PopupOpenDocumentDialog();
}