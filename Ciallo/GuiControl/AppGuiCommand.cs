using System.Linq;
using Godot;

namespace Ciallo.GuiControl;

public static class AppGuiCommand
{
    public static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    public static void PopupNewDocumentDialog()
    {
        var dialogNew = Tree.GetNodesInGroup("Dialog").OfType<NewDocumentDialog>().Single();
        dialogNew.Popup();
    }

    public static void PopupOpenDocumentDialog()
    {
        var dialogOpen = Tree.GetNodesInGroup("Dialog").OfType<OpenDocumentDialog>().Single();
        dialogOpen.Popup();
    }
}