using Godot;
using System;
using System.IO;
using Ciallo.Core;
using Ciallo.Data;

namespace Ciallo.Gui;

public partial class NewDocumentDialog : ConfirmationDialog
{
    public void OnCreate()
    {
        var docNameControl = GetNode<LineEdit>("%DocumentNameControl");
        var docName = docNameControl.Text;
        var saveFolderControl = GetNode<FilePathPicker>("%SaveFolderControl");
        var saveFolder = saveFolderControl.Path;
        var referenceSizeControl = GetNode<Vector2Editor>("%ReferenceSizeControl");
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
        if (!IsValidFileName(docName))
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
            FilePath = filePath,
        };
        DocumentManager.CreateDocument(setting);
        this.Hide();
    }
    
    // Gen by copilot
    public static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
    
        // Check for invalid characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.IndexOfAny(invalidChars) >= 0) return false;

        // Optionally: Check for reserved Windows names
        string[] reservedNames = {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        if (Array.Exists(reservedNames, rn => rn == nameWithoutExtension)) return false;

        return true;
    }
}
