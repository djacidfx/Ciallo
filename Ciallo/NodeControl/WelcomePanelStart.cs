using Ciallo.NodeControl;
using Godot;

public partial class WelcomePanelStart : VBoxContainer
{
    public void OnNewDocumentPressed() => AppGuiCommand.PopupNewDocumentDialog();
    public void OnOpenDocumentPressed() => AppGuiCommand.PopupOpenDocumentDialog();
}