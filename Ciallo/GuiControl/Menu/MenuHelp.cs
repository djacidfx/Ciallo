using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Ciallo.Command;
using Ciallo.Data;
using Godot;
using Environment = System.Environment;

namespace Ciallo.GuiControl;

public partial class MenuHelp : PopupMenu
{
    private FileDialog _researchAnimationDialog;

    public static readonly OrderedDictionary<string, AppAction> MenuItems = new()
    {
        { "User manual", null },
        { "About Ciallo", null },
        { "Copy system info", null },
        { "Report bug", null },
        { "-Debug", null },
        { "Load research animation", null },
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

    private void OnIndexPressed(int id)
    {
        switch (id)
        {
            case 0:
                OS.ShellOpen("https://www.patreon.com/posts/143863276");
                break;
            case 1:
                GetTree().GetNodesInGroup("Dialog").OfType<AcceptDialog>().First(n => n.Name == "AboutCiallo").Popup();
                break;
            case 2:
                DisplayServer.ClipboardSet(CollectSystemInfo());
                break;
            case 3:
                OS.ShellOpen("https://github.com/ShenCiao/Ciallo/issues/new");
                break;
            case 5:
                PopupResearchAnimationDialog();
                break;
        }
    }

    private void PopupResearchAnimationDialog()
    {
        if (AppDocumentManager.WorkingDocument.Value.IsNull) return;

        if (!IsInstanceValid(_researchAnimationDialog))
        {
            _researchAnimationDialog = new FileDialog
            {
                Access = FileDialog.AccessEnum.Filesystem,
                FileMode = FileDialog.FileModeEnum.OpenAny,
                Title = "Load research animation",
                CurrentDir = OS.GetSystemDir(OS.SystemDir.Documents),
                Size = new Vector2I(1080, 720),
                DisplayMode = FileDialog.DisplayModeEnum.List,
                UseNativeDialog = true,
            };
            _researchAnimationDialog.Filters = ["*.csv;Research animation CSV"];
            _researchAnimationDialog.FileSelected += OnResearchAnimationPathSelected;
            _researchAnimationDialog.DirSelected += OnResearchAnimationPathSelected;
            AddChild(_researchAnimationDialog);
        }

        _researchAnimationDialog.PopupCentered();
    }

    private void OnResearchAnimationPathSelected(string path)
    {
        try
        {
            ResearchAnimationImporter.Import(AppDocumentManager.WorkingDocument.Value, path);
        }
        catch (Exception exception)
        {
            GD.PrintErr(exception);
            var dialog = GetTree().GetNodesInGroup("Dialog").OfType<AcceptDialog>().Single(n => n.Name == "WarnUser");
            dialog.DialogText = "Cannot load research animation.".Tr() + " " + exception.Message;
            dialog.Popup();
        }
    }

    public static string CollectSystemInfo()
    {
        var builder = new StringBuilder();
        var device = RenderingServer.GetRenderingDevice();

        builder.AppendLine($"OS: {Environment.OSVersion}");
        var cpuNames = new List<string>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var item in searcher.Get())
            {
                var name = item["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    cpuNames.Add(name);
                }
            }
        }
        if (cpuNames.Count == 0)
        {
            cpuNames.Add(RuntimeInformation.ProcessArchitecture.ToString());
        }
        builder.AppendLine($"CPU: {string.Join("|", cpuNames)}");
        builder.AppendLine($"GPU: {device.GetDeviceName()}");
        var driverInfo = OS.GetVideoAdapterDriverInfo();
        builder.AppendLine($"Driver: {driverInfo[0]} {driverInfo[1]}");

        return builder.ToString().TrimEnd();
    }
}
