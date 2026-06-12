using System;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

public partial class OpenDocumentDialog : FileDialog
{
    public override void _Ready()
    {
        FileSelected += path => LoadWorldFile(path);
        CurrentDir = OS.GetSystemDir(OS.SystemDir.Documents);
    }

    public static bool LoadWorldFile(string path)
    {
        Entity dataDocument;
        try
        {
            dataDocument = AppDocumentManager.Load(path);
        }
        catch (Exception exception)
        {
            GD.PrintErr(exception);
            AppDialogHost.WarnUser.DialogText = "Cannot open document.".Tr() + " " + exception.Message;
            AppDialogHost.WarnUser.Popup();
            return false;
        }
        AppDocumentManager.CopyWorldByData(dataDocument);
        dataDocument.World.Dispose();
        if (!AppPreference.RecentFiles.Contains(path))
            AppPreference.RecentFiles.Add(path);
        return true;
    }
}
