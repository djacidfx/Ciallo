using System.IO;
using Godot;

namespace Ciallo.Gui;

[GlobalClass, Tool]
public partial class FilePathPicker : HBoxContainer
{
    [Export]
    public FileDialog.FileModeEnum FileMode = FileDialog.FileModeEnum.OpenFile;
    public readonly LineEdit PathEdit;
    public readonly Button OpenExplorerButton;
    public FileDialog FileDialog;

    public FilePathPicker()
    {
        OpenExplorerButton = new Button
        {
            Icon = GD.Load<Texture2D>("res://Icons/folder-edit-outline.svg"),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            TooltipText = "Select a file or folder",
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(32, 32),
        };
        PathEdit = new LineEdit
        {
            CustomMinimumSize = new Vector2(200, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };

        AddChild(OpenExplorerButton);
        AddChild(PathEdit);
        PathEdit.SetOwner(this);
        OpenExplorerButton.SetOwner(this);
        OpenExplorerButton.Pressed += OnOpenExplorer;
    }

    private void OnOpenExplorer()
    {
        string path = PathEdit.Text ?? string.Empty;

        if (File.Exists(path) || Directory.Exists(path.GetBaseDir()))
        {
            path = path.GetBaseDir();
        }
        else
        {
            path = OS.GetSystemDir(OS.SystemDir.Documents);
        }
        
        FileDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileMode,
            CurrentDir = path,
            Size = new Vector2I(800, 600),
            ForceNative = true,
            Title = $"Select a {(FileMode == FileDialog.FileModeEnum.OpenDir? "folder":"file")}",
            Unresizable = false,
        };
        FileDialog.FileSelected += OnFileSelected;
        var root = GetTree().Root;
        root.AddChild(FileDialog);
        FileDialog.PopupCentered();
    }

    private void OnFileSelected(string path)
    {
        PathEdit.Text = path;
        FileDialog.Hide();
        FileDialog.QueueFree();
    }
}