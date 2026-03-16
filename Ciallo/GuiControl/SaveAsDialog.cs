using Ciallo.Data;
using Godot;

namespace Ciallo.GuiControl;

public partial class SaveAsDialog : FileDialog
{
    public override void _Ready()
    {
        CurrentDir = OS.GetSystemDir(OS.SystemDir.Documents);
        FileSelected += path =>
        {
            AppDocumentManager.SaveWorkingDocumentAs(path);
            if (!AppPreference.RecentFiles.Contains(path))
                AppPreference.RecentFiles.Add(path);
        };
    }
}