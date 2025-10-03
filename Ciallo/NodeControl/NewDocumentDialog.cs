using System.IO;
using Ciallo.Data;
using Ciallo.Widget;
using Godot;

namespace Ciallo.NodeControl;

public partial class NewDocumentDialog : ConfirmationDialog
{
    public void OnCreate()
    {
        var docNameControl = GetNode<LineEdit>("%DocumentNameControl");
        var docName = docNameControl.Text;
        var saveFolderControl = GetNode<FilePathPicker>("%SaveFolderControl");
        var saveFolder = saveFolderControl.Path;
        var referenceSizeControl = GetNode<Vector2Edit>("%ReferenceSizeControl");
        var referenceSize = referenceSizeControl.Value;
        var bgControl = GetNode<ColorPickerButton>("%BackgroundColorControl");
        var bgColor = bgControl.Color;
        var errorMessage = GetNode<Label>("%ErrorMessage");
        errorMessage.Visible = false;
        
        // Sanity checks
        if (string.IsNullOrEmpty(docName))
        {
            errorMessage.Text = "Document name cannot be empty.";
            errorMessage.Visible = true;
            return;
        }
        if (docName.IsValidFileName())
        {
            errorMessage.Text = "Invalid document name. Please use a valid file name.";
            errorMessage.Visible = true;
            return;
        }
        if(!Directory.Exists(saveFolder))
        {
            errorMessage.Text = "Save folder does not exist.";
            errorMessage.Visible = true;
            return;
        }
        
        // Compute file path
        if(!saveFolder.EndsWith('/'))
            saveFolder += "/";
        string filePath = saveFolder + docName + ".ciallo";
        int index = 1;
        while(File.Exists(filePath))
        {
            filePath = saveFolder + docName + index++ + ".ciallo";
        }
        
        // Create the document
        var setting = new DocumentSetting
        {
            Name = { Value = docName },
            ReferenceSize = { Value = referenceSize },
            BackgroundColor = { Value = bgColor },
            FilePath = {Value = filePath},
        };
        var world = AppWorldManager.Create(setting);
        AppWorldManager.WorkingWorld.Value = world;
        AppWorldManager.InitialEmptyWorldForUser(world);
        AppWorldManager.SaveWorkingWorld();
        Hide();
        
        AppPreference.RecentFiles.Add(filePath);
    }
}
