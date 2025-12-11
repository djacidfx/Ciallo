using Ciallo.Data;
using Godot;

namespace Ciallo.NodeControl;

public partial class SaveAsDialog : FileDialog
{
    public override void _Ready()
    {
        CurrentDir = OS.GetSystemDir(OS.SystemDir.Documents);
        FileSelected += path =>
        {
            AppWorldManager.SaveWorkingWorldAs(path);
            if (!AppPreference.RecentFiles.Contains(path))
                AppPreference.RecentFiles.Add(path);
        };
    }
}