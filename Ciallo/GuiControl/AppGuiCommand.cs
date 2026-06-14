namespace Ciallo.GuiControl;

public static class AppGuiCommand
{
    public static void PopupNewDocumentDialog()
    {
        AppDialogHost.NewDocumentDialog.Popup();
    }

    public static void PopupOpenDocumentDialog()
    {
        AppDialogHost.OpenDocumentDialog.Popup();
    }
}
