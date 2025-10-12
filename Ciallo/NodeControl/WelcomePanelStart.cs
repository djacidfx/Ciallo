using Godot;
using Ciallo.NodeControl;

public partial class WelcomePanelStart : VBoxContainer
{
    public void OnNewDocumentPressed() => AppGuiCommand.PopupNewDocumentDialog();
    public void OnOpenDocumentPressed() => AppGuiCommand.PopupOpenDocumentDialog();
}
