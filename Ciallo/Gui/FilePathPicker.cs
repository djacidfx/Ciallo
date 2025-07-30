using System.IO;
using Godot;

namespace Ciallo.Gui;

[GlobalClass, Tool]
public partial class FilePathPicker : HBoxContainer
{
    [Export]
    public FileDialog.FileModeEnum FileMode = FileDialog.FileModeEnum.OpenFile;
    [Export(PropertyHint.Enum, "None:-1,Desktop:0,Dcim:1,Documents:2,Downloads:3")] // From OS.SystemDir
    public int DefaultPath = -1;
    
    public LineEdit PathEdit;
    public Button OpenExplorerButton;
    public FileDialog FileDialog;
    
    public string Path
    {
        get => PathEdit.Text;
        set => PathEdit.Text = value;
    }

    public override void _EnterTree()
    {
        OpenExplorerButton = new Button
        {
            Icon = GD.Load<Texture2D>("res://Icons/folder-edit-outline.svg"),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            TooltipText = $"Select a {(FileMode == FileDialog.FileModeEnum.OpenDir? "folder":"file")}",
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
        
        if(DefaultPath != -1)
            Path = OS.GetSystemDir((OS.SystemDir)DefaultPath) ?? string.Empty;
    }
    
    public override void _ExitTree()
    {
        OpenExplorerButton.QueueFree();
        PathEdit.QueueFree();
        if (FileDialog != null)
        {
            FileDialog.QueueFree();
        }
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
            Title = $"Select a {(FileMode == FileDialog.FileModeEnum.OpenDir? "folder":"file")}",
            Unresizable = false,
        };
        if (FileMode == FileDialog.FileModeEnum.OpenDir)
        {
            FileDialog.DirSelected += OnSelected;
        }
        if(FileMode == FileDialog.FileModeEnum.OpenFile)
        {
            FileDialog.FileSelected += OnSelected;
        }
        this.AddChild(FileDialog);
        FileDialog.PopupCentered();
    }
    private void OnSelected(string path)
    {
        PathEdit.Text = path;
        FileDialog.Hide();
        FileDialog.QueueFree();
    }
}