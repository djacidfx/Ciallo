using System;
using System.Linq;
using Ciallo.Data;
using Ciallo.Misc;
using Frent;
using Godot;

namespace Ciallo.NodeControl;

public partial class OpenDocumentDialog : FileDialog
{
    public override void _Ready()
    {
        FileSelected += path => LoadWorldFile(path);
        CurrentDir = OS.GetSystemDir(OS.SystemDir.Documents);
    }

    public static bool LoadWorldFile(string path)
    {
        World dataWorld;
        Entity dataDocument;
        try
        {
            dataWorld = AppWorldManager.Load(path, out dataDocument);
        }
        catch (Exception exception)
        {
            GD.PrintErr(exception);
            var dialog = ((SceneTree)Engine.GetMainLoop()).GetNodesInGroup("Dialog").OfType<AcceptDialog>().Single(n => n.Name == "WarnUser");
            dialog.DialogText = "Cannot open document.".Tr() + " " + exception.Message;
            dialog.Popup();
            return false;
        }
        AppWorldManager.CopyWorldByData(dataDocument);
        dataWorld.Dispose();
        if (!AppPreference.RecentFiles.Contains(path))
            AppPreference.RecentFiles.Add(path);
        return true;
    }
}