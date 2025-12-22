using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Godot;

namespace Ciallo.NodeControl;

public partial class MenuFile : PopupMenu
{
    public static readonly OrderedDictionary<string, AppAction> MenuItems = new()
    {
        { "New document", AppActions.NewDocument },
        { "Open document", AppActions.OpenDocument },
        { "Close document", null },
        { "-1", null },
        { "Save", AppActions.Save },
        { "Save As...", AppActions.SaveAs },
        { "-2", null },
        { "Export as image", null },
        { "Export as Godot scene", null },
    };

    public override void _Ready()
    {
        foreach (var (i, item) in MenuItems.Index())
        {
            if (item.Key.StartsWith('-'))
            {
                AddSeparator();
                continue;
            }
            AddItem(Tr(item.Key));
            if (item.Value != null) SetItemShortcut(i, item.Value.Shortcut);
        }

        IndexPressed += id => OnIndexPressed((int)id);
    }

    public void OnIndexPressed(int id)
    {
        switch (id)
        {
            case 0: // New Document
                AppGuiCommand.PopupNewDocumentDialog();
                break;
            case 1: // Open Document
                AppGuiCommand.PopupOpenDocumentDialog();
                break;
            case 2: // Close Document
                if (AppDocumentManager.WorkingDocument.Value.IsNull) break;
                _ = AppDocumentManager.UserCloseWorkingWorld();
                break;
            case 4: // Save
                if (AppDocumentManager.WorkingDocument.Value.IsNull) break;
                AppDocumentManager.SaveWorkingWorld();
                break;
            case 5: // Save as
                if (AppDocumentManager.WorkingDocument.Value.IsNull) break;
                var dialog = GetTree().GetNodesInGroup("Dialog").OfType<SaveAsDialog>().Single();
                var setting = AppDocumentManager.WorkingDocument.CurrentValue.Get<DocumentSetting>();
                dialog.CurrentDir = setting.FilePath.CurrentValue.GetBaseDir();
                dialog.PopupCentered();
                break;

            case 7: // Export as image
                if (AppDocumentManager.WorkingDocument.Value.IsNull) break;
                var dialogExportImage = GetTree().GetNodesInGroup("Dialog").OfType<ExportImage>().Single();
                dialogExportImage.Init();
                dialogExportImage.Popup();
                break;

            case 8: // Export as Godot scene
                if (AppDocumentManager.WorkingDocument.Value.IsNull) break;
                var dialogExportGodot = GetTree().GetNodesInGroup("Dialog").OfType<ExportGodotScene>().Single();
                var documentSetting = AppDocumentManager.WorkingDocument.CurrentValue.Get<DocumentSetting>();
                dialogExportGodot.CurrentFile = documentSetting.Name.Value;
                dialogExportGodot.CurrentDir = documentSetting.FilePath.CurrentValue.GetBaseDir();
                dialogExportGodot.Popup();
                break;

            default:
                GD.PrintErr($"Unhandled menu item index: {id}");
                break;
        }
    }
}